﻿using System.IO;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppInspector.Tests.RuleProcessor;

[TestClass]
public class XmlAndJsonTests
{
    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();
    
    private const string jsonAndXmlStringRule = @"[
        {
            ""id"": ""SA000005"",
            ""name"": ""Testing.Rules.JSONandXML"",
            ""tags"": [
                ""Testing.Rules.JSON.JSONandXML""
            ],
            ""severity"": ""Critical"",
            ""description"": ""This rule finds books from the JSON or XML titled with Franklin."",
            ""patterns"": [
                {
                    ""pattern"": ""Franklin"",
                    ""type"": ""string"",
                    ""confidence"": ""High"",
                    ""scopes"": [
                        ""code""
                    ],
                    ""jsonpaths"" : [""$.books[*].title""],
                    ""xpaths"" : [""/bookstore/book/title""]
                }
            ],
            ""_comment"": """"
        }
    ]";

    private const string jsonStringRule = @"[
        {
            ""id"": ""SA000005"",
            ""name"": ""Testing.Rules.JSON"",
            ""tags"": [
                ""Testing.Rules.JSON""
            ],
            ""severity"": ""Critical"",
            ""description"": ""This rule finds books from the JSON titled with Franklin."",
            ""patterns"": [
                {
                    ""pattern"": ""Franklin"",
                    ""type"": ""string"",
                    ""confidence"": ""High"",
                    ""scopes"": [
                        ""code""
                    ],
                    ""jsonpaths"" : [""$.books[*].title""]
                }
            ],
            ""_comment"": """"
        }
    ]";

    private const string xmlStringRule = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.XML"",
        ""tags"": [
            ""Testing.Rules.XML""
        ],
        ""severity"": ""Critical"",
        ""description"": ""This rule finds books from the XML titled with Franklin."",
        ""patterns"": [
            {
                ""pattern"": ""Franklin"",
                ""type"": ""string"",
                ""confidence"": ""High"",
                ""scopes"": [
                    ""code""
                ],
                ""xpaths"" : [""/bookstore/book/title""]
            }
        ],
        ""_comment"": """"
    }
]";

    private const string jsonData =
        @"{
    ""books"":
    [
        {
            ""category"": ""fiction"",
            ""title"" : ""A Wild Sheep Chase"",
            ""author"" : ""Haruki Murakami"",
            ""price"" : 22.72
        },
        {
            ""category"": ""fiction"",
            ""title"" : ""The Night Watch"",
            ""author"" : ""Sergei Lukyanenko"",
            ""price"" : 23.58
        },
        {
            ""category"": ""fiction"",
            ""title"" : ""The Comedians"",
            ""author"" : ""Graham Greene"",
            ""price"" : 21.99
        },
        {
            ""category"": ""memoir"",
            ""title"" : ""The Night Watch"",
            ""author"" : ""David Atlee Phillips"",
            ""price"" : 260.90
        },
        {
            ""category"": ""memoir"",
            ""title"" : ""The Autobiography of Benjamin Franklin"",
            ""author"" : ""Benjamin Franklin"",
            ""price"" : 123.45
        }
    ]
}
";

    private const string xmlData =
        @"<?xml version=""1.0"" encoding=""utf-8"" ?>   
  <bookstore>  
      <book genre=""autobiography"" publicationdate=""1981-03-22"" ISBN=""1-861003-11-0"">  
          <title>The Autobiography of Benjamin Franklin</title>  
          <author>  
              <first-name>Benjamin</first-name>  
              <last-name>Franklin</last-name>  
          </author>  
          <price>8.99</price>  
      </book>  
      <book genre=""novel"" publicationdate=""1967-11-17"" ISBN=""0-201-63361-2"">  
          <title>The Confidence Man</title>  
          <author>  
              <first-name>Herman</first-name>  
              <last-name>Melville</last-name>  
          </author>  
          <price>11.99</price>  
      </book>  
      <book genre=""philosophy"" publicationdate=""1991-02-15"" ISBN=""1-861001-57-6"">  
          <title>The Gorgias</title>  
          <author>  
              <name>Plato</name>  
          </author>  
          <price>9.99</price>  
      </book>  
  </bookstore>
";
    
    [DataRow(jsonStringRule)]
    [DataRow(jsonAndXmlStringRule)]
    [DataTestMethod]
    public void JsonStringRule(string rule)
    {
        RuleSet rules = new();
        rules.AddString(rule, "JsonTestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.json", out var info))
        {
            var matches = processor.AnalyzeFile(jsonData, new FileEntry("test.json", new MemoryStream()), info);
            Assert.AreEqual(1, matches.Count);
        }
        else
        {
            Assert.Fail();
        }
    }

    [DataRow(xmlStringRule)]
    [DataRow(jsonAndXmlStringRule)]
    [DataTestMethod]
    public void XmlStringRule(string rule)
    {
        RuleSet rules = new();
        rules.AddString(rule, "XmlTestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(xmlData, new FileEntry("test.xml", new MemoryStream()), info);
            Assert.AreEqual(1, matches.Count);
        }
        else
        {
            Assert.Fail();
        }
    }
    
    [TestMethod]
    public void TestXmlWithAndWithoutNamespace()
    {
        var content = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"">
  <modelVersion>4.0.0</modelVersion>

  <groupId>xxx</groupId>
  <artifactId>xxx</artifactId>
  <version>0.1.0-SNAPSHOT</version>
  <packaging>pom</packaging>

  <name>${project.groupId}:${project.artifactId}</name>
  <description />

  <properties>
    <java.version>17</java.version>
  </properties>

</project>";
        // The same as above but with no namespace specified
        var noNamespaceContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project>
  <modelVersion>4.0.0</modelVersion>

  <groupId>xxx</groupId>
  <artifactId>xxx</artifactId>
  <version>0.1.0-SNAPSHOT</version>
  <packaging>pom</packaging>

  <name>${project.groupId}:${project.artifactId}</name>
  <description />

  <properties>
    <java.version>17</java.version>
  </properties>

</project>";
        var rule = @"[{
    ""name"": ""Source code: Java 17"",
    ""id"": ""CODEJAVA000000"",
    ""description"": ""Java 17 maven configuration"",
    ""applies_to_file_regex"": [
      ""pom.xml""
    ],
    ""tags"": [
      ""Code.Java.17""
    ],
    ""severity"": ""critical"",
    ""patterns"": [
      {
        ""pattern"": ""17"",
        ""xpaths"" : [""/*[local-name(.)='project']/*[local-name(.)='properties']/*[local-name(.)='java.version']""],
        ""type"": ""regex"",
        ""scopes"": [
          ""code""
        ],
        ""modifiers"": [
          ""i""
        ],
        ""confidence"": ""high""
      }
    ]
  }]";
        RuleSet rules = new();
        var originalSource = "TestRules";
        rules.AddString(rule, originalSource);
        var analyzer = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules,
            new RuleProcessorOptions { Parallel = false, AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("pom.xml", out var info))
        {
            var matches = analyzer.AnalyzeFile(content, new FileEntry("pom.xml", new MemoryStream()), info);
            Assert.AreEqual(1, matches.Count);
            matches = analyzer.AnalyzeFile(noNamespaceContent, new FileEntry("pom.xml", new MemoryStream()), info);
            Assert.AreEqual(1, matches.Count);
        }
    }
}