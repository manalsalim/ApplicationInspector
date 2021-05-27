﻿// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using NLog;
using NLog.Config;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace Microsoft.ApplicationInspector.Common
{
    /// <summary>
    /// Wraps Console, TextWriter and Log Writes for convenience to write "once" from calling
    /// code to increase readability and support verbosity and color support conveniently
    ///
    /// Note: NLOG is not consistent writing to standard out console making it impossible
    /// to predict when it will be visible thus it is largely used for file log output only
    /// </summary>
    public class WriteOnce
    {
        /// <summary>
        /// For use when logging is needed and was not called via CLI
        /// </summary>
        /// <returns></returns>
        public static TextWriter? TextWriter { get; set; }

        public enum ConsoleVerbosity { High, Medium, Low, None }

        public static ConsoleVerbosity Verbosity { get; set; }
        public static Logger? Log { get; set; } //use SafeLog or check for null before use
        public static bool PauseConsoleOutput
        {
            get => _pauseConsoleOutput;
            set
            {
                _pauseConsoleOutput = value;
                if (!_pauseConsoleOutput)
                {
                    while(pausedWrites.TryTake(out ConsoleWrite? result))
                    {
                        if (result is not null)
                        {
                            SafeConsoleWrite(result.Message, result.WriteLine, result.Foreground, result.Verbosity);
                        }
                    }
                }
            }
        }
        private static bool _pauseConsoleOutput = false;
        //default colors
        private static ConsoleColor _infoColor = ConsoleColor.Magenta;

        private static ConsoleColor _errorColor = ConsoleColor.Red;
        private static ConsoleColor _generalColor = ConsoleColor.Gray;
        private static ConsoleColor _resultColor = ConsoleColor.Yellow;
        private static ConsoleColor _opColor = ConsoleColor.Cyan;
        private static readonly ConsoleColor _sysColor = ConsoleColor.Magenta;

        //change default colors
        public ConsoleColor InfoForeColor { set => _infoColor = value; }

        public ConsoleColor ErrorForeColor { set => _errorColor = value; }
        public ConsoleColor OperationForeColor { set => _opColor = value; }
        public ConsoleColor ResultForeColor { set => _resultColor = value; }
        public ConsoleColor GeneralForeColor { set => _generalColor = value; }

        public static void Operation(string msg, bool writeLine = true, ConsoleVerbosity verbosity = ConsoleVerbosity.Low)
        {
            Any(msg, writeLine, _opColor, verbosity);
        }

        public static void System(string msg, bool writeLine = true, ConsoleVerbosity verbosity = ConsoleVerbosity.Low)
        {
            Any(msg, writeLine, _sysColor, verbosity);
        }

        public static void General(string msg, bool writeLine = true, ConsoleVerbosity verbosity = ConsoleVerbosity.Medium)
        {
            Any(msg, writeLine, _generalColor, verbosity);
        }

        public static void Result(string msg, bool writeLine = true, ConsoleVerbosity verbosity = ConsoleVerbosity.Medium)
        {
            Any(msg, writeLine, _resultColor, verbosity);
        }

        public static void Info(string msg, bool writeLine = true, ConsoleVerbosity verbosity = ConsoleVerbosity.Medium, bool addToLog = true)
        {
            //log but special check for CLI final exit to avoid duplication of hint to check log in the log
            if (addToLog)
            {
                SafeLog(msg, LogLevel.Info);
            }

            //file if applicable
            SafeTextFileWriterWrite(msg, writeLine, verbosity);

            //console if applicable
            SafeConsoleWrite(msg, writeLine, _infoColor, verbosity);
        }

        public static void Error(string msg, bool writeLine = true, ConsoleVerbosity verbosity = ConsoleVerbosity.Low, bool addToLog = true)
        {
            //log but special check for CLI final exit to avoid duplication of hint to check log in the log
            if (addToLog)
            {
                SafeLog(msg, LogLevel.Error);
            }

            //file if applicable
            SafeTextFileWriterWrite(msg, writeLine, verbosity); SafeTextFileWriterWrite(msg, writeLine, verbosity);

            //console if applicable
            SafeConsoleWrite(msg, writeLine, _errorColor, verbosity);
        }

        public static void Any(string msg, bool writeLine = true, ConsoleColor foreColor = ConsoleColor.Gray, ConsoleVerbosity verbosity = ConsoleVerbosity.Medium)
        {
            //log
            SafeLog(msg, LogLevel.Trace);

            //file if applicable
            SafeTextFileWriterWrite(msg, writeLine, verbosity);

            //console if applicable
            SafeConsoleWrite(msg, writeLine, foreColor, verbosity);
        }

        public static void NewLine(ConsoleVerbosity verbosity = ConsoleVerbosity.Medium, bool ConsoleOnly = true)
        {
            if (TextWriter != null && TextWriter == Console.Out)
            {
                if (verbosity >= Verbosity)
                {
                    Console.WriteLine();
                }
            }

            if (!ConsoleOnly)
            {
                SafeTextFileWriterWrite("", true, verbosity);
            }
        }

        static ConcurrentBag<ConsoleWrite> pausedWrites = new ConcurrentBag<ConsoleWrite>();
        
        class ConsoleWrite
        {
            public string Message { get; }
            public bool WriteLine { get; }
            public ConsoleColor Foreground { get; }
            public ConsoleVerbosity Verbosity { get; }
            public ConsoleWrite(string message, bool writeLine, ConsoleColor foreground, ConsoleVerbosity verbosity)
            {
                Message = message;
                WriteLine = writeLine;
                Foreground = foreground;
                Verbosity = verbosity;
            }
        }

        /// <summary>
        /// Console commands are only effected from CLI
        /// Filters verbosity based on settings and given call
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="writeLine"></param>
        /// <param name="foreground"></param>
        /// <param name="verbosity"></param>
        private static void SafeConsoleWrite(string msg, bool writeLine, ConsoleColor foreground, ConsoleVerbosity verbosity)
        {
            if (TextWriter != null && TextWriter != Console.Out)
            {
                return;
            }
            if (verbosity >= Verbosity)
            {
                if (PauseConsoleOutput)
                {
                    pausedWrites.Add(new ConsoleWrite(msg, writeLine, foreground, verbosity));
                }
                else
                {
                    ConsoleColor lastForecolor = Console.ForegroundColor;
                    Console.ForegroundColor = foreground;

                    if (writeLine)
                    {
                        Console.WriteLine(msg);
                    }
                    else
                    {
                        Console.Write(msg);
                    }

                    Console.ForegroundColor = lastForecolor;

                }
            }
        }

        private static void SafeTextFileWriterWrite(string msg, bool writeLine, ConsoleVerbosity verbosity)
        {
            if (TextWriter == null || TextWriter == Console.Out)
            {
                return;
            }

            if (verbosity >= Verbosity)
            {
                if (writeLine)
                {
                    TextWriter.WriteLine(msg);
                }
                else
                {
                    TextWriter.Write(msg);
                }
            }
        }

        /// <summary>
        /// Attempts to initialize default log if not already setup and writes to log
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logLevel"></param>
        public static void SafeLog(string message, NLog.LogLevel logLevel)
        {
            if (Log == null)
            {
                Log = Utils.SetupLogging();
            }

            if (Log != null && Log.Name != "Console")
            {
                int value = logLevel.Ordinal;
                switch (value)
                {
                    case 0:
                        Log.Trace(message);
                        break;

                    case 1:
                        Log.Debug(message);
                        break;

                    case 2:
                        Log.Info(message);
                        break;

                    case 3:
                        Log.Warn(message);
                        break;

                    case 4:
                        Log.Error(message);
                        break;
                }
            }
        }
    }
}
