using System;
using System.Text;
using System.Text.RegularExpressions;

namespace JobSearch.Classes.Filter
{
    public class StringMatchFilter : FilterBase
    {
        private string _pattern;
        public string Pattern
        {
            get { return _pattern; }
            set
            {
                _pattern = value;
                _patternAsRegex = null;
            }
        }

        public bool Negative { get; set; }
        public FilterSearchType SearchType { get; set; }
        public string ContentPart { get; set; }

        private static readonly char[] _regexSpecialChars = { '[', ']', '{', '}', '(', ')', '|', '\\', ',', '.', '^', '$', '*', '+', '?', '-' };
        private static readonly char[] _maskSpecialChars = { '\\', '*', '?' };

        public StringMatchFilter(string pattern, bool negative, string contentPart, FilterPermission permission, FilterSearchType searchType)
            :base(permission)
        {
            SearchType = searchType;
            Pattern = pattern;
            ContentPart = contentPart;
            Negative = negative;
        }

        private Regex _patternAsRegex;
        private Regex PatternAsRegex
        {
            get { return _patternAsRegex ?? (_patternAsRegex = new Regex(_pattern, RegexOptions.IgnoreCase)); }
        }

        public static string RegexpEncode(string text)
        {
            return specialCharsEncode(_regexSpecialChars, text);
        }

        public static string MaskEncode(string text)
        {
            return specialCharsEncode(_maskSpecialChars, text);
        }

        private static string specialCharsEncode(char[] specialChars, string text)
        {
            var sb = new StringBuilder();
            foreach (var @char in text)
            {
                if (Array.IndexOf(specialChars, @char) >= 0)
                {
                    sb.Append("\\");
                }
                sb.Append(@char);
            }
            return sb.ToString();
        }

        public override bool Match(string text)
        {
            text = text ?? "";
            switch (SearchType)
            {
                case FilterSearchType.Regex:
                    return PatternAsRegex.Match(text).Success;
                case FilterSearchType.Mask:
                    return text.IndexOf(Pattern, StringComparison.CurrentCultureIgnoreCase) >= 0;
                default:
                    return false;
            }
        }
    }
}
