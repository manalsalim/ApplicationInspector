﻿using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using System.Reflection;

namespace Microsoft.ApplicationInspector.Common
{
    public static class Utils
    {
        public enum ExitCode
        {
            Success = 0,
            PartialFail = 1,
            CriticalError = 2
        }

        private static string? _basePath;
        public static string? LogFilePath { get; set; } //used to capture and report log path for console messages
        public static Logger? Logger { get; set; }

        public enum AppPath { basePath, defaultRulesSrc, defaultRulesPackedFile, defaultLog, tagGroupPref, tagCounterPref };
        public static Logger SetupLogging()
        {
            LogOptions opts = new LogOptions();//defaults used

            return SetupLogging(opts);
        }

        /// <summary>
        /// Setup application inspector logging; 1 file per process
        /// </summary>
        /// <param name="opts"></param>
        /// <returns></returns>
        public static Logger SetupLogging(LogOptions opts, bool onErrorConsole = false)
        {
            //prevent being called again if already set unless closed first
            if (Logger != null)
            {
                return Logger;
            }

            LoggingConfiguration config = LogManager.Configuration;
            if (config == null)//fix #179 to prevent overwrite of caller config...i.e. just add ours
            {
                config = new LoggingConfiguration();
            }

            if (string.IsNullOrEmpty(opts.LogFilePath))
            {
                opts.LogFilePath = "appinspector.log.txt";
            }

            //clean up previous for convenience in reading
            if (File.Exists(opts.LogFilePath))
            {
                // Read the file and display it line by line.
                StreamReader file = new StreamReader(opts.LogFilePath);
                string line = file?.ReadLine() ?? "";
                file?.Close();
                if (!string.IsNullOrEmpty(line))
                {
                    if (line.Contains("AppInsLog"))//prevent file other than our logs from deletion
                    {
                        File.Delete(opts.LogFilePath);
                    }
                    else
                    {
                        if (Utils.CLIExecutionContext && onErrorConsole)
                        {
                            WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_LOG_PATH, opts.LogFilePath), true, WriteOnce.ConsoleVerbosity.Low, false);
                        }

                        throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_LOG_PATH, opts.LogFilePath));
                    }
                }
            }
            else
            {
                try
                {
                    File.WriteAllText(opts.LogFilePath, "");//verify log file path is writable
                }
                catch (Exception e)
                {
                    WriteOnce.SafeLog(e.Message + "\n" + e.StackTrace, NLog.LogLevel.Error);
                    if (Utils.CLIExecutionContext && onErrorConsole)
                    {
                        WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, opts.LogFilePath), true, WriteOnce.ConsoleVerbosity.Low, false);
                    }

                    throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, opts.LogFilePath)));
                }
            }

            LogLevel log_level = LogLevel.Error;//default
            if (string.IsNullOrEmpty(opts.LogFileLevel))
            {
                opts.LogFileLevel = "Error";
            }

            try
            {
                log_level = LogLevel.FromString(opts.LogFileLevel);
            }
            catch (Exception)
            {
                if (Utils.CLIExecutionContext && onErrorConsole)
                {
                    WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-v"), true, WriteOnce.ConsoleVerbosity.Low, false);
                }

                throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-v")));
            }

            using (var fileTarget = new FileTarget()
            {
                Name = "LogFile",
                FileName = opts.LogFilePath,
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} ${threadid} ${level:uppercase=true} - AppInsLog - ${message}",
                ForceMutexConcurrentWrites = true
            })
            {
                config.AddTarget(fileTarget);
                config.LoggingRules.Add(new LoggingRule("Microsoft.CST.ApplicationInspector", log_level, fileTarget));
            }

            LogFilePath = opts.LogFilePath;//preserve for console path msg

            LogManager.Configuration = config;
            Logger = LogManager.GetLogger("Microsoft.CST.ApplicationInspector");
            return Logger;
        }
        public static string GetVersionString()
        {
            return string.Format("Microsoft Application Inspector {0}", GetVersion());
        }

        public static string GetVersion()
        {
            return (Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false) as AssemblyInformationalVersionAttribute[])?[0].InformationalVersion ?? "Unknown";
        }

        public static bool CLIExecutionContext { get; set; }

        public static string GetPath(AppPath pathType)
        {
            string result = "";
            switch (pathType)
            {
                case AppPath.basePath:
                    result = GetBaseAppPath();
                    break;

                case AppPath.defaultLog:
                    result = "appinspector.log.txt";
                    break;

                case AppPath.defaultRulesSrc://Packrules source use
                    result = Path.GetFullPath(Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector", "rules", "default"));//used to ref project folder
                    break;

                case AppPath.defaultRulesPackedFile://Packrules default output use
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector", "Resources", "defaultRulesPkd.json");//packed default file in project resources
                    break;

                case AppPath.tagGroupPref://CLI use only
                    result = Path.Combine(GetBaseAppPath(), "preferences", "tagreportgroups.json");
                    break;

                case AppPath.tagCounterPref://CLI use only
                    result = Path.Combine(GetBaseAppPath(), "preferences", "tagcounters.json");
                    break;
            }

            result = Path.GetFullPath(result);
            return result;
        }

        private static string GetBaseAppPath()
        {
            if (!string.IsNullOrEmpty(_basePath))
            {
                return _basePath;
            }

            _basePath = Path.GetFullPath(System.AppContext.BaseDirectory);
            return _basePath;
        }
    }
}
