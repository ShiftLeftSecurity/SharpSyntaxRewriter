// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using System.Text.RegularExpressions;

namespace SharpSyntaxRewriter.Extensions
{
    public static class StringExtensionMethods
    {
        public static string WithoutWhiteSpace(this string s)
        {
            return Regex.Replace(s, " ", "");
        }

        public static string WithoutAnySpace(this string s)
        {
            return Regex.Replace(s, "[\n\r\t\f ]", "");
        }

        public static string WithoutLineBreaks(this string s)
        {
            return Regex.Replace(s, "[\n\r]", " ");
        }

        public static string SpaceNormalised(this string s)
        {
            return Regex.Replace(s, @"\s+", " ");
        }
    }
}
