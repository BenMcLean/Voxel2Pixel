using System.Text;

namespace BenVoxel.Test;

public class Safe85Test
{
	[Theory]
	[InlineData("(q", new byte[] { 0xf1 })]
	[InlineData("$aF", new byte[] { 0x2e, 0x99 })]
	[InlineData("Bq|Q", new byte[] { 0xf2, 0x34, 0x56 })]
	[InlineData("@{743", new byte[] { 0x4a, 0x88, 0xbc, 0xd1 })]
	[InlineData("|.Ps^$g", new byte[] { 0xff, 0x71, 0xdd, 0x3a, 0x92 })]
	public void TestEncodeDecode(string encoded, byte[] decoded)
	{
		// Test encoding
		string actualEncoded = decoded.EncodeSafe85();
		Assert.Equal(encoded, actualEncoded);

		// Test decoding
		byte[] actualDecoded = encoded.DecodeSafe85();
		Assert.Equal(decoded, actualDecoded);
	}

	[Theory]
	[InlineData("$(q", new byte[] { 0xf1 })]
	[InlineData("($aF", new byte[] { 0x2e, 0x99 })]
	[InlineData(")Bq|Q", new byte[] { 0xf2, 0x34, 0x56 })]
	[InlineData("*@{743", new byte[] { 0x4a, 0x88, 0xbc, 0xd1 })]
	[InlineData("+|.Ps^$g", new byte[] { 0xff, 0x71, 0xdd, 0x3a, 0x92 })]
	public void TestEncodeDecodeWithLength(string encoded, byte[] decoded)
	{
		// Test encoding with length
		string actualEncoded = decoded.EncodeSafe85(true);
		Assert.Equal(encoded, actualEncoded);

		// Test decoding with length
		byte[] actualDecoded = encoded.DecodeSafe85(true);
		Assert.Equal(decoded, actualDecoded);
	}

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
	public void TestDecodeWithWhitespace(string encoded)
	{
		byte[] expected = new byte[] { 0xff, 0x71, 0xdd, 0x3a, 0x92 };
		byte[] actual = encoded.DecodeSafe85();
		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData("#.Ps^$g")]
	[InlineData("|#Ps^$g")]
	[InlineData("|.#s^$g")]
	[InlineData("|.P#^$g")]
	[InlineData("|.Ps#$g")]
	[InlineData("|.Ps^#g")]
	[InlineData("|.Ps^$#")]
	public void TestDecodeInvalidInput(string encoded)
	{
		Assert.Throws<InvalidDataException>(() => encoded.DecodeSafe85());
	}

	[Fact]
	public void TestLargeData()
	{
		byte[] largeData = new byte[1000];
		new Random(42).NextBytes(largeData);

		string encoded = largeData.EncodeSafe85();
		byte[] decoded = encoded.DecodeSafe85();

		Assert.Equal(largeData, decoded);
	}

	[Fact]
	public void TestStreamEncoding()
	{
		byte[] data = new byte[] { 0x39, 0x12, 0x82, 0xe1, 0x81, 0x39, 0xd9, 0x8b, 0x39, 0x4c, 0x63, 0x9d, 0x04, 0x8c };
		string expected = "9F3{+RVCLI9LDzZ!4e";

		using (MemoryStream input = new MemoryStream(data))
		using (MemoryStream output = new MemoryStream())
		{
			input.EncodeSafe85(output);
			string actual = Encoding.UTF8.GetString(output.ToArray());
			Assert.Equal(expected, actual);
		}
	}

	[Fact]
	public void TestStreamDecoding()
	{
		string encoded = "9F3{+RVCLI9LDzZ!4e";
		byte[] expected = new byte[] { 0x39, 0x12, 0x82, 0xe1, 0x81, 0x39, 0xd9, 0x8b, 0x39, 0x4c, 0x63, 0x9d, 0x04, 0x8c };

		using (MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(encoded)))
		using (MemoryStream output = new MemoryStream())
		{
			input.DecodeSafe85(output);
			byte[] actual = output.ToArray();
			Assert.Equal(expected, actual);
		}
	}

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
	public void TestLengthEncoding(int length, string expected)
	{
		byte[] data = new byte[length];
		string encoded = data.EncodeSafe85(true);
		Assert.StartsWith(expected, encoded);
	}

	[Theory]
	[InlineData("9F3{+RVCLI9LDzZ!4e", new byte[] { 0x39, 0x12, 0x82, 0xe1, 0x81, 0x39, 0xd9, 0x8b, 0x39, 0x4c, 0x63, 0x9d, 0x04, 0x8c })]
	[InlineData("szEXiyl02C!Tc2o.w;X", new byte[] { 0xe6, 0x12, 0xa6, 0x9f, 0xf8, 0x38, 0x6d, 0x7b, 0x01, 0x99, 0x3e, 0x6c, 0x53, 0x7b, 0x60 })]
	[InlineData("1stg+1r5~+MKP7zkj0X2", new byte[] { 0x21, 0xd1, 0x7d, 0x3f, 0x21, 0xc1, 0x88, 0x99, 0x71, 0x45, 0x96, 0xad, 0xcc, 0x96, 0x79, 0xd8 })]
	public void TestSpecificationExamples(string encoded, byte[] decoded)
	{
		// Test decoding
		byte[] actualDecoded = encoded.DecodeSafe85();
		Assert.Equal(decoded, actualDecoded);

		// Test encoding
		string actualEncoded = decoded.EncodeSafe85();
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
}
