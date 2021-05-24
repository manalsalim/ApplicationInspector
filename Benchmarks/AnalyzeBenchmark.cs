﻿using BenchmarkDotNet.Attributes;
using Microsoft.ApplicationInspector.Commands;
using System;
using System.IO;
using System.Reflection;

namespace Benchmarks
{
    //[ConcurrencyVisualizerProfiler]
    public class AnalyzeBenchmark
    {
        // Manually put the file you want to benchmark. But don't put this in a path with "Test" in the name ;)
        private const string path = "C:\\Users\\gstocco\\Documents\\GitHub\\ApplicationInspector\\RulesEngine\\";

        public AnalyzeBenchmark()
        {
        }

        [Benchmark(Baseline = true)]
        public static void AnalyzeSingleThreaded()
        {
            AnalyzeCommand command = new(new AnalyzeOptions()
            {
                SourcePath = new string[1] { path },
                SingleThread = true,
                IgnoreDefaultRules = false,
                FilePathExclusions = new string[] { "**/bin/**","**/obj/**" },
                NoShowProgress = true
            });

            _ = command.GetResult();
        }

        [Benchmark]
        public static void AnalyzeMultiThread()
        {
            AnalyzeCommand command = new(new AnalyzeOptions()
            {
                SourcePath = new string[1] { path },
                SingleThread = false,
                IgnoreDefaultRules = false,
                FilePathExclusions = new string[] { "**/bin/**", "**/obj/**" },
                NoShowProgress = true
            });

            _ = command.GetResult();
        }

        public static string GetExecutingDirectoryName()
        {
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            return new FileInfo(location.AbsolutePath).Directory.FullName;
        }
    }
}
