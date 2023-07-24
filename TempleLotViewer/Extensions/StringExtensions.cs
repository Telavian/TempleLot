using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace TempleLotViewer.Extensions
{
    public static class StringExtensions
    {
        public static string StringJoin(this IEnumerable<string> items, string separator)
        {
            return string.Join(separator, items);
        }

        public static string StringJoin(this IEnumerable<string> items, char separator)
        {
            return string.Join(separator, items);
        }

        /// <summary>
        /// Limits string to closest word boundary after max length
        /// </summary>
        public static string LimitTo(this string? value, int limit)
        {
            if (value == null) return "";
            if (value.Length < limit) return value;

            var end = value.IndexOf(' ', limit);

            return end == -1
                ? value
                : value.Substring(0, end)
                    .Trim();
        }

        public static bool IsNumber(this string text)
        {
            return int.TryParse(text, out _);
        }

        public static bool IsAlpha(this string text)
        {
            return text
                .All(x => char.IsLetter(x) || char.IsWhiteSpace(x));
        }

        public static bool IsRegexMatch(this string text, string regex)
        {
            return new Regex(regex, RegexOptions.IgnoreCase).IsMatch(text);
        }

        public static bool Matches(this string text, string[] matches, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return matches.Any(x => string.Equals(text, x, comparison));
        }

        public static string ReplaceEntireWord(this string text, string word, string replacement)
        {
            var pattern = $@"\b{Regex.Escape(word)}\b";
            return Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase);
        }

        public static string UnescapeJson(this string text)
        {
            while (true)
            {
                var startLength = text.Length;
                text = text.Replace(@"\""", @"""");

                if (startLength == text.Length)
                {
                    return text
                        .Trim('\\')
                        .Trim('"');
                }
            }
        }

        public static async Task<string> CompressTextAsync(this string text)
        {
            var bytes = Encoding.Unicode.GetBytes(text);
            await using (var input = new MemoryStream(bytes))
            {
                await using (var output = new MemoryStream())
                {
                    await using (var stream = new GZipStream(output, CompressionLevel.Optimal))
                    {

                        await input.CopyToAsync(stream);
                        await stream.FlushAsync();

                        return Convert.ToBase64String(output.ToArray());
                    }
                }
            }
        }

        public static string CompressText(this string text)
        {
            var bytes = Encoding.Unicode.GetBytes(text);
            using (var input = new MemoryStream(bytes))
            {
                using (var output = new MemoryStream())
                {
                    using (var stream = new GZipStream(output, CompressionLevel.Optimal))
                    {

                        input.CopyTo(stream);
                        stream.Flush();

                        return Convert.ToBase64String(output.ToArray());
                    }
                }
            }
        }

        public static async Task<string> DecompressTextAsync(this string text)
        {
            var bytes = Convert.FromBase64String(text);
            await using (var input = new MemoryStream(bytes))
            {
                await using (var output = new MemoryStream())
                {
                    await using (var stream = new GZipStream(input, CompressionMode.Decompress))
                    {
                        await stream.CopyToAsync(output);
                        await output.FlushAsync();

                        return Encoding.Unicode.GetString(output.ToArray());
                    }
                }
            }
        }
    }
}
