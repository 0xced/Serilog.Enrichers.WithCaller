﻿using System;

using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Serilog.Core;
using Serilog.Sinks.InMemory;
using Serilog.Sinks.InMemory.Assertions;

namespace Serilog.Enrichers.WithCaller.Tests
{
    [TestClass()]
    public class CallerEnricherTests
    {
        public static string LogMessageTemplate { get; } = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message} (at {Caller}){NewLine}{Exception}";
        public static LoggingLevelSwitch LoggingLevelSwitch { get; } = new LoggingLevelSwitch(Events.LogEventLevel.Verbose);

        public static InMemorySink InMemoryInstance => InMemorySink.Instance;

        public static ILogger CreateLogger(bool includeFileInfo = false, int maxDepth = 1)
        {
            return new LoggerConfiguration()
                .Enrich.WithCaller(includeFileInfo, maxDepth)
                .WriteTo.InMemory(outputTemplate: LogMessageTemplate)
                .CreateLogger();
        }

        [TestCleanup]
        public void Cleanup()
        {
            InMemoryInstance.Dispose();
        }

        //https://gist.github.com/nblumhardt/0e1e22f50fe79de60ad257f77653c813
        //https://github.com/serilog-contrib/SerilogSinksInMemory
        [TestMethod()]
        public void EnrichTest()
        {
            var logger = CreateLogger(includeFileInfo: false);
            logger.Error(new Exception(), "hello");
            InMemoryInstance.Should()
                .HaveMessage("hello")
                .Appearing().Once()
                .WithProperty("Caller")
                .WithValue("Serilog.Enrichers.WithCaller.Tests.CallerEnricherTests.EnrichTest()");
        }

        [TestMethod()]
        public void EnrichTestWithFileInfo()
        {
            var fileName = new StackFrame(fNeedFileInfo: true).GetFileName();
            var logger = CreateLogger(includeFileInfo: true);
            logger.Error(new Exception(), "hello");
            InMemoryInstance.Should()
                .HaveMessage("hello")
                .Appearing().Once()
                .WithProperty("Caller")
                .WithValue($"Serilog.Enrichers.WithCaller.Tests.CallerEnricherTests.EnrichTestWithFileInfo() {fileName}:55");
        }

        [TestMethod()]
        public void MaxDepthTest()
        {
            var logger = CreateLogger(includeFileInfo: false, maxDepth: 2);
            logger.Error(new Exception(), "hello");
            InMemoryInstance.Should()
                .HaveMessage("hello")
                .Appearing().Once()
                .WithProperty("Caller")
                .WithValue("Serilog.Enrichers.WithCaller.Tests.CallerEnricherTests.MaxDepthTest() at System.RuntimeMethodHandle.InvokeMethod(System.Object, System.Object[], System.Signature, System.Boolean, System.Boolean)");
        }
    }
}