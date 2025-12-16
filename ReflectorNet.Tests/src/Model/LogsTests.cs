/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using com.IvanMurzak.ReflectorNet.Model;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.Model
{
    public class LogsTests : BaseTest
    {
        public LogsTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Logs_ShouldBeEmpty_WhenInitialized()
        {
            var logs = new Logs();

            Assert.Empty(logs);
            Assert.Equal(string.Empty, logs.ToString().Trim());
        }

        [Fact]
        public void Trace_ShouldAddTraceLogEntry()
        {
            var logs = new Logs();
            logs.Trace("Test trace message");

            var output = logs.ToString();

            Assert.Single(logs);
            Assert.Contains("[Trace]", output);
            Assert.Contains("Test trace message", output);
        }

        [Fact]
        public void Debug_ShouldAddDebugLogEntry()
        {
            var logs = new Logs();
            logs.Debug("Test debug message");

            var output = logs.ToString();

            Assert.Single(logs);
            Assert.Contains("[Debug]", output);
            Assert.Contains("Test debug message", output);
        }

        [Fact]
        public void Info_ShouldAddInfoLogEntry()
        {
            var logs = new Logs();
            logs.Info("Test info message");

            var output = logs.ToString();

            Assert.Single(logs);
            Assert.Contains("[Info]", output);
            Assert.Contains("Test info message", output);
        }

        [Fact]
        public void Success_ShouldAddSuccessLogEntry()
        {
            var logs = new Logs();
            logs.Success("Test success message");

            var output = logs.ToString();

            Assert.Single(logs);
            Assert.Contains("[Success]", output);
            Assert.Contains("Test success message", output);
        }

        [Fact]
        public void Warning_ShouldAddWarningLogEntry()
        {
            var logs = new Logs();
            logs.Warning("Test warning message");

            var output = logs.ToString();

            Assert.Single(logs);
            Assert.Contains("[Warning]", output);
            Assert.Contains("Test warning message", output);
        }

        [Fact]
        public void Error_ShouldAddErrorLogEntry()
        {
            var logs = new Logs();
            logs.Error("Test error message");

            var output = logs.ToString();

            Assert.Single(logs);
            Assert.Contains("[Error]", output);
            Assert.Contains("Test error message", output);
        }

        [Fact]
        public void Critical_ShouldAddCriticalLogEntry()
        {
            var logs = new Logs();
            logs.Critical("Test critical message");

            var output = logs.ToString();

            Assert.Single(logs);
            Assert.Contains("[Critical]", output);
            Assert.Contains("Test critical message", output);
        }

        [Fact]
        public void Logs_ShouldHaveNoPadding_WithDepthZero()
        {
            var logs = new Logs();
            logs.Info("Test message", depth: 0);

            var output = logs.ToString();
            var lines = output.Split('\n');

            Assert.StartsWith("[Info]", lines[0]);
        }

        [Fact]
        public void Logs_ShouldHaveTwoSpacesPadding_WithDepthOne()
        {
            var logs = new Logs();
            logs.Info("Test message", depth: 1);

            var output = logs.ToString();
            var lines = output.Split('\n');

            Assert.StartsWith("  [Info]", lines[0]);
        }

        [Fact]
        public void Logs_ShouldHaveFourSpacesPadding_WithDepthTwo()
        {
            var logs = new Logs();
            logs.Info("Test message", depth: 2);

            var output = logs.ToString();
            var lines = output.Split('\n');

            Assert.StartsWith("    [Info]", lines[0]);
        }

        [Fact]
        public void Logs_ShouldHaveCorrectPadding_WithMultipleDepths()
        {
            var logs = new Logs();
            logs.Info("Depth 0", depth: 0);
            logs.Info("Depth 1", depth: 1);
            logs.Info("Depth 2", depth: 2);
            logs.Info("Depth 3", depth: 3);

            var output = logs.ToString();
            var lines = output.Split('\n');

            Assert.StartsWith("[Info]", lines[0]);
            Assert.StartsWith("  [Info]", lines[1]);
            Assert.StartsWith("    [Info]", lines[2]);
            Assert.StartsWith("      [Info]", lines[3]);
        }

        [Fact]
        public void Logs_ShouldPreserveOrder_WithMultipleEntries()
        {
            var logs = new Logs();
            logs.Trace("First");
            logs.Debug("Second");
            logs.Info("Third");
            logs.Success("Fourth");
            logs.Warning("Fifth");
            logs.Error("Sixth");
            logs.Critical("Seventh");

            var output = logs.ToString();

            Assert.Contains("First", output);
            Assert.Contains("Second", output);
            Assert.Contains("Third", output);
            Assert.Contains("Fourth", output);
            Assert.Contains("Fifth", output);
            Assert.Contains("Sixth", output);
            Assert.Contains("Seventh", output);

            var firstIndex = output.IndexOf("First");
            var secondIndex = output.IndexOf("Second");
            var thirdIndex = output.IndexOf("Third");
            var fourthIndex = output.IndexOf("Fourth");
            var fifthIndex = output.IndexOf("Fifth");
            var sixthIndex = output.IndexOf("Sixth");
            var seventhIndex = output.IndexOf("Seventh");

            Assert.True(firstIndex < secondIndex);
            Assert.True(secondIndex < thirdIndex);
            Assert.True(thirdIndex < fourthIndex);
            Assert.True(fourthIndex < fifthIndex);
            Assert.True(fifthIndex < sixthIndex);
            Assert.True(sixthIndex < seventhIndex);
        }

        [Fact]
        public void Logs_ShouldSupportMethodChaining()
        {
            var logs = new Logs();
            var result = logs.Info("First")
                            .Debug("Second")
                            .Error("Third");

            Assert.Same(logs, result);
            Assert.Equal(3, logs.Count);
        }

        [Fact]
        public void ToString_ShouldFormatAllLogTypes_Correctly()
        {
            var logs = new Logs();
            logs.Trace("trace");
            logs.Debug("debug");
            logs.Info("info");
            logs.Success("success");
            logs.Warning("warning");
            logs.Error("error");
            logs.Critical("critical");

            var output = logs.ToString();

            Assert.Contains("[Trace] trace", output);
            Assert.Contains("[Debug] debug", output);
            Assert.Contains("[Info] info", output);
            Assert.Contains("[Success] success", output);
            Assert.Contains("[Warning] warning", output);
            Assert.Contains("[Error] error", output);
            Assert.Contains("[Critical] critical", output);
        }

        [Fact]
        public void LogEntry_ShouldHaveDefaultValues()
        {
            var entry = new LogEntry();

            Assert.Equal(0, entry.Depth);
            Assert.Equal(string.Empty, entry.Message);
            Assert.Equal(LogType.Info, entry.Type);
        }
    }
}
