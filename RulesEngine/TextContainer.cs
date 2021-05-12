﻿// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    ///     Class to handle text as a searchable container
    /// </summary>
    public class TextContainer
    {
        public List<int> LineEnds;

        /// <summary>
        ///     Creates new instance
        /// </summary>
        /// <param name="content"> Text to work with </param>
        /// <param name="language"> The language of the test </param>
        /// <param name="lineNumber"> The line number to specify. Leave empty for full file as target. </param>
        public TextContainer(string content, string language, int lineNumber = 0)
        {
            Language = language;
            LineNumber = lineNumber;
            FullContent = content;
            Target = LineNumber == 0 ? FullContent : GetLineContent(lineNumber);
            LineEnds = new List<int>() { 0 };
            LineStarts = new List<int>() { 0, 0 };

            // Find line end in the text
            int pos = 0;
            while (pos > -1 && pos < FullContent.Length)
            {
                if (++pos < FullContent.Length)
                {
                    pos = FullContent.IndexOf("\n", pos, StringComparison.InvariantCultureIgnoreCase);
                    LineEnds.Add(pos);
                    if (pos > 0 && pos + 1 < FullContent.Length)
                    {
                        LineStarts.Add(pos + 1);
                    }
                }
            }

            // Text can end with \n or not
            if (LineEnds[LineEnds.Count - 1] == -1)
                LineEnds[LineEnds.Count - 1] = (FullContent.Length > 0) ? FullContent.Length - 1 : 0;

            prefix = RulesEngine.Language.GetCommentPrefix(Language);
            suffix = RulesEngine.Language.GetCommentSuffix(Language);
            inline = RulesEngine.Language.GetCommentInline(Language);
        }

        public string FullContent { get; }
        public string Language { get; }
        public string Line { get; } = "";
        public int LineNumber { get; }
        public List<int> LineStarts { get; }
        public string Target { get; }

        /// <summary>
        ///     Returns the Boundary of a specified line number
        /// </summary>
        /// <param name="lineNumber"> The line number to return the boundary for </param>
        /// <returns> </returns>
        public Boundary GetBoundaryFromLine(int lineNumber)
        {
            Boundary result = new Boundary();

            if (lineNumber >= LineEnds.Count)
            {
                return result;
            }

            // Fine when the line number is 0
            var start = 0;
            if (lineNumber > 0)
            {
                start = LineEnds[lineNumber - 1] + 1;
            }
            result.Index = start;
            result.Length = LineEnds[lineNumber] - result.Index + 1;

            return result;
        }

        public string GetBoundaryText(Boundary capture)
        {
            if (capture is null)
            {
                return string.Empty;
            }
            return FullContent[(Math.Min(FullContent.Length, capture.Index))..(Math.Min(FullContent.Length, capture.Index + capture.Length))];
        }

        /// <summary>
        ///     Returns boundary for a given index in text
        /// </summary>
        /// <param name="index"> Position in text </param>
        /// <returns> Boundary </returns>
        public Boundary GetLineBoundary(int index)
        {
            Boundary result = new Boundary();

            for (int i = 0; i < LineEnds.Count; i++)
            {
                if (LineEnds[i] >= index)
                {
                    result.Index = (i > 0 && LineEnds[i - 1] > 0) ? LineEnds[i - 1] + 1 : 0;
                    result.Length = LineEnds[i] - result.Index + 1;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        ///     Return content of the line
        /// </summary>
        /// <param name="line"> Line number </param>
        /// <returns> Text </returns>
        public string GetLineContent(int line)
        {
            int index = LineEnds[line];
            Boundary bound = GetLineBoundary(index);
            return FullContent.Substring(bound.Index, bound.Length);
        }

        /// <summary>
        ///     Returns location (Line, Column) for given index in text
        /// </summary>
        /// <param name="index"> Position in text </param>
        /// <returns> Location </returns>
        public Location GetLocation(int index)
        {
            Location result = new Location();

            if (index == 0)
            {
                result.Line = 1;
                result.Column = 1;
            }
            else
            {
                for (int i = 0; i < LineEnds.Count; i++)
                {
                    if (LineEnds[i] >= index)
                    {
                        result.Line = i;
                        result.Column = index - LineEnds[i - 1];

                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        ///     Check whether the boundary in a text matches the scope of a search pattern (code, comment etc.)
        /// </summary>
        /// <param name="pattern"> Pattern with scope </param>
        /// <param name="boundary"> Boundary in a text </param>
        /// <param name="text"> Text </param>
        /// <returns> True if boundary is matching the pattern scope </returns>
        public bool ScopeMatch(IEnumerable<PatternScope> patterns, Boundary boundary)
        {
            if (patterns is null)
            {
                return true;
            }
            if (patterns.Contains(PatternScope.All) || string.IsNullOrEmpty(prefix))
                return true;
            bool isInComment = IsBetween(FullContent, boundary.Index, prefix, suffix, inline);

            return !(isInComment && !patterns.Contains(PatternScope.Comment));
        }

        private string inline;
        private string prefix;
        private string suffix;

        /// <summary>
        ///     Return boundary defined by line and its offset
        /// </summary>
        /// <param name="line"> Line number </param>
        /// <param name="offset"> Offset from line number </param>
        /// <returns> Boundary </returns>
        private int BoundaryByLine(int line, int offset)
        {
            int index = line + offset;

            // We need the begining of the line when going up
            if (offset < 0)
                index--;

            if (index < 0)
                index = 0;
            if (index >= LineEnds.Count)
                index = LineEnds.Count - 1;

            return LineEnds[index];
        }

        /// <summary>
        ///     Checks if the index in the string lies between preffix and suffix
        /// </summary>
        /// <param name="text"> Text </param>
        /// <param name="index"> Index to check </param>
        /// <param name="prefix"> Prefix </param>
        /// <param name="suffix"> Suffix </param>
        /// <returns> True if the index is between prefix and suffix </returns>
        private static bool IsBetween(string text, int index, string prefix, string suffix, string inline = "")
        {
            int pinnedIndex = Math.Min(index, text.Length);
            string preText = text[0..pinnedIndex];
            int lastPrefix = FastGetLastIndex(preText, prefix);
            if (lastPrefix >= 0)
            {
                int lastInline = FastGetLastIndex(preText[0..lastPrefix], inline);
                // For example in C#, If this /* is actually commented out by a //
                if (!(lastInline >= 0 && lastInline < lastPrefix && !preText[lastInline..lastPrefix].Contains('\n')))
                {
                    var commentedText = text[lastPrefix..];
                    int nextSuffix = FastGetIndex(commentedText, suffix);

                    // If the index is in between the last prefix before the index and the next suffix after
                    // that prefix Then it is commented out
                    if (lastPrefix + nextSuffix > pinnedIndex)
                        return true;
                }
            }
            if (!string.IsNullOrEmpty(inline))
            {
                int lastInline = FastGetLastIndex(preText, inline, '\n'); // Check the same line for same-line comment marks, stopping if you find a newline
                if (lastInline >= 0)
                {
                    var commentedText = text[lastInline..];
                    int endOfLine = FastGetIndex(commentedText,"\n");//Environment.Newline looks for /r/n which is not guaranteed
                    if (endOfLine < 0)
                    {
                        endOfLine = commentedText.Length - 1;
                    }
                    if (index > lastInline && index < lastInline + endOfLine)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int FastGetIndex(string target, string query, char? cancelOn = null)
        {
            for (int i = 0; i < target.Length - query.Length; i++)
            {
                int offset = 0;
                bool skip = false;
                while (!skip && offset < query.Length)
                {
                    if (target[i + offset].Equals(cancelOn))
                    {
                        skip = true;
                    }
                    else if (!target[i + offset].Equals(query[offset]))
                    {
                        skip = true;
                    }
                    else
                    {
                        offset++;
                    }
                }
                if (!skip)
                {
                    return i;
                }
            }
            return -1;
        }

        private static int FastGetLastIndex(string target, string query, char? cancelOn = null)
        {
            for (int i = target.Length - query.Length; i > 0; i--)
            {
                int offset = 0;
                bool skip = false;
                while(!skip && offset < query.Length)
                {
                    if (target[i + offset].Equals(cancelOn))
                    {
                        skip = true;
                    }
                    else if (!target[i + offset].Equals(query[offset]))
                    {
                        skip = true;
                    }
                    else
                    {
                        offset++;
                    }
                }
                if (!skip)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}