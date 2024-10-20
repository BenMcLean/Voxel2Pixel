using System;
using System.Collections.ObjectModel;
using System.IO;

namespace BenVoxel;

/// <summary>
/// Implements Safe85 Encoding.
/// This class was translated to C# by Claude (the LLM) from the reference implementation in C and then cleaned up by Ben McLean.
/// Specification: https://github.com/kstenerud/safe-encoding/blob/master/safe85-specification.md
/// Reference implementation: https://github.com/kstenerud/safe-encoding/tree/master/reference-implementation/safe85
/// </summary>
public static class Safe85
{
	public const string Version = "1.0.0";
	public static string GetVersion() => Version;
	private const byte ChunkCodeError = 0xff,
		ChunkCodeWhitespace = 0xfe;
	private const int BytesPerGroup = 4,
		ChunksPerGroup = 5,
		BitsPerByte = 8,
		FactorPerChunk = 85,
		BitsPerLengthChunk = 5;
	private static readonly ReadOnlyCollection<byte> EncodeCharToChunk = Array.AsReadOnly<byte>([
		0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xfe,0xfe,0xff,0xff,0xfe,0xff,0xff,
		0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
		0xfe,0x00,0xff,0xff,0x01,0xff,0xff,0xff,0x02,0x03,0x04,0x05,0x06,0x07,0x08,0xff,
		0x09,0x0a,0x0b,0x0c,0x0d,0x0e,0x0f,0x10,0x11,0x12,0x13,0x14,0xff,0x15,0x16,0xff,
		0x17,0x18,0x19,0x1a,0x1b,0x1c,0x1d,0x1e,0x1f,0x20,0x21,0x22,0x23,0x24,0x25,0x26,
		0x27,0x28,0x29,0x2a,0x2b,0x2c,0x2d,0x2e,0x2f,0x30,0x31,0x32,0xff,0x33,0x34,0x35,
		0x36,0x37,0x38,0x39,0x3a,0x3b,0x3c,0x3d,0x3e,0x3f,0x40,0x41,0x42,0x43,0x44,0x45,
		0x46,0x47,0x48,0x49,0x4a,0x4b,0x4c,0x4d,0x4e,0x4f,0x50,0x51,0x52,0x53,0x54,0xff,
		0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
		0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
		0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
		0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
		0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
		0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
		0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
		0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff
	]),
		ChunkToEncodeChar = Array.AsReadOnly("!$()*+,-.0123456789:;=>@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~"u8.ToArray());
	private static readonly ReadOnlyCollection<int> ChunkToByteCount = Array.AsReadOnly([0, 0, 1, 2, 3, 4]),
		ByteToChunkCount = Array.AsReadOnly([0, 2, 3, 4, 5]);
	public enum Safe85Status
	{
		Ok = 0,
		PartiallyComplete = -1,
		ErrorInvalidSourceData = -2,
		ErrorUnterminatedLengthField = -3,
		ErrorTruncatedData = -4,
		ErrorInvalidLength = -5,
		ErrorNotEnoughRoom = -6
	}
	[Flags]
	public enum Safe85StreamState
	{
		None = 0,
		ExpectDstStreamToEnd = 1,
		SrcIsAtEndOfStream = 2,
		DstIsAtEndOfStream = 4
	}
	private static void WriteBytes(ref int dstIndex, byte[] dstBuffer, long accumulator, int chunkCount)
	{
		int bytesToWrite = ChunkToByteCount[chunkCount];
		for (int i = bytesToWrite - 1; i >= 0; i--)
			dstBuffer[dstIndex++] = (byte)((accumulator >> (i * BitsPerByte)) & 0xFF);
	}
	public static long GetEncodedLength(long decodedLength, bool includeLengthField)
	{
		if (decodedLength < 0)
			return (long)Safe85Status.ErrorInvalidLength;
		long groupCount = decodedLength / BytesPerGroup;
		int chunkCount = ByteToChunkCount[(int)(decodedLength % BytesPerGroup)],
			lengthChunkCount = 0;
		if (includeLengthField)
			lengthChunkCount = CalculateLengthChunkCount(decodedLength);
		return groupCount * ChunksPerGroup + chunkCount + lengthChunkCount;
	}
	public static long GetDecodedLength(long encodedLength)
	{
		if (encodedLength < 0)
			return (long)Safe85Status.ErrorInvalidLength;
		long groupCount = encodedLength / ChunksPerGroup;
		int byteCount = ChunkToByteCount[(int)(encodedLength % ChunksPerGroup)];
		return groupCount * BytesPerGroup + byteCount;
	}
	public static Safe85Status EncodeFeed(ref int srcIndex, byte[] srcBuffer, long srcLength, ref int dstIndex, byte[] dstBuffer, long dstLength, bool isEndOfData)
	{
		if (srcLength < 0 || dstLength < 0)
			return Safe85Status.ErrorInvalidLength;
		int lastSrcIndex = srcIndex,
			currentGroupByteCount = 0;
		long accumulator = 0;
		while (srcIndex < srcLength)
		{
			byte nextByte = srcBuffer[srcIndex++];
			accumulator = (accumulator << BitsPerByte) | nextByte;
			currentGroupByteCount++;
			if (currentGroupByteCount == BytesPerGroup)
			{
				if (!WriteChunks(ref dstIndex, dstBuffer, dstLength, accumulator, currentGroupByteCount))
				{
					srcIndex = lastSrcIndex;
					return Safe85Status.PartiallyComplete;
				}
				currentGroupByteCount = 0;
				accumulator = 0;
				lastSrcIndex = srcIndex;
			}
		}
		if (currentGroupByteCount > 0 && isEndOfData)
		{
			if (!WriteChunks(ref dstIndex, dstBuffer, dstLength, accumulator, currentGroupByteCount))
			{
				srcIndex = lastSrcIndex;
				return Safe85Status.PartiallyComplete;
			}
		}
		else if (currentGroupByteCount > 0)
			srcIndex -= currentGroupByteCount;
		return Safe85Status.Ok;
	}
	public static Safe85Status EncodeStream(Stream input, Stream output, bool isEndOfData)
	{
		byte[] buffer = new byte[BytesPerGroup];
		int bytesRead;
		while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
		{
			byte[] encodeBuffer = new byte[ChunksPerGroup];
			int encodeIndex = 0;
			Safe85Status status = EncodeFeed(ref encodeIndex, buffer, bytesRead, ref encodeIndex, encodeBuffer, ChunksPerGroup, bytesRead < BytesPerGroup && isEndOfData);
			if (status != Safe85Status.Ok)
				return status;
			output.Write(encodeBuffer, 0, encodeIndex);
		}
		return Safe85Status.Ok;
	}
	private static bool WriteChunks(ref int dstIndex, byte[] dstBuffer, long dstLength, long accumulator, int byteCount)
	{
		int chunksToWrite = ByteToChunkCount[byteCount];
		if (dstIndex + chunksToWrite > dstLength)
			return false;
		for (int i = chunksToWrite - 1; i >= 0; i--)
			dstBuffer[dstIndex++] = ChunkToEncodeChar[ExtractChunkFromAccumulator(accumulator, i)];
		return true;
	}
	private static readonly ReadOnlyCollection<int> _divideAmounts = Array.AsReadOnly([1, 85, 85 * 85, 85 * 85 * 85, 85 * 85 * 85 * 85]);
	private static int ExtractChunkFromAccumulator(long accumulator, int chunkIndexLoFirst)
	{
		int chunkModulo = FactorPerChunk;
		if (chunkIndexLoFirst == 0)
			return (int)(accumulator % chunkModulo);
		int divideAmount = _divideAmounts[chunkIndexLoFirst];
		return (int)((accumulator / divideAmount) % chunkModulo);
	}
	public static long WriteLengthField(long length, byte[] dstBuffer, long dstBufferLength)
	{
		if (dstBufferLength < 0 || length < 0)
			return (long)Safe85Status.ErrorInvalidLength;
		int continuationBit = 1 << BitsPerLengthChunk,
			chunkMask = continuationBit - 1,
			chunkCount = CalculateLengthChunkCount(length);
		if (chunkCount > dstBufferLength)
			return (long)Safe85Status.ErrorNotEnoughRoom;
		int dstIndex = 0;
		for (int shiftAmount = chunkCount - 1; shiftAmount >= 0; shiftAmount--)
		{
			int shouldContinue = (shiftAmount == 0) ? 0 : continuationBit,
				chunkValue = (int)(((length >> (BitsPerLengthChunk * shiftAmount)) & chunkMask) + shouldContinue);
			dstBuffer[dstIndex++] = ChunkToEncodeChar[chunkValue];
		}
		return chunkCount;
	}
	public static long Encode(byte[] srcBuffer, long srcLength, byte[] dstBuffer, long dstLength)
	{
		if (srcLength < 0 || dstLength < 0)
			return (long)Safe85Status.ErrorInvalidLength;
		int srcIndex = 0,
			dstIndex = 0;
		Safe85Status status = EncodeFeed(ref srcIndex, srcBuffer, srcLength, ref dstIndex, dstBuffer, dstLength, true);
		if (status != Safe85Status.Ok)
			return status == Safe85Status.PartiallyComplete ? (long)Safe85Status.ErrorNotEnoughRoom : (long)status;
		return dstIndex;
	}
	public static long EncodeWithLength(byte[] srcBuffer, long srcLength, byte[] dstBuffer, long dstLength)
	{
		if (srcLength < 0 || dstLength < 0)
			return (long)Safe85Status.ErrorInvalidLength;
		long bytesUsed = WriteLengthField(srcLength, dstBuffer, dstLength);
		if (bytesUsed < 0)
			return bytesUsed;
		int srcIndex = 0,
			dstIndex = (int)bytesUsed;
		Safe85Status status = EncodeFeed(ref srcIndex, srcBuffer, srcLength, ref dstIndex, dstBuffer, dstLength, true);
		if (status != Safe85Status.Ok)
			return status == Safe85Status.PartiallyComplete ? (long)Safe85Status.ErrorNotEnoughRoom : (long)status;
		return dstIndex;
	}
	private static int CalculateLengthChunkCount(long length)
	{
		int chunkCount = 0;
		for (ulong i = (ulong)length; i > 0; i >>= BitsPerLengthChunk, chunkCount++) { }
		return chunkCount == 0 ? 1 : chunkCount;
	}
	public static Safe85Status DecodeFeed(ref int srcIndex, byte[] srcBuffer, long srcLength, ref int dstIndex, byte[] dstBuffer, long dstLength, Safe85StreamState streamState)
	{
		if (srcLength < 0 || dstLength < 0)
			return Safe85Status.ErrorInvalidLength;
		int currentGroupChunkCount = 0;
		long accumulator = 0;
		while (srcIndex < srcLength)
		{
			byte nextChar = srcBuffer[srcIndex++],
				nextChunk = EncodeCharToChunk[nextChar];
			if (nextChunk == ChunkCodeWhitespace)
				continue;
			if (nextChunk == ChunkCodeError)
			{
				srcIndex--;
				return Safe85Status.ErrorInvalidSourceData;
			}
			accumulator = AccumulateChunk(accumulator, nextChunk);
			currentGroupChunkCount++;
			if (dstIndex + ChunkToByteCount[currentGroupChunkCount] >= dstLength)
				break;
			if (currentGroupChunkCount == ChunksPerGroup)
			{
				WriteBytes(ref dstIndex, dstBuffer, accumulator, currentGroupChunkCount);
				currentGroupChunkCount = 0;
				accumulator = 0;
			}
		}
		while (srcIndex < srcLength && EncodeCharToChunk[srcBuffer[srcIndex]] == ChunkCodeWhitespace)
			srcIndex++;
		int lastSrcIndex = srcIndex;
		bool srcIsAtEnd = (streamState.HasFlag(Safe85StreamState.SrcIsAtEndOfStream) && srcIndex >= srcLength),
			dstIsAtEnd = (streamState.HasFlag(Safe85StreamState.DstIsAtEndOfStream) && dstIndex + ChunkToByteCount[currentGroupChunkCount] >= dstLength);
		if (currentGroupChunkCount > 0 && (srcIsAtEnd || dstIsAtEnd))
		{
			WriteBytes(ref dstIndex, dstBuffer, accumulator, currentGroupChunkCount);
			lastSrcIndex = srcIndex;
			dstIsAtEnd = (streamState.HasFlag(Safe85StreamState.DstIsAtEndOfStream) && dstIndex + ChunkToByteCount[currentGroupChunkCount] >= dstLength);
		}
		srcIndex = lastSrcIndex;
		return srcIsAtEnd || dstIsAtEnd ?
			streamState.HasFlag(Safe85StreamState.ExpectDstStreamToEnd) ?
				dstIsAtEnd ?
					Safe85Status.Ok
					: Safe85Status.ErrorTruncatedData
				: srcIsAtEnd ?
					Safe85Status.Ok
					: Safe85Status.ErrorNotEnoughRoom
			: Safe85Status.PartiallyComplete;
	}
	public static Safe85Status DecodeStream(Stream input, Stream output, Safe85StreamState streamState)
	{
		byte[] buffer = new byte[ChunksPerGroup];
		int bytesRead;
		while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
		{
			byte[] decodeBuffer = new byte[BytesPerGroup];
			int decodeIndex = 0,
				inputIndex = 0;
			Safe85Status status = DecodeFeed(ref inputIndex, buffer, bytesRead, ref decodeIndex, decodeBuffer, BytesPerGroup, streamState);
			if (status != Safe85Status.Ok && status != Safe85Status.PartiallyComplete)
				return status;
			output.Write(decodeBuffer, 0, decodeIndex);
		}
		return Safe85Status.Ok;
	}
	public static long ReadLengthField(byte[] buffer, long bufferLength, out long length)
	{
		length = 0;
		if (bufferLength < 0)
			return (long)Safe85Status.ErrorInvalidLength;
		long maxPreAppendValue = long.MaxValue >> BitsPerLengthChunk,
			value = 0;
		int continuationBit = 1 << BitsPerLengthChunk,
			maxChunkValue = continuationBit - 1,
			chunkMask = continuationBit - 1,
			nextChunk = 0,
			srcIndex = 0;
		while (srcIndex < bufferLength)
		{
			nextChunk = EncodeCharToChunk[buffer[srcIndex]];
			if (nextChunk == ChunkCodeWhitespace)
			{
				srcIndex++;
				continue;
			}
			int chunkValue = nextChunk & ~continuationBit;
			if (chunkValue > maxChunkValue)
				return (long)Safe85Status.ErrorInvalidSourceData;
			if (value > maxPreAppendValue)
				return (long)Safe85Status.ErrorInvalidSourceData;
			value = (value << BitsPerLengthChunk) | (long)(nextChunk & chunkMask);
			srcIndex++;
			if ((nextChunk & continuationBit) == 0)
				break;
		}
		if ((nextChunk & continuationBit) != 0)
			return (long)Safe85Status.ErrorUnterminatedLengthField;
		length = value;
		return srcIndex;
	}
	public static long Decode(byte[] srcBuffer, long srcLength, byte[] dstBuffer, long dstLength)
	{
		if (srcLength < 0 || dstLength < 0)
			return (long)Safe85Status.ErrorInvalidLength;
		int srcIndex = 0;
		int dstIndex = 0;
		Safe85Status status = DecodeFeed(ref srcIndex, srcBuffer, srcLength, ref dstIndex, dstBuffer, dstLength,
			Safe85StreamState.SrcIsAtEndOfStream | Safe85StreamState.DstIsAtEndOfStream);
		if (status != Safe85Status.Ok)
			return status == Safe85Status.PartiallyComplete ? (long)Safe85Status.ErrorNotEnoughRoom : (long)status;
		return dstIndex;
	}
	public static long DecodeWithLength(byte[] srcBuffer, long srcLength, byte[] dstBuffer, long dstLength)
	{
		if (srcLength < 0 || dstLength < 0)
			return (long)Safe85Status.ErrorInvalidLength;
		long specifiedLength,
			bytesUsed = ReadLengthField(srcBuffer, srcLength, out specifiedLength);
		if (bytesUsed < 0)
			return bytesUsed;
		long readLength = srcLength - bytesUsed;
		int srcIndex = (int)bytesUsed,
			dstIndex = 0;
		Safe85Status status = DecodeFeed(ref srcIndex, srcBuffer, readLength, ref dstIndex, dstBuffer, specifiedLength,
			Safe85StreamState.SrcIsAtEndOfStream | Safe85StreamState.DstIsAtEndOfStream | Safe85StreamState.ExpectDstStreamToEnd);
		if (status != Safe85Status.Ok)
			return status == Safe85Status.PartiallyComplete ? (long)Safe85Status.ErrorNotEnoughRoom : (long)status;
		long decodedByteCount = dstIndex;
		if (decodedByteCount < specifiedLength)
			return (long)Safe85Status.ErrorTruncatedData;
		return decodedByteCount;
	}
	private static long AccumulateChunk(long accumulator, byte nextChunk) => (accumulator * FactorPerChunk) + nextChunk;
}
