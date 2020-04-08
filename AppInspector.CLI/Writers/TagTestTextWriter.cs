﻿// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.Commands;
using System;

namespace Microsoft.ApplicationInspector.CLI
{
    public class TagTestTextWriter : CommandResultsWriter
    {
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            CLITagTestCmdOptions cLITagTestCmdOptions = (CLITagTestCmdOptions)commandOptions;
            TagTestResult tagTestResult = (TagTestResult)result;

            //For text output, update write once for same results to console or file
            WriteOnce.TextWriter = TextWriter;
            WriteOnce.Result("Result status");
            WriteOnce.General(MsgHelp.FormatString(MsgHelp.ID.TAGTEST_RESULTS_TEST_TYPE, cLITagTestCmdOptions.TestType), false, WriteOnce.ConsoleVerbosity.Low);

            if (tagTestResult.ResultCode == TagTestResult.ExitCode.TestFailed)
            {
                WriteOnce.Any(MsgHelp.GetString(MsgHelp.ID.TAGTEST_RESULTS_FAIL), true, ConsoleColor.Red, WriteOnce.ConsoleVerbosity.Low);
            }
            else
            {
                WriteOnce.Any(MsgHelp.GetString(MsgHelp.ID.TAGTEST_RESULTS_SUCCESS), true, ConsoleColor.Green, WriteOnce.ConsoleVerbosity.Low);
            }

            if (tagTestResult.TagsStatusList.Count > 0)
            {
                WriteOnce.Result("Result details:");
            }

            foreach (TagStatus tag in tagTestResult.TagsStatusList)
            {
                WriteOnce.General(String.Format("Tag: {0}, Detected: {1}", tag.Tag, tag.Detected));
            }

            if (autoClose)
            {
                FlushAndClose();
            }
        }

        public override void FlushAndClose()
        {
            TextWriter.Flush();
            TextWriter.Close();
            WriteOnce.TextWriter = null;
        }
    }
}