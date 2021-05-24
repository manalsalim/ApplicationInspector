﻿using Microsoft.CST.OAT;
using Microsoft.CST.OAT.Operations;
using Microsoft.CST.OAT.Utils;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// The Custom Operation to enable identification of pattern index in result used by Application Inspector to report why a given
    /// result was matched and to retrieve other pattern level meta-data
    /// </summary>
    public class OATRegexWithIndexOperation : OatOperation
    {
        private readonly ConcurrentDictionary<(string, RegexOptions), Regex?> RegexCache = new();

        /// <summary>
        /// Create an OatOperation given an analyzer
        /// </summary>
        /// <param name="analyzer">The analyzer context to work with</param>
        public OATRegexWithIndexOperation(Analyzer analyzer) : base(Operation.Custom, analyzer)
        {
            CustomOperation = "RegexWithIndex";
            OperationDelegate = RegexWithIndexOperationDelegate;
            ValidationDelegate = RegexWithIndexValidationDelegate;
        }

        public static IEnumerable<Violation> RegexWithIndexValidationDelegate(CST.OAT.Rule rule, Clause clause)
        {
            if (clause.Data?.Count == null || clause.Data?.Count == 0)
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseNoData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
            }
            else if (clause.Data is List<string> regexList)
            {
                foreach (var regex in regexList)
                {
                    if (!Helpers.IsValidRegex(regex))
                    {
                        yield return new Violation(string.Format(Strings.Get("Err_ClauseInvalidRegex"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), regex), rule, clause);
                    }
                }
            }
            if (clause.DictData != null && clause.DictData?.Count > 0)
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseDictDataUnexpected"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), clause.Operation.ToString()), rule, clause);
            }
        }

        /// <summary>
        /// Returns results with pattern index and Boundary as a tuple to enable retrieval of Rule pattern level meta-data like Confidence and report the
        /// pattern that was responsible for the match
        /// </summary>
        /// <param name="clause"></param>
        /// <param name="state1"></param>
        /// <param name="state2"></param>
        /// <param name="captures"></param>
        /// <returns></returns>
        public OperationResult RegexWithIndexOperationDelegate(Clause clause, object? state1, object? state2, IEnumerable<ClauseCapture>? captures)
        {
            if (state1 is TextContainer tc && clause is OATRegexWithIndexClause src && clause.Data is List<string> RegexList && RegexList.Count > 0)
            {
                RegexOptions regexOpts = new();

                if (src.Arguments.Contains("i"))
                {
                    regexOpts |= RegexOptions.IgnoreCase;
                }
                if (src.Arguments.Contains("m"))
                {
                    regexOpts |= RegexOptions.Multiline;
                }

                List<(int, Boundary)> outmatches = new();//tuple results i.e. pattern index and where

                if (Analyzer != null)
                {
                    var regex = StringToRegex(string.Join('|', RegexList), regexOpts);

                    if (regex != null)
                    {
                        foreach (var match in regex.Matches(tc.FullContent))
                        {
                            if (match is Match m)
                            {
                                Boundary translatedBoundary = new()
                                {
                                    Length = m.Length,
                                    Index = m.Index
                                };

                                //regex patterns will be indexed off data while string patterns result in N clauses
                                int patternIndex = Convert.ToInt32(clause.Label);

                                // Should return only scoped matches
                                if (tc.ScopeMatch(src.Scopes, translatedBoundary))
                                {
                                    outmatches.Add((patternIndex, translatedBoundary));
                                }
                            }
                        }
                    }

                    var result = src.Invert ? outmatches.Count == 0 : outmatches.Count > 0;
                    return new OperationResult(result, result && src.Capture ? new TypedClauseCapture<List<(int, Boundary)>>(clause, outmatches, state1) : null);
                }
            }
            return new OperationResult(false, null);
        }
        /// <summary>
        /// Converts a strings to a compiled regex.
        /// Uses an internal cache.
        /// </summary>
        /// <param name="built">The regex to build</param>
        /// <param name="regexOptions">The options to use.</param>
        /// <returns>The built Regex</returns>
        public Regex? StringToRegex(string built, RegexOptions regexOptions)
        {
            if (!RegexCache.ContainsKey((built, regexOptions)))
            {
                try
                {
                    RegexCache.TryAdd((built, regexOptions), new Regex(built, regexOptions));
                }
                catch (ArgumentException)
                {
                    Log.Warning("InvalidArgumentException when creating regex. Regex {0} is invalid and will be skipped.", built);
                    RegexCache.TryAdd((built, regexOptions), null);
                }
            }
            return RegexCache[(built, regexOptions)];
        }
    }
}