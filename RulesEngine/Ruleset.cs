﻿// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    ///     Storage for rules
    /// </summary>
    public class RuleSet : IEnumerable<Rule>
    {
        private readonly Logger? _logger;
        private List<ConvertedOatRule> _oatRules = new();//used for analyze cmd primarily
        private IEnumerable<Rule> _rules { get => _oatRules.Select(x => x.AppInspectorRule); }
        private Regex searchInRegex = new Regex("\\((.*),(.*)\\)", RegexOptions.Compiled);

        /// <summary>
        ///     Creates instance of Ruleset
        /// </summary>
        public RuleSet(Logger? log)
        {
            _logger = log;
        }

        /// <summary>
        ///     Parse a directory with rule files and loads the rules
        /// </summary>
        /// <param name="path"> Path to rules folder </param>
        /// <param name="tag"> Tag for the rules </param>
        public void AddDirectory(string path, string? tag = null)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException();

            foreach (string filename in Directory.EnumerateFileSystemEntries(path, "*.json", SearchOption.AllDirectories))
            {
                AddFile(filename, tag);
            }
        }

        /// <summary>
        ///     Load rules from a file
        /// </summary>
        /// <param name="filename"> Filename with rules </param>
        /// <param name="tag"> Tag for the rules </param>
        public void AddFile(string? filename, string? tag = null)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException(null, nameof(filename));

            _logger?.Debug("Attempting to read rule file: " + filename);

            if (!File.Exists(filename))
                throw new FileNotFoundException();

            using StreamReader file = File.OpenText(filename);
            AddString(file.ReadToEnd(), filename, tag);
        }

        /// <summary>
        ///     Adds the elements of the collection to the Ruleset
        /// </summary>
        /// <param name="collection"> Collection of rules </param>
        public void AddRange(IEnumerable<Rule>? collection)
        {
            if (collection is null)
            {
                return;
            }
            foreach (var rule in collection.Select(AppInspectorRuleToOatRule))
            {
                if (rule != null)
                {
                    _logger?.Debug("Attempting to add rule: " + rule.Name);
                    _oatRules.Add(rule);
                }
            }
        }

        /// <summary>
        ///     Add rule into Ruleset
        /// </summary>
        /// <param name="rule"> </param>
        public void AddRule(Rule rule)
        {
            if (AppInspectorRuleToOatRule(rule) is ConvertedOatRule cor)
            {
                _logger?.Debug("Attempting to add rule: " + rule.Name);
                _oatRules.Add(cor);
            }
        }

        /// <summary>
        ///     Load rules from JSON string
        /// </summary>
        /// <param name="jsonstring"> JSON string </param>
        /// <param name="sourcename"> Name of the source (file, stream, etc..) </param>
        /// <param name="tag"> Tag for the rules </param>
        public void AddString(string jsonstring, string sourcename, string? tag = null)
        {
            AddRange(StringToRules(jsonstring ?? string.Empty, sourcename ?? string.Empty, tag));
        }

        /// <summary>
        ///     Filters rules within Ruleset by languages
        /// </summary>
        /// <param name="languages"> Languages </param>
        /// <returns> Filtered rules </returns>
        public IEnumerable<ConvertedOatRule> ByLanguage(string language)
        {
            if (!string.IsNullOrEmpty(language))
            {
                return _oatRules.Where(x =>  x.AppInspectorRule.AppliesTo is string[] appliesList && appliesList.Contains(language));
            }
            return Array.Empty<ConvertedOatRule>();
        }

        /// <summary>
        ///     Filters rules within Ruleset by applies to regexes
        /// </summary>
        /// <param name="languages"> Languages </param>
        /// <returns> Filtered rules </returns>
        public IEnumerable<ConvertedOatRule> ByFilename(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                return _oatRules.Where(x => x.AppInspectorRule.CompiledFileRegexes.Any(y => y.IsMatch(input)));
            }
            return Array.Empty<ConvertedOatRule>();
        }

        public IEnumerable<ConvertedOatRule> GetUniversalRules()
        {
            return _oatRules.Where(x => (x.AppInspectorRule.FileRegexes is null || x.AppInspectorRule.FileRegexes.Length == 0) && (x.AppInspectorRule.AppliesTo is null || x.AppInspectorRule.AppliesTo.Length == 0));
        }

        public ConvertedOatRule? AppInspectorRuleToOatRule(Rule rule)
        {
            var clauses = new List<Clause>();
            int clauseNumber = 0;
            var expression = new StringBuilder("(");
            foreach (var pattern in rule.Patterns ?? Array.Empty<SearchPattern>())
            {
                if (pattern.Pattern != null)
                {
                    var scopes = pattern.Scopes ?? new PatternScope[] { PatternScope.All };
                    var modifiers = pattern.Modifiers ?? Array.Empty<string>();
                    if (pattern.PatternType == PatternType.String || pattern.PatternType == PatternType.Substring)
                    {
                        clauses.Add(new OATSubstringIndexClause(scopes, useWordBoundaries: pattern.PatternType == PatternType.String)
                        {
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),//important to pattern index identification
                            Data = new List<string>() { pattern.Pattern },
                            Capture = true
                        });
                        if (clauseNumber > 0)
                        {
                            expression.Append(" OR ");
                        }
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                    else if (pattern.PatternType == PatternType.Regex)
                    {
                        clauses.Add(new OATRegexWithIndexClause(scopes)
                        {
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),//important to pattern index identification
                            Data = new List<string>() { pattern.Pattern },
                            Capture = true,
                            Arguments = pattern.Modifiers?.ToList() ?? new List<string>(),
                            CustomOperation = "RegexWithIndex"
                        });
                        if (clauseNumber > 0)
                        {
                            expression.Append(" OR ");
                        }
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                    else if (pattern.PatternType == PatternType.RegexWord)
                    {
                        clauses.Add(new OATRegexWithIndexClause(scopes)
                        {
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),//important to pattern index identification
                            Data = new List<string>() { $"\\b({pattern.Pattern})\\b" },
                            Capture = true,
                            Arguments = pattern.Modifiers?.ToList() ?? new List<string>(),
                            CustomOperation = "RegexWithIndex"
                        });
                    
                        if (clauseNumber > 0)
                        {
                            expression.Append(" OR ");
                        }
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
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
                if (condition.Pattern?.Pattern != null)
                {
                    if (condition.SearchIn?.Equals("finding-only", StringComparison.InvariantCultureIgnoreCase) != false)
                    {
                        clauses.Add(new WithinClause()
                        {
                            Data = new List<string>() { condition.Pattern.Pattern },
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            Invert = condition.NegateFinding,
                            Arguments = condition.Pattern.Modifiers?.ToList() ?? new List<string>(),
                            FindingOnly = true,
                            CustomOperation = "Within"
                        });
                        expression.Append(" AND ");
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                    else if (condition.SearchIn.StartsWith("finding-region", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var argList = new List<int>();
                        Match m = searchInRegex.Match(condition.SearchIn);
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
                            clauses.Add(new WithinClause()
                            {
                                Data = new List<string>() { condition.Pattern.Pattern },
                                Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                                Invert = condition.NegateFinding,
                                Arguments = condition.Pattern.Modifiers?.ToList() ?? new List<string>(),
                                FindingOnly = false,
                                CustomOperation = "Within",
                                Before = argList[0],
                                After = argList[1]
                            });
                            expression.Append(" AND ");
                            expression.Append(clauseNumber);
                            clauseNumber++;
                        }
                    }
                    else if (condition.SearchIn.Equals("same-line", StringComparison.InvariantCultureIgnoreCase))
                    {
                        clauses.Add(new WithinClause()
                        {
                            Data = new List<string>() { condition.Pattern.Pattern },
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            Invert = condition.NegateFinding,
                            Arguments = condition.Pattern.Modifiers?.ToList() ?? new List<string>(),
                            SameLineOnly = true,
                            CustomOperation = "Within"
                        });
                        expression.Append(" AND ");
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                }
            }
            return new ConvertedOatRule(rule.Id, rule)
            {
                Clauses = clauses,
                Expression = expression.ToString()
            };
        }

        public IEnumerable<ConvertedOatRule> GetOatRules() => _oatRules;

        public IEnumerable<Rule> GetAppInspectorRules()
        {
            return _rules;
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the Ruleset where the default is the AppInspector version
        /// </summary>
        /// <returns> Enumerator </returns>
        public IEnumerator GetEnumerator()
        {
            GetAppInspectorRules();
            return _rules.GetEnumerator();
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the Ruleset where the default is the AppInspector version
        /// </summary>
        /// <returns> Enumerator </returns>
        IEnumerator<Rule> IEnumerable<Rule>.GetEnumerator()
        {
            GetAppInspectorRules();
            return _rules.GetEnumerator();
        }

        internal IEnumerable<Rule> StringToRules(string jsonstring, string sourcename, string? tag = null)
        {
            List<Rule>? ruleList = JsonConvert.DeserializeObject<List<Rule>>(jsonstring);
            if (ruleList is List<Rule>)
            {
                foreach (Rule r in ruleList)
                {
                    r.Source = sourcename;
                    r.RuntimeTag = tag ?? "";
                    if (r.Patterns == null)
                        r.Patterns = Array.Empty<SearchPattern>();
                    yield return r;
                }
            }
        }
    }
}
