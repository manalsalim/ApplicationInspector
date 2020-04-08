﻿using ApplicationInspector.Unitprocess.Misc;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.IO;

namespace ApplicationInspector.Unitprocess.CLICommands
{
    /// <summary>
    /// Test class for Analyze Commands
    /// Each method really needs to be complete i.e. options and command objects created and checked for exceptions etc. based on inputs so
    /// doesn't create a set of shared objects
    ///
    /// </summary>
    [TestClass]
    public class CLITestAnalyzeCmd
    {
        [TestMethod]
        public void BasicHTMLOutput_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -b -s {0} -f html -k none", Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"));
                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void SimpleTagsHTMLOutput_Fail() //simple tags not supported for html format
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -b -s {0} -f html -k none -t", Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"));
                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void DupTagsHTMLOutput_Fail() //dupliacte tags not supported for html format
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -b -s {0} -f html -k none -d", Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"));
                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void UnknownFormat_Fail() //dupliacte tags not supported for html format
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -b -s {0} -f unknown -k none", Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"));
                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void ZipReadHTMLOutput_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -b -s {0} -f html -k none -l {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\mainx.zip"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void SimpleTagsTextOutput_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -f text -k none -o {1} -t",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                if (File.Exists(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt")))
                {
                    File.Delete(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
                }

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
                exitCode = testContent.Contains("Data.Sensitive") ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void SimpleTagsJsonOutput_JSSrc_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -f json -k none -o {1} -t",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\onetag.js"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                if (File.Exists(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt")))
                {
                    File.Delete(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
                }

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
                exitCode = testContent.Contains("Data.Parsing.JSON") ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void SimpleTagsJsonOutput_CPPSrc_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -f json -k none -o {1} -t",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                if (File.Exists(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt")))
                {
                    File.Delete(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
                }

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
                exitCode = testContent.Contains("Authentication.General") ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void InvalidOutputfilePath_Fail()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -f json -k none -o {1}",
                     Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                     Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"badir\output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -f json -k none -o {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfilepath.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InvalidRulesPath_Fail()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -r badrulespath -f json -k none -o {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -i -f json -k none -o {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -i -r {1} -f json -k none -o {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -r {1} -f json -k none -o {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void DefaultAndCustomRulesPosMatches_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -r {1} -f json -k none -o {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);

                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
                exitCode = testContent.Contains("Data.Parsing.JSON") ? AnalyzeResult.ExitCode.Success : exitCode;
                exitCode = testContent.Contains("Data.Custom1") ? AnalyzeResult.ExitCode.Success : exitCode;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void ExclusionFilter_Fail()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -r {1} -f json -o {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.NoMatches);
        }

        [TestMethod]
        public void ExpectedTagCountDupsAllowed_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -d -f json -o {1} -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);

                if (exitCode == AnalyzeResult.ExitCode.Success)
                {
                    string content = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
                    var result = JsonConvert.DeserializeObject<AnalyzeResult>(content);
                    exitCode = result.Metadata.TotalMatchesCount == 11 && result.Metadata.UniqueMatchesCount == 7 ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.NoMatches;
                }
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void ExpectedTagCountNoDupsAllowed_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -f json -o {1} -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);

                if (exitCode == AnalyzeResult.ExitCode.Success)
                {
                    string content = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
                    var result = JsonConvert.DeserializeObject<AnalyzeResult>(content);
                    exitCode = result.Metadata.TotalMatchesCount == 7 && result.Metadata.UniqueMatchesCount == 7 ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.NoMatches;
                }
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void NoMatchesFound_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -f json -o {1} -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.NoMatches);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -f json -l {1} -v trace -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);

                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                exitCode = testContent.ToLower().Contains("trace") ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -f json -l {1} -v error -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);

                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                exitCode = testContent.ToLower().Contains("error") ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -f json -l {1} -v debug -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);

                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                exitCode = testContent.ToLower().Contains("debug") ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -f json -l {1} -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfile.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"badir\log.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -f json -l {1} -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -x none -f text -k none -o {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args, out string testContent);

                if (exitCode == AnalyzeResult.ExitCode.Success)
                {
                    exitCode = String.IsNullOrEmpty(testContent) ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void NoConsoleNoFileOutput_Fail()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"analyze -s {0} -x none -f text -k none -l {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args);
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }
    }
}