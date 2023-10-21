using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace VoxModel
{
	public static class ExtensionMethods
	{
		/// <summary>
		/// Formats a string to the exact length specified, left-padded
		/// </summary>
		/// <param name="string">Input string to be formatted</param>
		/// <param name="length">Length of final string</param>
		/// <param name="paddingChar">Character to pad with if extra padding is needed (default is space)</param>
		/// <returns>String of exact length from length parameter</returns>
		public static string ExactPadLeft(this string @string, int length, char paddingChar = ' ') =>
			 ReplaceEndLines(@string, " ") is string cleaned ?
				 cleaned.Substring(Math.Max(cleaned.Length - length, 0)).PadLeft(length, paddingChar) ?? "".PadLeft(length, paddingChar)
				 : throw new InvalidDataException("Couldn't remove end lines from string!");
		/// <summary>
		/// Formats a string to the exact length specified, right-padded
		/// </summary>
		/// <param name="string">Input string to be formatted</param>
		/// <param name="length">Length of final string</param>
		/// <param name="paddingChar">Character to pad with if extra padding is needed (default is space)</param>
		/// <returns>String of exact length from length parameter</returns>
		public static string ExactPadRight(this string @string, int length, char paddingChar = ' ') =>
			 ReplaceEndLines(@string, " ") is string cleaned ?
				 cleaned.Substring(0, Math.Min(cleaned.Length, length)).PadRight(length, paddingChar) ?? "".PadRight(length, paddingChar)
				 : throw new InvalidDataException("Couldn't remove end lines from string!");
		public static string ExactPadLeft(int[] lengths, params object[] strings) => ExactPadLeft(' ', lengths, strings);
		/// <summary>
		/// Concatonates a series of objects interpreted as left-padded strings where each object gets an exact string length in order to guarantee the lengths and relative positions in the result
		/// </summary>
		/// <param name="paddingChar">Character to pad with if extra padding is needed (default is space)</param>
		/// <param name="lengths">Length of each object string</param>
		/// <param name="strings">Objects to concatonate</param>
		/// <returns>String as long as the sum of lengths</returns>
		public static string ExactPadLeft(char paddingChar, int[] lengths, params object[] strings) => string.Join("", Enumerable.Range(0, lengths.Length).Select(i => (strings[i]?.ToString() ?? "").ExactPadLeft(lengths[i], paddingChar)));
		public static string ExactPadRight(int[] lengths, params object[] strings) => ExactPadRight(' ', lengths, strings);
		/// <summary>
		/// Concatonates a series of objects interpreted as right-padded strings where each object gets an exact string length in order to guarantee the lengths and relative positions in the result
		/// </summary>
		/// <param name="paddingChar">Character to pad with if extra padding is needed (default is space)</param>
		/// <param name="lengths">Length of each object string</param>
		/// <param name="strings">Objects to concatonate</param>
		/// <returns>String as long as the sum of lengths</returns>
		public static string ExactPadRight(char paddingChar, int[] lengths, params object[] strings) => string.Join("", Enumerable.Range(0, lengths.Length).Select(i => (strings[i]?.ToString() ?? "").ExactPadRight(lengths[i], paddingChar)));
		/// <summary>
		/// Shows an amount of money without the decimal point
		/// </summary>
		/// <param name="decimal">Money amount</param>
		/// <returns>Rounded to two decimal places without the decimal point</returns>
		public static string NoDecimal(this decimal @decimal) => Truncate(Math.Abs(@decimal), 2).ToString("F2").Replace(".", string.Empty).TrimStart('0');
		/// <summary>
		/// Shows an amount of money without the decimal point, padded or limited to the specified length
		/// </summary>
		/// <param name="decimal">Money amount</param>
		/// <param name="length">Length to pad or limit to</param>
		/// <returns>Rounded to two decimal places without the decimal point padded or limited to the specified length</returns>
		public static string NoDecimal(this decimal @decimal, int length) => @decimal.NoDecimal().ExactPadLeft(length, '0');
		/// <summary>
		/// NoDecimal, but show a zero in case of zero
		/// </summary>
		public static string NoDecimalWithZero(this decimal @decimal)
		{
			string noDecimal = @decimal.NoDecimal();
			return string.IsNullOrWhiteSpace(noDecimal) ? "0" : noDecimal;
		}
		/// <summary>
		/// NoDecimal, but show a dash in the leftmost column for negative numbers
		/// </summary>
		public static string NoDecimalSigned(this decimal @decimal, int length = 0) =>
			length > 0 ?
				@decimal < 0m ?
					"-" + @decimal.NoDecimal().ExactPadLeft(length - 1, '0')
					: @decimal.NoDecimal().ExactPadLeft(length, '0')
				: @decimal < 0m ?
					"-" + @decimal.NoDecimalWithZero()
					: @decimal.NoDecimalWithZero();
		/// <summary>
		/// Truncates a decimal value to the specified number of decimal places without rounding
		/// </summary>
		/// <param name="decimal">Value to truncate. Default is 2.</param>
		/// <param name="places">Number of decimal places to truncate to</param>
		/// <returns>Truncated decimal value</returns>
		public static decimal Truncate(this decimal @decimal, byte places = 2)
		{
			decimal rounded = Math.Round(@decimal, places);
			return @decimal > 0 && rounded > @decimal ?
				rounded - new decimal(1, 0, 0, false, places)
				: @decimal < 0 && rounded < @decimal ?
					rounded + new decimal(1, 0, 0, false, places)
					: rounded;
		}
		public static string RemoveEndLines(this string @string) => ReplaceEndLines(@string, string.Empty);
		public static string ReplaceEndLines(this string @string, string replaceWith) => Regex.Replace(@string, @"[\u000A\u000B\u000C\u000D\u2028\u2029\u0085]+", string.IsNullOrEmpty(replaceWith) ? string.Empty : replaceWith);
	}
}
