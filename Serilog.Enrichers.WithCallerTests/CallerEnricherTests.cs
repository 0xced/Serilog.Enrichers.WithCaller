using System;

using System.Diagnostics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using Serilog.Sinks.InMemory.Assertions;

namespace Serilog.Enrichers.WithCaller.Tests
{
    [TestClass()]
    public class CallerEnricherTests
    {
        public static string OutputTemplate { get; } = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message} (at {Caller}){NewLine}{Exception}";
        public static LoggingLevelSwitch LoggingLevelSwitch { get; } = new LoggingLevelSwitch(Events.LogEventLevel.Verbose);

        public static InMemorySink InMemoryInstance => InMemorySink.Instance;

        public static ILogger CreateLogger(bool includeFileInfo = false, int maxDepth = 1)
        {
            return new LoggerConfiguration()
                .Enrich.WithCaller(includeFileInfo, maxDepth)
                .WriteTo.InMemory(outputTemplate: OutputTemplate)
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
            var logger = new LoggerConfiguration()
                        .Enrich.WithCaller()
                        .WriteTo.InMemory(outputTemplate: OutputTemplate)
                        .CreateLogger();

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

            var logger = new LoggerConfiguration()
                        .Enrich.WithCaller(true, 1)
                        .WriteTo.InMemory(outputTemplate: OutputTemplate)
                        .CreateLogger();

            logger.Error(new Exception(), "hello"); // line value "nn" is the suffix in WithValue check below
            InMemoryInstance.Should()
                .HaveMessage("hello")
                .Appearing().Once()
                .WithProperty("Caller")
                .WithValue($"Serilog.Enrichers.WithCaller.Tests.CallerEnricherTests.EnrichTestWithFileInfo() {fileName}:64");
        }

        [TestMethod()]
        public void MaxDepthTest()
        {
            var logger = new LoggerConfiguration()
                        .Enrich.WithCaller(includeFileInfo: false, maxDepth: 2)
                        .WriteTo.InMemory(outputTemplate: OutputTemplate)
                        .CreateLogger();

            logger.Error(new Exception(), "hello");
            InMemoryInstance.Should()
                .HaveMessage("hello")
                .Appearing().Once()
                .WithProperty("Caller")
                .WithValue("Serilog.Enrichers.WithCaller.Tests.CallerEnricherTests.MaxDepthTest() at System.RuntimeMethodHandle.InvokeMethod(System.Object, System.Object[], System.Signature, System.Boolean, System.Boolean)");
            //"Serilog.Enrichers.WithCaller.Tests.CallerEnricherTests.MaxDepthTest()"
        }

        [TestMethod()]
        [DataRow(true)]
        [DataRow(false)]
        public void EnrichWithCallerInfoAsStructure(bool fileInfo)
        {
            var logger = CreateLogger(fileInfo);
            logger.Information("hello");
            var value = InMemoryInstance.Should()
                .HaveMessage("hello")
                .Appearing().Once()
                .WithProperty("CallerInfo")
                .Subject;

            var caller = value.Should().BeOfType<StructureValue>().Subject;
            caller.Properties.Should().ContainSingle(e => e.Name == "Class")
                .Which.Value.Should().BeOfType<ScalarValue>()
                .Which.Value.Should().Be("CallerEnricherTests");
            caller.Properties.Should().ContainSingle(e => e.Name == "Namespace")
                .Which.Value.Should().BeOfType<ScalarValue>()
                .Which.Value.Should().Be("Serilog.Enrichers.WithCaller.Tests");
            var method = caller.Properties.Should().ContainSingle(e => e.Name == "Method")
                .Which.Value.Should().BeOfType<StructureValue>().Subject;
            method.Properties.Should().ContainSingle(e => e.Name == "Name")
                .Which.Value.Should().BeOfType<ScalarValue>()
                .Which.Value.Should().BeOfType<string>()
                .Which.Should().Be("EnrichWithCallerInfoAsStructure");
            var parameter = method.Properties.Should().ContainSingle(e => e.Name == "Parameters")
                .Which.Value.Should().BeOfType<SequenceValue>()
                .Which.Elements.Should().ContainSingle()
                .Which.Should().BeOfType<StructureValue>().Subject;
            parameter.Properties.Should().HaveCount(2);
            parameter.Properties.Should().Contain(e => e.Name == "Type")
                .Which.Value.Should().BeOfType<ScalarValue>()
                .Which.Value.Should().BeOfType<string>()
                .Which.Should().Be("System.Boolean");
            parameter.Properties.Should().Contain(e => e.Name == "Name")
                .Which.Value.Should().BeOfType<ScalarValue>()
                .Which.Value.Should().BeOfType<string>()
                .Which.Should().Be("fileInfo");

            if (fileInfo)
            {
                var file = caller.Properties.Should().ContainSingle(e => e.Name == "File")
                    .Which.Value.Should().BeOfType<StructureValue>().Subject;
                file.Properties.Should().ContainSingle(e => e.Name == "Path")
                    .Which.Value.Should().BeOfType<ScalarValue>()
                    .Which.Value.Should().BeOfType<string>()
                    .Which.Should().EndWith("Serilog.Enrichers.WithCaller/Serilog.Enrichers.WithCallerTests/CallerEnricherTests.cs");
                file.Properties.Should().ContainSingle(e => e.Name == "Line")
                    .Which.Value.Should().BeOfType<ScalarValue>()
                    .Which.Value.Should().Be(95);
                file.Properties.Should().ContainSingle(e => e.Name == "Column")
                    .Which.Value.Should().BeOfType<ScalarValue>()
                    .Which.Value.Should().Be(13);
            }
            else
            {
                caller.Properties.Should().NotContain(e => e.Name == "File");
            }
        }
    }
}