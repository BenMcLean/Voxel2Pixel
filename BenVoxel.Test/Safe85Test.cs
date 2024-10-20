using System.Text;

namespace BenVoxel.Test;

public class Safe85Test
{
	private const int BytesPerGroup = 4;
	private const int ChunksPerGroup = 5;
	[Fact]
	public void TestVersion() => Assert.Equal(expected: "1.0.0", actual: Safe85.GetVersion());
	[Theory]
	[InlineData("(q", new byte[] { 0xf1 })]
	[InlineData("$aF", new byte[] { 0x2e, 0x99 })]
	[InlineData("Bq|Q", new byte[] { 0xf2, 0x34, 0x56 })]
	[InlineData("@{743", new byte[] { 0x4a, 0x88, 0xbc, 0xd1 })]
	[InlineData("|.Ps^$g", new byte[] { 0xff, 0x71, 0xdd, 0x3a, 0x92 })]
	public void TestEncodeDecode(string expectedEncoded, byte[] expectedDecoded) =>
		AssertEncodeDecode(expectedEncoded, expectedDecoded);
	[Theory]
	[InlineData("$(q", new byte[] { 0xf1 })]
	[InlineData("($aF", new byte[] { 0x2e, 0x99 })]
	[InlineData(")Bq|Q", new byte[] { 0xf2, 0x34, 0x56 })]
	[InlineData("*@{743", new byte[] { 0x4a, 0x88, 0xbc, 0xd1 })]
	[InlineData("+|.Ps^$g", new byte[] { 0xff, 0x71, 0xdd, 0x3a, 0x92 })]
	public void TestEncodeDecodeWithLength(string expectedEncoded, byte[] expectedDecoded) =>
		AssertEncodeDecodeWithLength(expectedEncoded, expectedDecoded);
	[Theory]
	[InlineData(4, "|.Ps^$g", (long)Safe85.Safe85Status.ErrorNotEnoughRoom)]
	[InlineData(3, "|.Ps^$g", (long)Safe85.Safe85Status.ErrorNotEnoughRoom)]
	[InlineData(2, "|.Ps^$g", (long)Safe85.Safe85Status.ErrorNotEnoughRoom)]
	[InlineData(1, "|.Ps^$g", (long)Safe85.Safe85Status.ErrorNotEnoughRoom)]
	public void TestDecodeError(int bufferSize, string encoded, long expectedStatusCode) =>
		AssertDecodeStatus(bufferSize, encoded, expectedStatusCode);
	[Theory]
	[InlineData("#.Ps^$g")]
	[InlineData("|#Ps^$g")]
	[InlineData("|.#s^$g")]
	[InlineData("|.P#^$g")]
	[InlineData("|.Ps#$g")]
	[InlineData("|.Ps^#g")]
	[InlineData("|.Ps^$#")]
	public void TestDecodeInvalidData(string encoded) =>
		AssertDecodeStatus(100, encoded, (long)Safe85.Safe85Status.ErrorInvalidSourceData);
	[Theory]
	[InlineData(" |.Ps^$g")]
	[InlineData("| .Ps^$g")]
	[InlineData("|. Ps^$g")]
	[InlineData("|.P s^$g")]
	[InlineData("|.Ps ^$g")]
	[InlineData("|.Ps^ $g")]
	[InlineData("|.Ps^$ g")]
	[InlineData("|.Ps^$g ")]
	[InlineData("|\t\t.\r\n\n P   s^\t \t\t$g")]
	public void TestDecodeWithWhitespace(string encoded) =>
		AssertDecode(encoded, [0xff, 0x71, 0xdd, 0x3a, 0x92]);
	[Theory]
	[InlineData(0, "!")]
	[InlineData(1, "$")]
	[InlineData(10, "1")]
	[InlineData(31, "H")]
	[InlineData(32, "J!")]
	[InlineData(33, "J$")]
	[InlineData(1023, "iH")]
	[InlineData(1024, "JI!")]
	[InlineData(1025, "JI$")]
	[InlineData(32767, "iiH")]
	[InlineData(32768, "JII!")]
	[InlineData(32769, "JII$")]
	[InlineData(1048575, "iiiH")]
	[InlineData(1048576, "JIII!")]
	[InlineData(1048577, "JIII$")]
	public void TestEncodeLength(long length, string expected) =>
		AssertEncodeLength(length, expected);
	[Theory]
	[InlineData(1, 0, (long)Safe85.Safe85Status.ErrorNotEnoughRoom)]
	[InlineData(1, 1, 1)]
	[InlineData(32, 1, (long)Safe85.Safe85Status.ErrorNotEnoughRoom)]
	[InlineData(32, 2, 2)]
	public void TestEncodeLengthStatus(long length, int bufferSize, long expectedStatus) =>
		AssertEncodeLengthStatus(length, bufferSize, expectedStatus);
	[Theory]
	[InlineData("!", 0, 1)]
	[InlineData("H", 31, 1)]
	[InlineData("J", 32, (long)Safe85.Safe85Status.ErrorUnterminatedLengthField)]
	[InlineData("J!", 32, 2)]
	[InlineData("JI", 1024, (long)Safe85.Safe85Status.ErrorUnterminatedLengthField)]
	[InlineData("JI!", 1024, 3)]
	public void TestDecodeLength(string encoded, long expectedLength, long expectedStatus) =>
		AssertDecodeLength(encoded, expectedLength, expectedStatus);
	[Theory]
	[InlineData("$$aF", -1, 1)]
	[InlineData("9*|-Ps^$g", -1, (long)Safe85.Safe85Status.ErrorTruncatedData)]
	[InlineData("$", -1, (long)Safe85.Safe85Status.ErrorTruncatedData)]
	[InlineData("/%q", -1, (long)Safe85.Safe85Status.ErrorInvalidSourceData)]
	[InlineData(" J $ 1 j a=a;71mK1lIG[I+9|Mh81U!_X!`XYRvJ]as!._(W", -1, 33)]
	public void TestDecodeWithLengthStatus(string encoded, long forceLength, long expectedStatus) =>
		AssertDecodeWithLengthStatus(encoded, forceLength, expectedStatus);
	[Fact]
	public void TestInvalidLength()
	{
		byte[] encodedData = new byte[100],
			decodedData = new byte[100];
		Assert.Equal((long)Safe85.Safe85Status.ErrorInvalidLength, Safe85.GetDecodedLength(-1));
		Assert.Equal((long)Safe85.Safe85Status.ErrorInvalidLength, Safe85.Decode(encodedData, -1, decodedData, 1));
		Assert.Equal((long)Safe85.Safe85Status.ErrorInvalidLength, Safe85.Decode(encodedData, 1, decodedData, -1));
		Assert.Equal((long)Safe85.Safe85Status.ErrorInvalidLength, Safe85.DecodeWithLength(encodedData, -1, decodedData, 1));
		Assert.Equal((long)Safe85.Safe85Status.ErrorInvalidLength, Safe85.DecodeWithLength(encodedData, 1, decodedData, -1));
		Assert.Equal((long)Safe85.Safe85Status.ErrorInvalidLength, Safe85.GetEncodedLength(-1, false));
		Assert.Equal((long)Safe85.Safe85Status.ErrorInvalidLength, Safe85.GetEncodedLength(-1, true));
		Assert.Equal((long)Safe85.Safe85Status.ErrorInvalidLength, Safe85.Encode(decodedData, -1, encodedData, 1));
		Assert.Equal((long)Safe85.Safe85Status.ErrorInvalidLength, Safe85.Encode(decodedData, 1, encodedData, -1));
		Assert.Equal((long)Safe85.Safe85Status.ErrorInvalidLength, Safe85.EncodeWithLength(decodedData, -1, encodedData, 1));
		Assert.Equal((long)Safe85.Safe85Status.ErrorInvalidLength, Safe85.EncodeWithLength(decodedData, 1, encodedData, -1));
		int srcIndex = 0, dstIndex = 0;
		Assert.Equal((long)Safe85.Safe85Status.ErrorInvalidLength, Safe85.ReadLengthField(encodedData, -1, out long length));
		Assert.Equal(Safe85.Safe85Status.ErrorInvalidLength, Safe85.DecodeFeed(ref srcIndex, encodedData, -1, ref dstIndex, decodedData, 1, Safe85.Safe85StreamState.None));
		Assert.Equal(Safe85.Safe85Status.ErrorInvalidLength, Safe85.DecodeFeed(ref srcIndex, encodedData, 1, ref dstIndex, decodedData, -1, Safe85.Safe85StreamState.None));
		Assert.Equal((long)Safe85.Safe85Status.ErrorInvalidLength, Safe85.WriteLengthField(1, encodedData, -1));
		Assert.Equal(Safe85.Safe85Status.ErrorInvalidLength, Safe85.EncodeFeed(ref srcIndex, decodedData, -1, ref dstIndex, encodedData, 1, false));
		Assert.Equal(Safe85.Safe85Status.ErrorInvalidLength, Safe85.EncodeFeed(ref srcIndex, decodedData, 1, ref dstIndex, encodedData, -1, false));
	}
	[Fact]
	public void TestEncodeDstPacketed()
	{
		AssertChunkedEncodeDstPacketed(163);
		AssertChunkedEncodeDstPacketed(10);
		AssertChunkedEncodeDstPacketed(200);
	}
	[Fact]
	public void TestEncodeSrcPacketed()
	{
		AssertChunkedEncodeSrcPacketed(131);
		AssertChunkedEncodeSrcPacketed(15);
		AssertChunkedEncodeSrcPacketed(230);
	}
	[Fact]
	public void TestDecodeDstPacketed()
	{
		AssertChunkedDecodeDstPacketed(102);
		AssertChunkedDecodeDstPacketed(20);
		AssertChunkedDecodeDstPacketed(250);
	}
	#region Helper methods
	private void AssertEncodeDecode(string expectedEncoded, byte[] expectedDecoded)
	{
		long expectedEncodedLength = expectedEncoded.Length,
			actualEncodedLength = Safe85.GetEncodedLength(expectedDecoded.Length, false);
		Assert.Equal(expectedEncodedLength, actualEncodedLength);
		byte[] encodeBuffer = new byte[1000];
		long actualEncodeUsedBytes = Safe85.Encode(expectedDecoded, expectedDecoded.Length, encodeBuffer, encodeBuffer.Length);
		Assert.Equal(expectedEncodedLength, actualEncodeUsedBytes);
		string actualEncoded = System.Text.Encoding.ASCII.GetString(encodeBuffer, 0, (int)actualEncodeUsedBytes);
		Assert.Equal(expectedEncoded, actualEncoded);
		long expectedDecodedLength = expectedDecoded.Length,
			actualDecodedLength = Safe85.GetDecodedLength(expectedEncoded.Length);
		Assert.Equal(expectedDecodedLength, actualDecodedLength);
		byte[] decodeBuffer = new byte[1000];
		long actualDecodeUsedBytes = Safe85.Decode(System.Text.Encoding.ASCII.GetBytes(expectedEncoded), expectedEncoded.Length, decodeBuffer, decodeBuffer.Length);
		Assert.Equal(expectedDecodedLength, actualDecodeUsedBytes);
		byte[] actualDecoded = new byte[actualDecodeUsedBytes];
		Array.Copy(decodeBuffer, actualDecoded, actualDecodeUsedBytes);
		Assert.Equal(expectedDecoded, actualDecoded);
	}
	private void AssertEncodeDecodeWithLength(string expectedEncoded, byte[] expectedDecoded)
	{
		byte[] encodeBuffer = new byte[1000];
		long actualEncodeUsedBytes = Safe85.EncodeWithLength(expectedDecoded, expectedDecoded.Length, encodeBuffer, encodeBuffer.Length);
		Assert.True(actualEncodeUsedBytes > 0);
		string actualEncoded = System.Text.Encoding.ASCII.GetString(encodeBuffer, 0, (int)actualEncodeUsedBytes);
		Assert.Equal(expectedEncoded, actualEncoded);
		byte[] decodeBuffer = new byte[1000];
		long actualDecodeUsedBytes = Safe85.DecodeWithLength(System.Text.Encoding.ASCII.GetBytes(expectedEncoded), expectedEncoded.Length, decodeBuffer, decodeBuffer.Length);
		Assert.True(actualDecodeUsedBytes > 0);
		byte[] actualDecoded = new byte[actualDecodeUsedBytes];
		Array.Copy(decodeBuffer, actualDecoded, actualDecodeUsedBytes);
		Assert.Equal(expectedDecoded, actualDecoded);
	}
	private void AssertDecodeStatus(int bufferSize, string encoded, long expectedStatusCode)
	{
		byte[] decodeBuffer = new byte[bufferSize];
		long actualStatusCode = Safe85.Decode(System.Text.Encoding.ASCII.GetBytes(encoded), encoded.Length, decodeBuffer, decodeBuffer.Length);
		Assert.Equal(expectedStatusCode, actualStatusCode);
	}
	private void AssertDecode(string expectedEncoded, byte[] expectedDecoded)
	{
		long decodedLength = Safe85.GetDecodedLength(expectedEncoded.Length);
		Assert.True(decodedLength >= 0);
		byte[] decodeBuffer = new byte[decodedLength];
		long actualDecodeUsedBytes = Safe85.Decode(System.Text.Encoding.ASCII.GetBytes(expectedEncoded), expectedEncoded.Length, decodeBuffer, decodeBuffer.Length);
		Assert.True(actualDecodeUsedBytes >= 1);
		byte[] actualDecoded = new byte[actualDecodeUsedBytes];
		Array.Copy(decodeBuffer, actualDecoded, actualDecodeUsedBytes);
		Assert.Equal(expectedDecoded, actualDecoded);
	}
	private void AssertEncodeLength(long length, string expected)
	{
		byte[] encodeBuffer = new byte[100];
		long bytesWritten = Safe85.WriteLengthField(length, encodeBuffer, encodeBuffer.Length);
		Assert.True(bytesWritten > 0);
		string actual = System.Text.Encoding.ASCII.GetString(encodeBuffer, 0, (int)bytesWritten);
		Assert.Equal(expected, actual);
	}
	private void AssertEncodeLengthStatus(long length, int bufferSize, long expectedStatus)
	{
		byte[] encodeBuffer = new byte[bufferSize];
		long actualStatus = Safe85.WriteLengthField(length, encodeBuffer, encodeBuffer.Length);
		Assert.Equal(expectedStatus, actualStatus);
	}
	private void AssertDecodeLength(string encoded, long expectedLength, long expectedStatus)
	{
		long actualStatus = Safe85.ReadLengthField(System.Text.Encoding.ASCII.GetBytes(encoded), encoded.Length, out long actualLength);
		Assert.Equal(expectedStatus, actualStatus);
		if (expectedStatus >= 0)
			Assert.Equal(expectedLength, actualLength);
	}
	private void AssertDecodeWithLengthStatus(string encoded, long forceLength, long expectedStatus)
	{
		if (forceLength < 0)
			forceLength = encoded.Length;
		byte[] decodeBuffer = new byte[1000];
		long actualDecodeUsedBytes = Safe85.DecodeWithLength(System.Text.Encoding.ASCII.GetBytes(encoded), forceLength, decodeBuffer, decodeBuffer.Length);
		Assert.Equal(expectedStatus, actualDecodeUsedBytes);
	}
	private void AssertChunkedEncodeDstPacketed(int length)
	{
		byte[] data = MakeBytes(length, length),
			encodeBuffer = new byte[length * 2],
			decodeBuffer = new byte[length];
		for (int packetSize = length - 1; packetSize >= ChunksPerGroup; packetSize--)
		{
			Safe85.Safe85Status status = Safe85.Safe85Status.Ok;
			int eSrcIndex = 0,
				eDstIndex = 0,
				dSrcIndex = 0,
				dDstIndex = 0;
			while (eSrcIndex < data.Length)
			{
				bool isEnd = eSrcIndex + packetSize >= data.Length;
				Safe85.Safe85StreamState streamState = isEnd ? Safe85.Safe85StreamState.SrcIsAtEndOfStream : Safe85.Safe85StreamState.None;
				status = Safe85.EncodeFeed(ref eSrcIndex, data, data.Length, ref eDstIndex, encodeBuffer, packetSize, isEnd);
				Assert.True(status == Safe85.Safe85Status.Ok || status == Safe85.Safe85Status.PartiallyComplete);
				status = Safe85.DecodeFeed(ref dSrcIndex, encodeBuffer, eDstIndex, ref dDstIndex, decodeBuffer, decodeBuffer.Length, streamState);
				Assert.True(status == Safe85.Safe85Status.Ok || status == Safe85.Safe85Status.PartiallyComplete);
			}
			Assert.Equal(Safe85.Safe85Status.Ok, status);
			Assert.Equal(data, decodeBuffer.Take(length).ToArray());
		}
	}
	private void AssertChunkedEncodeSrcPacketed(int length)
	{
		byte[] data = MakeBytes(length, length),
			encodeBuffer = new byte[length * 2],
			decodeBuffer = new byte[length];
		for (int packetSize = length - 1; packetSize >= BytesPerGroup; packetSize--)
		{
			Safe85.Safe85Status status = Safe85.Safe85Status.Ok;
			int eSrcIndex = 0,
				eDstIndex = 0,
				dSrcIndex = 0,
				dDstIndex = 0;
			while (eSrcIndex < data.Length)
			{
				bool isEnd = false;
				Safe85.Safe85StreamState streamState = Safe85.Safe85StreamState.None;
				int encodeByteCount = Math.Min(packetSize, data.Length - eSrcIndex);
				if (encodeByteCount == data.Length - eSrcIndex)
				{
					streamState = Safe85.Safe85StreamState.SrcIsAtEndOfStream;
					isEnd = true;
				}
				status = Safe85.EncodeFeed(ref eSrcIndex, data, encodeByteCount, ref eDstIndex, encodeBuffer, encodeBuffer.Length, isEnd);
				Assert.Equal(Safe85.Safe85Status.Ok, status);
				status = Safe85.DecodeFeed(ref dSrcIndex, encodeBuffer, eDstIndex, ref dDstIndex, decodeBuffer, decodeBuffer.Length, streamState);
				Assert.True(status == Safe85.Safe85Status.Ok || status == Safe85.Safe85Status.PartiallyComplete);
			}
			Assert.Equal(Safe85.Safe85Status.Ok, status);
			Assert.Equal(data, decodeBuffer.Take(length).ToArray());
		}
	}
	private void AssertChunkedDecodeDstPacketed(int length)
	{
		byte[] data = MakeBytes(length, length),
			encodeBuffer = new byte[length * 2],
			decodeBuffer = new byte[length];
		long encodedLength = Safe85.Encode(data, data.Length, encodeBuffer, encodeBuffer.Length);
		Assert.True(encodedLength > 0);
		for (int packetSize = length - 1; packetSize >= ChunksPerGroup; packetSize--)
		{
			int dSrcIndex = 0,
				dDstIndex = 0;
			while (dSrcIndex < encodedLength)
			{
				int dSrcEnd = (int)Math.Min(dSrcIndex + packetSize, encodedLength);
				Safe85.Safe85StreamState streamState = Safe85.Safe85StreamState.None;
				if (dSrcEnd >= encodedLength)
					streamState = Safe85.Safe85StreamState.ExpectDstStreamToEnd | Safe85.Safe85StreamState.DstIsAtEndOfStream;
				Safe85.Safe85Status status = Safe85.DecodeFeed(ref dSrcIndex, encodeBuffer, dSrcEnd, ref dDstIndex, decodeBuffer, decodeBuffer.Length, streamState);
				Assert.True(status == Safe85.Safe85Status.Ok || status == Safe85.Safe85Status.PartiallyComplete);
			}
			Assert.Equal(data, decodeBuffer.Take(length).ToArray());
		}
	}
	private byte[] MakeBytes(int length, int startValue)
	{
		byte[] result = new byte[length];
		for (int i = 0; i < length; i++)
			result[i] = (byte)((startValue + i) & 0xff);
		return result;
	}
	#endregion Helper methods
	#region Specification Examples
	[Theory]
	[InlineData("9F3{+RVCLI9LDzZ!4e", new byte[] { 0x39, 0x12, 0x82, 0xe1, 0x81, 0x39, 0xd9, 0x8b, 0x39, 0x4c, 0x63, 0x9d, 0x04, 0x8c })]
	[InlineData("szEXiyl02C!Tc2o.w;X", new byte[] { 0xe6, 0x12, 0xa6, 0x9f, 0xf8, 0x38, 0x6d, 0x7b, 0x01, 0x99, 0x3e, 0x6c, 0x53, 0x7b, 0x60 })]
	[InlineData("1stg+1r5~+MKP7zkj0X2", new byte[] { 0x21, 0xd1, 0x7d, 0x3f, 0x21, 0xc1, 0x88, 0x99, 0x71, 0x45, 0x96, 0xad, 0xcc, 0x96, 0x79, 0xd8 })]
	public void TestSpecificationExamples(string encoded, byte[] decoded) =>
		AssertEncodeDecode(encoded, decoded);
	[Theory]
	[InlineData("J$1ja=a;71mK1lIG[I+9|Mh81U!_X!`XYRvJ]as!._(W", new byte[] { 0x21, 0x7b, 0x01, 0x99, 0x3e, 0xd1, 0x7d, 0x3f, 0x21, 0x8b, 0x39, 0x4c, 0x63, 0xc1, 0x88, 0x21, 0xc1, 0x88, 0x99, 0x71, 0xa6, 0x9f, 0xf8, 0x45, 0x96, 0xe1, 0x81, 0x39, 0xad, 0xcc, 0x96, 0x79, 0xd8 })]
	public void TestSpecificationExampleWithLength(string encoded, byte[] decoded) =>
		AssertEncodeDecodeWithLength(encoded, decoded);
	#endregion Specification Examples
	#region Streams
	[Fact]
	public void TestEncodeStream()
	{
		byte[] input = [0x39, 0x12, 0x82, 0xe1, 0x81, 0x39, 0xd9, 0x8b, 0x39, 0x4c, 0x63, 0x9d, 0x04, 0x8c];
		string expected = "9F3{+RVCLI9LDzZ!4e";
		using MemoryStream inputStream = new(input);
		using MemoryStream outputStream = new();
		Safe85.EncodeStream(inputStream, outputStream, true);
		string result = Encoding.ASCII.GetString(outputStream.ToArray());
		Assert.Equal(expected, result);
	}
	[Fact]
	public void TestDecodeStream()
	{
		string input = "9F3{+RVCLI9LDzZ!4e";
		byte[] expected = [0x39, 0x12, 0x82, 0xe1, 0x81, 0x39, 0xd9, 0x8b, 0x39, 0x4c, 0x63, 0x9d, 0x04, 0x8c];
		using MemoryStream inputStream = new(Encoding.ASCII.GetBytes(input));
		using MemoryStream outputStream = new();
		Safe85.DecodeStream(inputStream, outputStream, Safe85.Safe85StreamState.SrcIsAtEndOfStream);
		byte[] result = outputStream.ToArray();
		Assert.Equal(expected, result);
	}
	[Fact]
	public void TestEncodeDecodeStreamWithChunks()
	{
		byte[] originalData = new byte[1000];
		new Random().NextBytes(originalData);
		using MemoryStream encodedStream = new();
		// Encode in chunks
		for (int i = 0; i < originalData.Length; i += 100)
		{
			int chunkSize = Math.Min(100, originalData.Length - i);
			using MemoryStream inputChunk = new(originalData, i, chunkSize);
			Safe85.EncodeStream(inputChunk, encodedStream, i + chunkSize >= originalData.Length);
		}
		// Decode in chunks
		using MemoryStream decodedStream = new();
		encodedStream.Position = 0;
		byte[] buffer = new byte[120];
		int bytesRead;
		while ((bytesRead = encodedStream.Read(buffer, 0, buffer.Length)) > 0)
		{
			using MemoryStream inputChunk = new(buffer, 0, bytesRead);
			Safe85.DecodeStream(inputChunk, decodedStream, encodedStream.Position == encodedStream.Length ? Safe85.Safe85StreamState.SrcIsAtEndOfStream : Safe85.Safe85StreamState.None);
		}
		Assert.Equal(originalData, decodedStream.ToArray());
	}
	#endregion Streams
}
