using System;
using System.IO;
using System.Text;
using Xunit;

namespace BenVoxel.Test;

public class Safe85Test
{
	[Theory]
	[InlineData("(q", new byte[] { 0xf1 }, false)]
	[InlineData("$(q", new byte[] { 0xf1 }, true)]
	[InlineData("$aF", new byte[] { 0x2e, 0x99 }, false)]
	[InlineData("($aF", new byte[] { 0x2e, 0x99 }, true)]
	[InlineData("Bq|Q", new byte[] { 0xf2, 0x34, 0x56 }, false)]
	[InlineData(")Bq|Q", new byte[] { 0xf2, 0x34, 0x56 }, true)]
	[InlineData("@{743", new byte[] { 0x4a, 0x88, 0xbc, 0xd1 }, false)]
	[InlineData("*@{743", new byte[] { 0x4a, 0x88, 0xbc, 0xd1 }, true)]
	[InlineData("|.Ps^$g", new byte[] { 0xff, 0x71, 0xdd, 0x3a, 0x92 }, false)]
	[InlineData("+|.Ps^$g", new byte[] { 0xff, 0x71, 0xdd, 0x3a, 0x92 }, true)]
	public void TestEncodeDecode(string encoded, byte[] decoded, bool useLength)
	{
		// Test encoding
		string actualEncoded = decoded.EncodeSafe85(useLength);
		Assert.Equal(encoded, actualEncoded);

		// Test decoding
		byte[] actualDecoded = encoded.DecodeSafe85(useLength);
		Assert.Equal(decoded, actualDecoded);
	}

