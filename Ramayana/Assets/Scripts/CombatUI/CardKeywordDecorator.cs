// Colours keywords in a card description pattern before its numbers are filled in: every whole-word, case-sensitive
// match of a keyword is wrapped in that keyword's colour tag. Rich-text tags (<...>), placeholders ({0}) and escaped
// braces ({{ and }}) are copied untouched, so a keyword is never matched inside them and the pattern still formats.
// When keywords overlap, the first in the list that matches wins, so the caller lists longer words first. Plain C#
// writing into a caller-owned StringBuilder, EditMode tested, and run only when a pattern, a word or a colour changes.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace JourneyToLanka.CombatUI
{
    public static class CardKeywordDecorator
    {
        public const string CloseTag = "</color>";

        public static void Decorate(string pattern, IReadOnlyList<CardKeyword> keywords, StringBuilder output)
        {
            output.Clear();
            if (string.IsNullOrEmpty(pattern))
            {
                return;
            }

            int length = pattern.Length;
            int i = 0;
            while (i < length)
            {
                char c = pattern[i];
                if (c == '<' && TryCopyThrough(pattern, i, '>', output, out int afterTag))
                {
                    i = afterTag;
                    continue;
                }

                if (c == '{' || c == '}')
                {
                    if (i + 1 < length && pattern[i + 1] == c)
                    {
                        output.Append(c).Append(c);
                        i += 2;
                        continue;
                    }

                    if (c == '{' && TryCopyThrough(pattern, i, '}', output, out int afterPlaceholder))
                    {
                        i = afterPlaceholder;
                        continue;
                    }
                }

                bool wordStart = IsWordChar(c) && (i == 0 || !IsWordChar(pattern[i - 1]));
                if (wordStart && keywords != null && TryMatch(pattern, i, keywords, out CardKeyword keyword))
                {
                    int wordLength = keyword.Word.Length;
                    output.Append(keyword.OpenTag).Append(pattern, i, wordLength).Append(CloseTag);
                    i += wordLength;
                    continue;
                }

                output.Append(c);
                i++;
            }
        }

        private static bool TryCopyThrough(string pattern, int start, char close, StringBuilder output, out int next)
        {
            int end = pattern.IndexOf(close, start + 1);
            if (end < 0)
            {
                next = start;
                return false;
            }

            output.Append(pattern, start, end - start + 1);
            next = end + 1;
            return true;
        }

        private static bool TryMatch(string pattern, int start, IReadOnlyList<CardKeyword> keywords,
            out CardKeyword match)
        {
            for (int k = 0; k < keywords.Count; k++)
            {
                CardKeyword keyword = keywords[k];
                string word = keyword.Word;
                if (string.IsNullOrEmpty(word) || start + word.Length > pattern.Length)
                {
                    continue;
                }

                if (string.Compare(pattern, start, word, 0, word.Length, StringComparison.Ordinal) != 0)
                {
                    continue;
                }

                int end = start + word.Length;
                if (end < pattern.Length && IsWordChar(pattern[end]))
                {
                    continue;
                }

                match = keyword;
                return true;
            }

            match = default;
            return false;
        }

        // Combining marks count as part of a word, so a Devanagari keyword never matches the start of a longer word
        // whose next letter carries a vowel sign.
        private static bool IsWordChar(char c)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                return true;
            }

            UnicodeCategory category = char.GetUnicodeCategory(c);
            return category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark;
        }
    }
}
