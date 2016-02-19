using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MinecraftManager
{
    public static class StringExtensions
    {
        public static string SubstringAfter(this string s, string indexText)
        {
            return s.Substring(s.IndexOf(indexText) + indexText.Length);
        }

        /// <summary>
        /// Preserves the split characters as part of the previous item
        /// </summary>
        /// <param name="fullText"></param>
        /// <returns></returns>
        public static IEnumerable<string> SplitLinesPreserving(this string fullText)
        {
            var remainder = fullText;
            while (remainder.Length > 0)
            {
                if (remainder.Contains('\r') == false && remainder.Contains('\n') == false)
                {
                    yield return remainder;
                    remainder = string.Empty;
                }
                else
                {
                    var seperators = new[] { '\r', '\n' };
                    var index = remainder.IndexOfAny(seperators);

                    if (remainder.Length > index + 1 && seperators.Contains(remainder[index + 1]))
                    {
                        //ensure it's not \r\r or \n\n
                        if (remainder[index] != remainder[index + 1])
                            index++;
                    }
                    var item = remainder.Substring(0, index+1);
                    Debug.Assert(item.Length > 0);
                    yield return item;

                    remainder = remainder.Substring(item.Length);
                }
            }
        }

        public static IEnumerable<string> SplitLines(this string fullText)
        {
            foreach (var item in fullText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
                yield return item;
        }

        public static string SubstringBefore(this string s, string indexText)
        {
            return s.Substring(0, s.IndexOf(indexText));
        }

        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        public static bool HasValue(this string s)
        {
            return !string.IsNullOrEmpty(s);
        }

        public static string EnsureEndsWith(this string s, string end)
        {
            if (s.EndsWith(end))
                return s;
            return s + end;
        }
    }
}
