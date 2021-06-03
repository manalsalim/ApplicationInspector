﻿using ApplicationInspector.Unitprocess.Misc;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationInspector.Unitprocess.Commands
{
    /// <summary>
    /// Test class for Analyze Commands
    /// Each method really needs to be complete i.e. options and command objects created and checked for exceptions etc. based on inputs so
    /// doesn't create a set of shared objects
    /// Note: in order to avoid log reuse, include the optional parameter CloseLogOnCommandExit = true
    /// </summary>
    [TestClass]
    public class TestAnalyzeCmd
    {
        [TestInitialize]
        public void InitOutput()
        {
            Directory.CreateDirectory(Helper.GetPath(Helper.AppPath.testOutput));
        }

        [TestCleanup]
        public void CleanUp()
        {
            try
            {
                Directory.Delete(Helper.GetPath(Helper.AppPath.testOutput), true);
            }
            catch
            {
            }

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\log.txt")
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void BasicAnalyze_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>() //allow source under unittest path
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {

            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void BasicZipRead_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\main.zip") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {

            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"baddir\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InvalidRulesPath_Fail()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"notfound.json")
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                IgnoreDefaultRules = true,
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                IgnoreDefaultRules = true,
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = AnalyzeResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = AnalyzeResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void DefaultAndCustomRulesMatched_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json")
            };

            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
                Assert.IsTrue(result.Metadata.UniqueTags.Any(v => v.Contains("Cryptography.Encryption.General")));
                Assert.IsTrue(result.Metadata.UniqueTags.Any(v => v.Contains("Data.Custom1")));
            }
            catch (Exception)
            {
                throw;
            }
        }

        [TestMethod]
        public void ExclusionFilter_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\project\one") },
                FilePathExclusions = new string[] { "**/main.cpp" }
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.NoMatches);
        }

        [TestMethod]
        public async Task ExpectedTagCountAsync_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                SingleThread = true
            };
            AnalyzeCommand command = new AnalyzeCommand(options);
            AnalyzeResult result = await command.GetResultAsync(new CancellationToken());
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(11, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void ScanUnknownFileTypesTest_Include()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unknowntest") },
                FilePathExclusions = Array.Empty<string>(),
                ScanUnknownTypes = true
            };
            AnalyzeCommand command = new AnalyzeCommand(options);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(2, result.Metadata.TotalFiles);
            Assert.AreEqual(0, result.Metadata.FilesSkipped);
            Assert.AreEqual(2, result.Metadata.FilesAffected);
            Assert.AreEqual(69, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(22, result.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void ScanUnknownFileTypesTest_Exclude()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unknowntest") },
                FilePathExclusions = Array.Empty<string>(),
                ScanUnknownTypes = false,
                SingleThread = true
            };
            AnalyzeCommand command = new AnalyzeCommand(options);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(2, result.Metadata.TotalFiles);
            Assert.AreEqual(1, result.Metadata.FilesSkipped);
            Assert.AreEqual(1, result.Metadata.FilesAffected);
            Assert.AreEqual(37, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(21, result.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void MultiPath_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[] 
                { 
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp")
                },
                FilePathExclusions = Array.Empty<string>(),
            };

            AnalyzeCommand command = new AnalyzeCommand(options);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);

            AnalyzeOptions options2 = new AnalyzeOptions()
            {
                SourcePath = new string[2]
                {
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp")
                },
                FilePathExclusions = Array.Empty<string>(),
            };

            AnalyzeCommand command2 = new AnalyzeCommand(options2);
            AnalyzeResult result2 = command2.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result2.ResultCode);
            Assert.AreEqual(result.Metadata.TotalMatchesCount * 2, result2.Metadata.TotalMatchesCount);
            Assert.AreEqual(result.Metadata.UniqueMatchesCount, result2.Metadata.UniqueMatchesCount); 
        }


        [TestMethod]
        public void ExpectedTagCountDupsAllowed_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp") },
                FilePathExclusions = Array.Empty<string>(),
                SingleThread = true
            };

            AnalyzeCommand command = new AnalyzeCommand(options);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(11, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueMatchesCount);


            options.SingleThread = false;
            command = new AnalyzeCommand(options);
            result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(11, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void NoMatchesFound_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = AnalyzeResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.NoMatches);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path

                LogFileLevel = "trace",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logtrace.txt"),
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                {
                    exitCode = AnalyzeResult.ExitCode.CriticalError;
                }
                else if (testLogContent.ToLower().Contains("trace"))
                {
                    exitCode = AnalyzeResult.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                exitCode = AnalyzeResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfile.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path

                LogFileLevel = "error",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logerror.txt"),
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
            }
            catch (Exception)
            {
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (!String.IsNullOrEmpty(testLogContent) && testLogContent.ToLower().Contains("error"))
                {
                    exitCode = AnalyzeResult.ExitCode.Success;
                }
                else
                {
                    exitCode = AnalyzeResult.ExitCode.CriticalError;
                }
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                LogFileLevel = "debug",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logdebug.txt"),
                CloseLogOnCommandExit = true
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                {
                    exitCode = AnalyzeResult.ExitCode.CriticalError;
                }
                else if (testLogContent.ToLower().Contains("debug"))
                {
                    exitCode = AnalyzeResult.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = new string[1] { Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                ConsoleVerbosityLevel = "none"
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                // Attempt to open output file.
                using (var writer = new StreamWriter(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt")))
                {
                    // Redirect standard output from the console to the output file.
                    Console.SetOut(writer);

                    AnalyzeCommand command = new AnalyzeCommand(options);
                    AnalyzeResult result = command.GetResult();
                    exitCode = result.ResultCode;
                    try
                    {
                        string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt"));
                        if (String.IsNullOrEmpty(testContent))
                        {
                            exitCode = AnalyzeResult.ExitCode.Success;
                        }
                        else
                        {
                            exitCode = AnalyzeResult.ExitCode.NoMatches;
                        }
                    }
                    catch (Exception)
                    {
                        exitCode = AnalyzeResult.ExitCode.Success;//no console output file found
                    }
                }
            }
            catch (Exception)
            {
                exitCode = AnalyzeResult.ExitCode.CriticalError;
            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }
    }
}