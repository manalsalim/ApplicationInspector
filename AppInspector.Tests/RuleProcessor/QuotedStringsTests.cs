﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace AppInspector.Tests.RuleProcessor;

[TestClass]
public class QuotedStringsTests
{

    private const string testDoubleQuotesAreCode = "var url = \"https://contoso.com\"; // contoso.com";
    private const string testSingleQuotesAreCode = "var url = 'https://contoso.com'; // contoso.com";
    private const string testSingleLineWithQuotesInComment = "// var url = 'https://contoso.com';";
    private const string testSingleLineWithDoubleQuotesInComment = "// var url = 'https://contoso.com';";
    private const string testMultiLine = @"/* 
https://contoso.com 
*/";
    private const string testMultiLineWithoutProto = @"
/* 
contoso.com 
*/";
    private const string testMultiLineWithResultFollowingCommentEnd = @"
/* 
contoso.com 
*/ var url = ""https://contoso.com""";

    private static string detectContosoRule = @"
    [
    {
        ""id"": ""RE000001"",
        ""name"": ""Testing.Rules.Quotes"",
        ""tags"": [
            ""Testing.Rules.Quotes""
        ],
        ""severity"": ""Critical"",
        ""description"": ""Find contoso.com"",
        ""patterns"": [
            {
                ""pattern"": ""contoso.com"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""_comment"": """"
    }
]
";
    
    private readonly ILoggerFactory _loggerFactory =
        new LogOptions { ConsoleVerbosityLevel = LogEventLevel.Verbose }.GetLoggerFactory();

    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();
    
    [DataRow(testDoubleQuotesAreCode,1)]
    [DataRow(testSingleQuotesAreCode,1)]
    [DataRow(testMultiLine,0)]
    [DataRow(testMultiLineWithoutProto,0)]
    [DataRow(testMultiLineWithResultFollowingCommentEnd,1)]
    [DataRow(testSingleLineWithQuotesInComment,0)]
    [DataRow(testSingleLineWithDoubleQuotesInComment,0)]
    [DataTestMethod]
    public void QuotedStrings(string content, int numIssues)
    {
        RuleSet rules = new(_loggerFactory);
        rules.AddString(detectContosoRule, "contosorule");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor ruleProcessor =
            new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new RuleProcessorOptions());
        _languages.FromFileNameOut("testfile.cs", out LanguageInfo info);
        Assert.AreEqual(numIssues,
            ruleProcessor.AnalyzeFile(content, new FileEntry("testfile.cs", new MemoryStream()), info).Count());
    }
}