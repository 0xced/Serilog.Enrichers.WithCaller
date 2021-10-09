using System;

using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Serilog.Core;
using Serilog.Enrichers.WithCaller;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using Serilog.Sinks.InMemory.Assertions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;

namespace Serilog.Enrichers.WithCaller.Tests
{
    [TestClass()]
    public class CallerEnricherTests
    {
        public static string LogMessageTemplate { get; } = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message} (at {Caller}){NewLine}{Exception}";
        public static LoggingLevelSwitch LoggingLevelSwitch { get; } = new LoggingLevelSwitch(Events.LogEventLevel.Verbose);

        public static InMemorySink InMemoryInstance => InMemorySink.Instance;

        public static ILogger CreateLogger(CallerPropertyType callerPropertyType, bool fileInfo)
        {
            return new LoggerConfiguration()
                            .Enrich.WithCaller(callerPropertyType, fileInfo)
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
        public void EnrichAsString()
        {
            var logger = CreateLogger(CallerPropertyType.String, fileInfo: false);
            logger.Information("hello");
            InMemoryInstance.Should()
                .HaveMessage("hello")
                .Appearing().Once()
                .WithProperty("Caller")
                .WithValue("Serilog.Enrichers.WithCaller.Tests.CallerEnricherTests.EnrichAsString()");
        }

        [TestMethod()]
        public void EnrichAsStringWithFileInfo()
        {
            var fileName = new StackFrame(fNeedFileInfo: true).GetFileName();
            var logger = CreateLogger(CallerPropertyType.String, fileInfo: true);
            logger.Information("hello");
            InMemoryInstance.Should()
                .HaveMessage("hello")
                .Appearing().Once()
                .WithProperty("Caller")
                .WithValue($"Serilog.Enrichers.WithCaller.Tests.CallerEnricherTests.EnrichAsStringWithFileInfo() {fileName}:63");
        }

        [TestMethod()]
        [DataRow(true)]
        [DataRow(false)]
        public void EnrichAsStructure(bool fileInfo)
        {
            var logger = CreateLogger(CallerPropertyType.Structure, fileInfo);
            logger.Information("hello");
            var value = InMemoryInstance.Should()
                .HaveMessage("hello")
                .Appearing().Once()
                .WithProperty("Caller")
                .Subject;

            var caller = value.Should().BeOfType<StructureValue>().Subject;
            caller.Properties.Should().ContainSingle(e => e.Name == "Class")
                .Which.Value.Should().BeOfType<ScalarValue>()
                .Which.Value.Should().Be("Serilog.Enrichers.WithCaller.Tests.CallerEnricherTests");
            var method = caller.Properties.Should().ContainSingle(e => e.Name == "Method")
                .Which.Value.Should().BeOfType<StructureValue>().Subject;
            method.Properties.Should().ContainSingle(e => e.Name == "Name")
                .Which.Value.Should().BeOfType<ScalarValue>()
                .Which.Value.Should().BeOfType<string>()
                .Which.Should().Be("EnrichAsStructure");
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
                    .Which.Value.Should().Be(77);
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