	[Theory]
	[InlineData(" |.Ps^$g", false)]
	[InlineData("| .Ps^$g", false)]
	[InlineData("|. Ps^$g", false)]
	[InlineData("|.P s^$g", false)]
	[InlineData("|.Ps ^$g", false)]
	[InlineData("|.Ps^ $g", false)]
	[InlineData("|.Ps^$ g", false)]
	[InlineData("|.Ps^$g ", false)]
	[InlineData("|\t\t.\r\n\n P   s^\t \t\t$g", false)]
	[InlineData(" +|.Ps^$g", true)]
	[InlineData("+ |.Ps^$g", true)]
	[InlineData("+| .Ps^$g", true)]
	[InlineData("+|. Ps^$g", true)]
	[InlineData("+|.P s^$g", true)]
	[InlineData("+|.Ps ^$g", true)]
	[InlineData("+|.Ps^ $g", true)]
	[InlineData("+|.Ps^$ g", true)]
	[InlineData("+|.Ps^$g ", true)]
	[InlineData("+|\t\t.\r\n\n P   s^\t \t\t$g", true)]
	public void TestDecodeWithWhitespace(string encoded, bool useLength)
	{
		byte[] expected = new byte[] { 0xff, 0x71, 0xdd, 0x3a, 0x92 };
		byte[] actual = encoded.DecodeSafe85(useLength);
		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData("#.Ps^$g", false)]
	[InlineData("|#Ps^$g", false)]
	[InlineData("|.#s^$g", false)]
	[InlineData("|.P#^$g", false)]
	[InlineData("|.Ps#$g", false)]
	[InlineData("|.Ps^#g", false)]
	[InlineData("|.Ps^$#", false)]
	[InlineData("+#.Ps^$g", true)]
	[InlineData("+|#Ps^$g", true)]
	[InlineData("+|.#s^$g", true)]
	[InlineData("+|.P#^$g", true)]
	[InlineData("+|.Ps#$g", true)]
	[InlineData("+|.Ps^#g", true)]
	[InlineData("+|.Ps^$#", true)]
	public void TestDecodeInvalidInput(string encoded, bool useLength)
	{
		Assert.Throws<InvalidDataException>(() => encoded.DecodeSafe85(useLength));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void TestLargeData(bool useLength)
	{
		byte[] largeData = new byte[1000];
		new Random(42).NextBytes(largeData);

		string encoded = largeData.EncodeSafe85(useLength);
		byte[] decoded = encoded.DecodeSafe85(useLength);

		Assert.Equal(largeData, decoded);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void TestStreamEncoding(bool useLength)
	{
		byte[] data = new byte[] { 0x39, 0x12, 0x82, 0xe1, 0x81, 0x39, 0xd9, 0x8b, 0x39, 0x4c, 0x63, 0x9d, 0x04, 0x8c };
		string expected = useLength ? "59F3{+RVCLI9LDzZ!4e" : "9F3{+RVCLI9LDzZ!4e";

		using (MemoryStream input = new MemoryStream(data))
		using (MemoryStream output = new MemoryStream())
		{
			input.EncodeSafe85(output, useLength);
			string actual = Encoding.UTF8.GetString(output.ToArray());
			Assert.Equal(expected, actual);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void TestStreamDecoding(bool useLength)
	{
		string encoded = useLength ? "59F3{+RVCLI9LDzZ!4e" : "9F3{+RVCLI9LDzZ!4e";
		byte[] expected = new byte[] { 0x39, 0x12, 0x82, 0xe1, 0x81, 0x39, 0xd9, 0x8b, 0x39, 0x4c, 0x63, 0x9d, 0x04, 0x8c };

		using (MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(encoded)))
		using (MemoryStream output = new MemoryStream())
		{
			input.DecodeSafe85(output, useLength);
			byte[] actual = output.ToArray();
			Assert.Equal(expected, actual);
		}
	}

	[Theory]
	[InlineData(0, "!", "$")]
	[InlineData(1, "$", "($")]
	[InlineData(10, "1", "11")]
	[InlineData(31, "H", "HH")]
	[InlineData(32, "J!", "J!J!")]
	[InlineData(33, "J$", "J!J$")]
	[InlineData(1023, "iH", "iHiH")]
	[InlineData(1024, "JI!", "JI!JI!")]
	[InlineData(1025, "JI$", "JI!JI$")]
	public void TestLengthEncoding(int length, string expectedWithoutLength, string expectedWithLength)
	{
		byte[] data = new byte[length];
		string encodedWithoutLength = data.EncodeSafe85(false);
		string encodedWithLength = data.EncodeSafe85(true);

		Assert.StartsWith(expectedWithoutLength, encodedWithoutLength);
		Assert.StartsWith(expectedWithLength, encodedWithLength);
	}

	[Theory]
	[InlineData("9F3{+RVCLI9LDzZ!4e", new byte[] { 0x39, 0x12, 0x82, 0xe1, 0x81, 0x39, 0xd9, 0x8b, 0x39, 0x4c, 0x63, 0x9d, 0x04, 0x8c }, false)]
	[InlineData("59F3{+RVCLI9LDzZ!4e", new byte[] { 0x39, 0x12, 0x82, 0xe1, 0x81, 0x39, 0xd9, 0x8b, 0x39, 0x4c, 0x63, 0x9d, 0x04, 0x8c }, true)]
	[InlineData("szEXiyl02C!Tc2o.w;X", new byte[] { 0xe6, 0x12, 0xa6, 0x9f, 0xf8, 0x38, 0x6d, 0x7b, 0x01, 0x99, 0x3e, 0x6c, 0x53, 0x7b, 0x60 }, false)]
	[InlineData("5szEXiyl02C!Tc2o.w;X", new byte[] { 0xe6, 0x12, 0xa6, 0x9f, 0xf8, 0x38, 0x6d, 0x7b, 0x01, 0x99, 0x3e, 0x6c, 0x53, 0x7b, 0x60 }, true)]
	[InlineData("1stg+1r5~+MKP7zkj0X2", new byte[] { 0x21, 0xd1, 0x7d, 0x3f, 0x21, 0xc1, 0x88, 0x99, 0x71, 0x45, 0x96, 0xad, 0xcc, 0x96, 0x79, 0xd8 }, false)]
	[InlineData("61stg+1r5~+MKP7zkj0X2", new byte[] { 0x21, 0xd1, 0x7d, 0x3f, 0x21, 0xc1, 0x88, 0x99, 0x71, 0x45, 0x96, 0xad, 0xcc, 0x96, 0x79, 0xd8 }, true)]
	public void TestSpecificationExamples(string encoded, byte[] decoded, bool useLength)
	{
		// Test decoding
		byte[] actualDecoded = encoded.DecodeSafe85(useLength);
		Assert.Equal(decoded, actualDecoded);

		// Test encoding
		string actualEncoded = decoded.EncodeSafe85(useLength);
		Assert.Equal(encoded, actualEncoded);
	}

	[Fact]
	public void TestSpecificationExampleWithLength()
	{
		string encoded = "J$1ja=a;71mK1lIG[I+9|Mh81U!_X!`XYRvJ]as!._(W";
		byte[] expected = new byte[] { 0x21, 0x7b, 0x01, 0x99, 0x3e, 0xd1, 0x7d, 0x3f, 0x21, 0x8b, 0x39, 0x4c, 0x63, 0xc1, 0x88, 0x21, 0xc1, 0x88, 0x99, 0x71, 0xa6, 0x9f, 0xf8, 0x45, 0x96, 0xe1, 0x81, 0x39, 0xad, 0xcc, 0x96, 0x79, 0xd8 };

		// Test decoding with length
		byte[] actualDecoded = encoded.DecodeSafe85(true);
		Assert.Equal(expected, actualDecoded);

		// Test encoding with length
		string actualEncoded = expected.EncodeSafe85(true);
		Assert.Equal(encoded, actualEncoded);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void TestEmptyInput(bool useLength)
	{
		byte[] emptyInput = Array.Empty<byte>();
		string encoded = emptyInput.EncodeSafe85(useLength);
		byte[] decoded = encoded.DecodeSafe85(useLength);
		Assert.Empty(decoded);
	}

	[Fact]
	public void TestInvalidLengthField()
	{
		string invalidLengthEncoded = "k1ja=a;71mK1lIG[I+9|Mh81U!_X!`XYRvJ]as!._(W"; // 'k' is not a valid length field character
		Assert.Throws<InvalidDataException>(() => invalidLengthEncoded.DecodeSafe85(true));
	}

	[Fact]
	public void TestMismatchedLength()
	{
		string mismatchedLengthEncoded = "J(1ja=a;71mK1lIG[I+9|Mh81U!_X!`XYRvJ]as!._(W"; // Length field says 32 bytes, but data is 33 bytes
		Assert.Throws<InvalidDataException>(() => mismatchedLengthEncoded.DecodeSafe85(true));
	}

	[Theory]
	[InlineData(1_000_000)]
	[InlineData(10_000_000)]
	public void TestLargeLengthField(int length)
	{
		byte[] largeData = new byte[length];
		new Random(42).NextBytes(largeData);

		string encoded = largeData.EncodeSafe85(true);
		byte[] decoded = encoded.DecodeSafe85(true);
		Assert.Equal(largeData, decoded);
	}
	[Fact]
	public void TestExceedMaximumEncodableLength()
	{
		byte[] tooLargeData = new byte[1 << 30]; // One byte over the maximum
		Assert.Throws<ArgumentException>(() => tooLargeData.EncodeSafe85(true));
	}

	[Theory]
	[InlineData("!")]
	[InlineData("$")]
	[InlineData("1")]
	[InlineData("H")]
	public void TestTruncatedLengthField(string truncatedLength)
	{
		Assert.Throws<InvalidDataException>(() => truncatedLength.DecodeSafe85(true));
	}

	[Fact]
	public void TestTruncatedData()
	{
		string truncatedData = "J$1ja=a;71mK1"; // Length field indicates more data than provided
		Assert.Throws<InvalidDataException>(() => truncatedData.DecodeSafe85(true));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void TestStreamEncodingWithLargeInput(bool useLength)
	{
		const int largeSize = 1_000_000;
		byte[] largeData = new byte[largeSize];
		new Random(42).NextBytes(largeData);

		using (MemoryStream input = new MemoryStream(largeData))
		using (MemoryStream encodedOutput = new MemoryStream())
		using (MemoryStream decodedOutput = new MemoryStream())
		{
			input.EncodeSafe85(encodedOutput, useLength);
			encodedOutput.Position = 0;
			encodedOutput.DecodeSafe85(decodedOutput, useLength);

			Assert.Equal(largeData, decodedOutput.ToArray());
		}
	}

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	public void TestPartialGroupEncoding(int length)
	{
		byte[] data = new byte[length];
		new Random(42).NextBytes(data);

		string encodedWithoutLength = data.EncodeSafe85(false);
		string encodedWithLength = data.EncodeSafe85(true);

		byte[] decodedWithoutLength = encodedWithoutLength.DecodeSafe85(false);
		byte[] decodedWithLength = encodedWithLength.DecodeSafe85(true);

		Assert.Equal(data, decodedWithoutLength);
		Assert.Equal(data, decodedWithLength);
	}

	[Fact]
	public void TestInvalidCharacterInData()
	{
		string invalidData = "J$1ja=a;71mK1lIG[I+9|Mh81U!_X!`XYRvJ]as!._(W%"; // '%' is not in the Safe85 alphabet
		Assert.Throws<InvalidDataException>(() => invalidData.DecodeSafe85(true));
	}

	[Fact]
	public void TestLengthFieldOverflow()
	{
		string overflowLengthField = "JJJJJJ1"; // More than 6 chunks in the length field
		Assert.Throws<InvalidDataException>(() => overflowLengthField.DecodeSafe85(true));
	}
}
