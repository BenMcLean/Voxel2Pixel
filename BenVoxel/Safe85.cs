using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace BenVoxel;

/// <summary>
/// Specification: https://github.com/kstenerud/safe-encoding/blob/master/safe85-specification.md
/// Reference implementation: https://github.com/kstenerud/safe-encoding/tree/master/reference-implementation/safe85
/// </summary>
public static class Safe85
{
	#region Read Only Data
	private const int BytesPerGroup = 4,
		ChunksPerGroup = 5,
		BitsPerLengthChunk = 5,
		LengthChunkContinueBit = 0x20,
		LengthChunkDataMask = 0x1F,
		MaxLengthFieldChunks = 6,
		MaxEncodableLength = (1 << 30) - 1; // Maximum length that can be encoded in MaxLengthFieldChunks
	private static readonly ReadOnlyCollection<byte> EncodingTable = Array.AsReadOnly([.. "!$()*+,-.0123456789:;=>@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~"u8]);
	#endregion Read Only Data
	#region Encoding
	public static string Encode(Stream binaryData, bool lengthField = false)
	{
		using MemoryStream output = new();
		Encode(binaryData, output, lengthField);
		return Encoding.UTF8.GetString(output.ToArray());
	}
	public static string Encode(byte[] binaryData, bool lengthField = false)
	{
		using MemoryStream input = new(binaryData);
		return Encode(input, lengthField);
	}
	public static void Encode(Stream inputBinaryData, Stream outputUtf8, bool lengthField = false)
	{
		if (lengthField)
		{
			long length = inputBinaryData.Length;
			if (length > MaxEncodableLength)
				throw new ArgumentException($"Input data length exceeds maximum encodable length of {MaxEncodableLength}.");
			do
			{
				int chunk = (int)length & LengthChunkDataMask;
				length >>= BitsPerLengthChunk;
				if (length > 0)
					chunk |= LengthChunkContinueBit;
				outputUtf8.WriteByte(EncodingTable[chunk]);
			} while (length > 0);
		}
		byte[] buffer = new byte[4];
		int bytesRead;
		while ((bytesRead = inputBinaryData.Read(buffer, 0, BytesPerGroup)) > 0)
		{
			int length = bytesRead;
			ulong accumulator = 0;
			for (int i = 0; i < length; i++)
				accumulator = (accumulator << 8) | buffer[i];
			for (int i = 4; i >= ChunksPerGroup - length; i--)
			{
				int chunk = (int)(accumulator % 85);
				if (i == 4 && chunk > 82)
					throw new InvalidDataException("First chunk cannot be larger than 82.");
				accumulator /= 85;
				outputUtf8.WriteByte(EncodingTable[chunk]);
			}
		}
	}
	#endregion Encoding
	#region Decoding
	public static byte[] Decode(Stream utf8, bool lengthField = false)
	{
		using MemoryStream output = new();
		Decode(utf8, output, lengthField);
		return output.ToArray();
	}
	public static byte[] Decode(string utf8, bool lengthField = false)
	{
		using MemoryStream input = new(Encoding.UTF8.GetBytes(utf8));
		return Decode(input, lengthField);
	}
	public static void Decode(Stream inputUtf8, Stream outputBinaryData, bool lengthField = false)
	{
		int length = 0;
		if (lengthField)
		{
			int shift = 0, chunk, chunkCount = 0;
			do
			{
				int nextByte = inputUtf8.ReadByte();
				if (nextByte == -1)
					throw new InvalidDataException("Unexpected end of stream while reading length field.");
				chunk = EncodingTable.IndexOf((byte)nextByte);
				if (chunk == -1 || chunk >= 64)
					throw new InvalidDataException($"Invalid character in length field: \"{(char)nextByte}\".");
				length |= (chunk & LengthChunkDataMask) << shift;
				shift += 5;
				if (++chunkCount > MaxLengthFieldChunks)
					throw new InvalidDataException("Length field is too long.");
			} while ((chunk & LengthChunkContinueBit) != 0);
		}
		byte[] buffer = new byte[5];
		int bytesRead;
		int ReadNextGroup()
		{
			int i = 0, nextByte;
			while (i < 5 && (nextByte = inputUtf8.ReadByte()) != -1)
				if (!char.IsWhiteSpace((char)nextByte))
					buffer[i++] = (byte)nextByte;
			return i;
		}
		while ((bytesRead = ReadNextGroup()) > 0)
		{
			ulong accumulator = 0;
			for (int i = 0; i < bytesRead; i++)
			{
				int chunk = EncodingTable.IndexOf(buffer[i]);
				if (chunk == -1)
					throw new InvalidDataException($"Invalid character in input: \"{(char)buffer[i]}\".");
				accumulator = accumulator * 85 + (ulong)chunk;
			}
			int bytesToWrite = bytesRead - 1;
			for (int i = bytesToWrite - 1; i >= 0; i--)
				outputBinaryData.WriteByte((byte)(accumulator >> (i * 8)));
		}
		if (lengthField && outputBinaryData.Length != length)
			throw new InvalidDataException("Decoded data length does not match the length field.");
	}
	#endregion Decoding
}
