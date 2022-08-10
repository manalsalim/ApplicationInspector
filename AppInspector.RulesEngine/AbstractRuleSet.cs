﻿// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.CST.OAT;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    ///     Base class for a set of <see cref="Rule"/> objects to be operated on by the <see cref="RuleProcessor"/>. This is the abstract class used to allow for the default <see cref="RuleSet"/> and <see cref="TypedRuleSet{T}"/> for use with rules that have extra properties.
    /// </summary>
    public abstract class AbstractRuleSet
    {
        protected ILogger _logger = NullLogger.Instance;
        protected readonly List<ConvertedOatRule> _oatRules = new();
        protected IEnumerable<Rule> _rules { get => _oatRules.Select(x => x.AppInspectorRule); }
        private readonly Regex _searchInRegex = new("\\((.*),(.*)\\)", RegexOptions.Compiled);

        /// <summary>
        ///     Filters rules within Ruleset by language
        /// </summary>
        /// <param name="language"></param>
        /// <returns> Filtered rules </returns>
        public IEnumerable<ConvertedOatRule> ByLanguage(string language)
        {
            if (!string.IsNullOrEmpty(language))
            {
                return _oatRules.Where(x => x.AppInspectorRule.AppliesTo is { } appliesList && appliesList.Contains(language));
            }
            return Array.Empty<ConvertedOatRule>();
        }

        /// <summary>
        ///     Filters rules within Ruleset filename
        /// </summary>
        /// <param name="input"></param>
        /// <returns> Filtered rules </returns>
        public IEnumerable<ConvertedOatRule> ByFilename(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                return _oatRules.Where(x => x.AppInspectorRule.CompiledFileRegexes.Any(y => y.IsMatch(input)));
            }
            return Array.Empty<ConvertedOatRule>();
        }

        /// <summary>
        /// Get the set of rules that apply to all files
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ConvertedOatRule> GetUniversalRules()
        {
            return _oatRules.Where(x => (x.AppInspectorRule.FileRegexes is null || x.AppInspectorRule.FileRegexes.Length == 0) && (x.AppInspectorRule.AppliesTo is null || x.AppInspectorRule.AppliesTo.Length == 0));
        }

        /// <summary>
        ///     Convert an AppInspector rule into an OAT rule.
        /// </summary>
        /// <param name="rule">The <see cref="Rule"/> to convert.</param>
        /// <returns>A <see cref="ConvertedOatRule"/> if the AI rule was valid otherwise null.</returns>
        public ConvertedOatRule? AppInspectorRuleToOatRule(Rule rule)
        {
            var clauses = new List<Clause>();
            int clauseNumber = 0;
            var expression = new StringBuilder("(");
            foreach (var pattern in rule.Patterns)
            {
                if (GenerateClause(pattern, clauseNumber) is { } clause)
                {
                    clauses.Add(clause);
                    if (clauseNumber > 0)
                    {
                        expression.Append(" OR ");
                    }
                    expression.Append(clauseNumber);
                    clauseNumber++;
                }
                else
                {
                    _logger.LogWarning("Clause could not be generated from pattern {pattern}", pattern.Pattern);
                }
            }

            if (clauses.Count > 0)
            {
                expression.Append(')');
            }
            else
            {
                return new ConvertedOatRule(rule.Id, rule);
            }

            foreach (var condition in rule.Conditions ?? Array.Empty<SearchCondition>())
            {
                Clause? clause = GenerateCondition(condition, clauseNumber);
                if (clause is { })
                {
                    clauses.Add(clause);
                    expression.Append(" AND ");
                    expression.Append(clauseNumber);
                    clauseNumber++; 
                }
            }
            return new ConvertedOatRule(rule.Id, rule)
            {
                Clauses = clauses,
                Expression = expression.ToString()
            };
        }

        private Clause? GenerateCondition(SearchCondition condition, int clauseNumber)
        {
            if (condition.Pattern is {} conditionPattern)
            {
                var subClause = GenerateClause(conditionPattern);
                if (subClause is null)
                {
                    _logger.LogWarning("SubClause for condition could not be generated");
                }
                else
                {
                    if (condition.SearchIn?.Equals("finding-only", StringComparison.InvariantCultureIgnoreCase) != false)
                    {
                        return new WithinClause(subClause)
                        {
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            FindingOnly = true,
                            Invert = condition.NegateFinding
                        };
                    }
                    else if (condition.SearchIn.StartsWith("finding-region", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var argList = new List<int>();
                        Match m = _searchInRegex.Match(condition.SearchIn);
                        if (m.Success)
                        {
                            for (int i = 1; i < m.Groups.Count; i++)
                            {
                                if (int.TryParse(m.Groups[i].Value, out int value))
                                {
                                    argList.Add(value);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        if (argList.Count == 2)
                        {
                            return new WithinClause(subClause)
                            {
                                Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                                FindingRegion = true,
                                Before = argList[0],
                                After = argList[1],
                                Invert = condition.NegateFinding
                            };
                        }
                    }
                    else if (condition.SearchIn.Equals("same-line", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return new WithinClause(subClause)
                        {
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            SameLineOnly = true,
                            Invert = condition.NegateFinding
                        };
                    }
                    else if (condition.SearchIn.Equals("same-file", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return new WithinClause(subClause)
                        {
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            SameFile = true,
                            Invert = condition.NegateFinding
                        };
                    }
                    else if (condition.SearchIn.Equals("only-before", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return new WithinClause(subClause)
                        {
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            OnlyBefore = true,
                            Invert = condition.NegateFinding
                        };
                    }
                    else if (condition.SearchIn.Equals("only-after", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return new WithinClause(subClause)
                        {
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            OnlyAfter = true,
                            Invert = condition.NegateFinding
                        };
                    }
                    else
                    {
                        _logger.LogWarning("Search condition {Condition} is not one of the accepted values and this condition will be ignored", condition.SearchIn);
                    }
                }
                
            }
            return null;
        }

        private Clause? GenerateClause(SearchPattern pattern, int clauseNumber = -1)
        {
            if (pattern.Pattern != null)
            {
                var scopes = pattern.Scopes ?? new PatternScope[] { PatternScope.All };
                var modifiers = pattern.Modifiers?.ToList() ?? new List<string>();
                if (pattern.PatternType is PatternType.String or PatternType.Substring)
                {
                    return new OatSubstringIndexClause(scopes, useWordBoundaries: pattern.PatternType == PatternType.String, xPaths: pattern.XPaths, jsonPaths:pattern.JsonPaths)
                    {
                        Label = clauseNumber.ToString(CultureInfo.InvariantCulture),//important to pattern index identification
                        Data = new List<string>() { pattern.Pattern },
                        Capture = true,
                        Arguments = pattern.Modifiers?.ToList() ?? new List<string>()
                    };
                }
                else if (pattern.PatternType == PatternType.Regex)
                {
                    return new OatRegexWithIndexClause(scopes, null, pattern.XPaths, pattern.JsonPaths)
                    {
                        Label = clauseNumber.ToString(CultureInfo
                            .InvariantCulture), //important to pattern index identification
                        Data = new List<string>() { pattern.Pattern },
                        Capture = true,
                        Arguments = modifiers
                    };
                }
                else if (pattern.PatternType == PatternType.RegexWord)
                {
                    return new OatRegexWithIndexClause(scopes, null, pattern.XPaths, pattern.JsonPaths)
                    {
                        Label = clauseNumber.ToString(CultureInfo.InvariantCulture),//important to pattern index identification
                        Data = new List<string>() { $"\\b({pattern.Pattern})\\b" },
                        Capture = true,
                        Arguments = pattern.Modifiers?.ToList() ?? new List<string>(),
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Get the OAT Rules used in this RuleSet.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ConvertedOatRule> GetOatRules() => _oatRules;

        /// <summary>
        /// Get the AppInspector Rules contained in this RuleSet.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Rule> GetAppInspectorRules() => _rules;
    }
}
