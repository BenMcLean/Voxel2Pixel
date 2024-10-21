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
		return Encoding.UTF8.GetString(outputUtf8.ToArray());
	}
	public static string EncodeSafe85(this byte[] binaryData, bool lengthField = false)
	{
		using MemoryStream inputBinaryData = new(binaryData);
		return inputBinaryData.EncodeSafe85(lengthField);
	}
	public static void EncodeSafe85(this Stream inputBinaryData, Stream outputUtf8, bool lengthField = false)
	{
		long length = inputBinaryData.Length;
		if (lengthField)
		{
			if (length > MaxEncodableLength)
				throw new ArgumentException($"Input data length exceeds maximum encodable length of {MaxEncodableLength}.");
			int chunkCount = 1;
			long tempLength = length;
			while (tempLength >= 32)
			{
				chunkCount++;
				tempLength /= 32;
			}
			for (int i = chunkCount - 1; i >= 0; i--)
			{
				int chunk = (int)(length & LengthChunkDataMask);
				if (i > 0) // Set continuation bit for all but the last chunk
					chunk |= LengthChunkContinueBit;
				outputUtf8.WriteByte(EncodingTable[chunk]);
				length >>= BitsPerLengthChunk;
			}
		}
		if (inputBinaryData.Length == 0)
		{
			if (!lengthField)
				outputUtf8.WriteByte(EncodingTable[0]); // Write "!" for empty input without length field
			return;
		}
		byte[] inputBuffer = new byte[BytesPerGroup],
			encodedBuffer = new byte[ChunksPerGroup];
		int bytesRead;
		while ((bytesRead = inputBinaryData.Read(inputBuffer, 0, BytesPerGroup)) > 0)
		{
			ulong accumulator = 0;
			for (int i = 0; i < bytesRead; i++)
				accumulator = (accumulator << 8) | inputBuffer[i];
			int chunkCount = bytesRead == BytesPerGroup ? ChunksPerGroup : bytesRead + 1;
			for (int i = chunkCount - 1; i >= 0; i--)
			{
				encodedBuffer[i] = EncodingTable[(int)(accumulator % 85)];
				accumulator /= 85;
			}
			outputUtf8.Write(encodedBuffer, 0, chunkCount);
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
		long expectedLength = -1;
		if (lengthField)
		{
			expectedLength = 0;
			int shift = 0, chunk, chunkCount = 0;
			do
			{
				int nextByte;
				do
				{
					nextByte = inputUtf8.ReadByte();
					if (nextByte == -1)
						throw new InvalidDataException("Unexpected end of stream while reading length field.");
				} while (char.IsWhiteSpace((char)nextByte));
				chunk = EncodingTable.IndexOf((byte)nextByte);
				if (chunk == -1 || chunk >= 64)
					throw new InvalidDataException($"Invalid character in length field: \"{(char)nextByte}\".");
				expectedLength |= (long)(chunk & LengthChunkDataMask) << shift;
				shift += BitsPerLengthChunk;
				if (++chunkCount > MaxLengthFieldChunks)
					throw new InvalidDataException("Length field is too long.");
			} while ((chunk & LengthChunkContinueBit) != 0);
		}
		ulong accumulator = 0;
		int accumulatedChunks = 0;
		byte[] outputBuffer = new byte[BytesPerGroup];
		long decodedLength = 0;
		while (true)
		{
			int nextByte = inputUtf8.ReadByte();
			if (nextByte == -1)
				break;
			if (char.IsWhiteSpace((char)nextByte))
				continue;
			int chunk = EncodingTable.IndexOf((byte)nextByte);
			if (chunk == -1)
				throw new InvalidDataException($"Invalid character in input: \"{(char)nextByte}\".");
			accumulator = accumulator * 85 + (ulong)chunk;
			if (++accumulatedChunks == ChunksPerGroup)
			{
				for (int i = BytesPerGroup - 1; i >= 0; i--)
				{
					outputBuffer[i] = (byte)accumulator;
					accumulator >>= 8;
				}
				int bytesToWrite = BytesPerGroup;
				if (lengthField && decodedLength + bytesToWrite > expectedLength)
					bytesToWrite = (int)(expectedLength - decodedLength);
				outputBinaryData.Write(outputBuffer, 0, bytesToWrite);
				decodedLength += bytesToWrite;
				if (lengthField && decodedLength >= expectedLength)
					break;
				accumulator = 0;
				accumulatedChunks = 0;
			}
		}
		if (accumulatedChunks > 0)
		{
			int remainingBytes = accumulatedChunks - 1;
			for (int i = remainingBytes - 1; i >= 0; i--)
			{
				outputBuffer[i] = (byte)accumulator;
				accumulator >>= 8;
			}
			int bytesToWrite = remainingBytes;
			if (lengthField && decodedLength + bytesToWrite > expectedLength)
				bytesToWrite = (int)(expectedLength - decodedLength);
			outputBinaryData.Write(outputBuffer, 0, bytesToWrite);
			decodedLength += bytesToWrite;
		}
		if (lengthField && decodedLength != expectedLength)
			throw new InvalidDataException($"Decoded data length ({decodedLength}) does not match the length field ({expectedLength}).");
	}
	#endregion Decoding
}
