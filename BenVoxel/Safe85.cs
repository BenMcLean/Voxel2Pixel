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
	public static string EncodeSafe85(this Stream inputBinaryData, bool lengthField = false)
	{
		using MemoryStream outputUtf8 = new();
		inputBinaryData.EncodeSafe85(outputUtf8, lengthField);
		return Encoding.UTF8.GetString(outputUtf8.GetBuffer(), 0, (int)outputUtf8.Length);
	}
	public static string EncodeSafe85(this byte[] binaryData, bool lengthField = false)
	{
		using MemoryStream inputBinaryData = new(binaryData);
		return inputBinaryData.EncodeSafe85(lengthField);
	}
	public static void EncodeSafe85(this Stream inputBinaryData, Stream outputUtf8, bool lengthField = false)
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
		byte[] buffer = new byte[BytesPerGroup];
		int bytesRead;
		while ((bytesRead = inputBinaryData.Read(buffer, 0, BytesPerGroup)) > 0)
		{
			ulong accumulator = 0;
			for (int i = 0; i < bytesRead; i++)
				accumulator = (accumulator << 8) | buffer[i];
			for (int i = ChunksPerGroup - 1; i >= ChunksPerGroup - bytesRead - 1; i--)
			{
				int chunk = (int)(accumulator % 85);
				accumulator /= 85;
				outputUtf8.WriteByte(EncodingTable[chunk]);
			}
		}
	}
	#endregion Encoding
	#region Decoding
	public static byte[] DecodeSafe85(this Stream inputUtf8, bool lengthField = false)
	{
		using MemoryStream outputBinaryData = new();
		inputUtf8.DecodeSafe85(outputBinaryData, lengthField);
		return outputBinaryData.ToArray();
	}
	public static byte[] DecodeSafe85(this string utf8, bool lengthField = false)
	{
		using MemoryStream inputUtf8 = new(Encoding.UTF8.GetBytes(utf8));
		return inputUtf8.DecodeSafe85(lengthField);
	}
	public static void DecodeSafe85(this Stream inputUtf8, Stream outputBinaryData, bool lengthField = false)
	{
		int expectedLength = 0, nextByte;
		if (lengthField)
		{
			int shift = 0, chunk, chunkCount = 0;
			do
			{
				nextByte = inputUtf8.ReadByte();
				if (nextByte == -1)
					throw new InvalidDataException("Unexpected end of stream while reading length field.");
				chunk = EncodingTable.IndexOf((byte)nextByte);
				if (chunk == -1 || chunk >= 64)
					throw new InvalidDataException($"Invalid character in length field: \"{(char)nextByte}\".");
				expectedLength |= (chunk & LengthChunkDataMask) << shift;
				shift += BitsPerLengthChunk;
				if (++chunkCount > MaxLengthFieldChunks)
					throw new InvalidDataException("Length field is too long.");
			} while ((chunk & LengthChunkContinueBit) != 0);
		}
		ulong accumulator = 0;
		int accumulatedChunks = 0, groupSize = 0, i = 0;
		while ((nextByte = inputUtf8.ReadByte()) != -1)
		{
			if (char.IsWhiteSpace((char)nextByte))
				continue;
			int chunk = EncodingTable.IndexOf((byte)nextByte);
			if (chunk == -1)
				throw new InvalidDataException($"Invalid character in input: \"{(char)nextByte}\".");
			accumulator = accumulator * 85 + (ulong)chunk;
			if (++accumulatedChunks == ChunksPerGroup || ++i == ChunksPerGroup)
			{
				groupSize = accumulatedChunks;
				int bytesToWrite = groupSize == ChunksPerGroup ? BytesPerGroup : groupSize - 1;
				for (int j = bytesToWrite - 1; j >= 0; j--)
					outputBinaryData.WriteByte((byte)(accumulator >> (j * 8)));
				accumulator = 0;
				accumulatedChunks = 0;
				i = 0;
			}
		}
		if (accumulatedChunks > 0)
		{
			int bytesToWrite = accumulatedChunks - 1;
			for (int j = bytesToWrite - 1; j >= 0; j--)
				outputBinaryData.WriteByte((byte)(accumulator >> (j * 8)));
		}
		if (lengthField && outputBinaryData.Length != expectedLength)
			throw new InvalidDataException("Decoded data length does not match the length field.");
	}
	#endregion Decoding
}
