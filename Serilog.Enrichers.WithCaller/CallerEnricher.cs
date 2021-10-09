using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using Serilog.Core;
using Serilog.Events;
using System.Text;

namespace Serilog.Enrichers.WithCaller
{
    public class CallerEnricher : ILogEventEnricher
    {
        private readonly CallerPropertyType _callerPropertyType;
        private readonly bool _includeFileInfo;

        public CallerEnricher() : this(CallerPropertyType.String, includeFileInfo: false)
        {
        }

        public CallerEnricher(bool includeFileInfo) : this(CallerPropertyType.String, includeFileInfo)
        {
        }

        public CallerEnricher(CallerPropertyType callerPropertyType, bool includeFileInfo)
        {
            _callerPropertyType = callerPropertyType;
            _includeFileInfo = includeFileInfo;
        }

        public static int SkipFramesCount { get; set; } = 3;
        public static int MaxFrameCount { get; set; } = 128;

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            int skipFrames = SkipFramesCount;
            while (skipFrames < MaxFrameCount)
            {
                StackFrame stack = new StackFrame(skipFrames, _includeFileInfo);
                if (!stack.HasMethod())
                {
                    LogEventProperty property;
                    if (_callerPropertyType == CallerPropertyType.String)
                    {
                        property = new LogEventProperty("Caller", new ScalarValue("<unknown method>"));
                    }
                    else
                    {
                        property = new LogEventProperty("Caller", new StructureValue(Enumerable.Empty<LogEventProperty>()));
                    }
                    logEvent.AddPropertyIfAbsent(property);

                    return;
                }

                MethodBase method = stack.GetMethod();
                if (method.DeclaringType != null && method.DeclaringType.Assembly != typeof(Log).Assembly)
                {
                    LogEventProperty property;
                    if (_callerPropertyType == CallerPropertyType.String)
                    {
                        StringBuilder caller = new StringBuilder($"{method.DeclaringType.FullName}.{method.Name}({GetParameterFullNames(method.GetParameters())})");
                        string fileName = stack.GetFileName();
                        if (fileName != null)
                        {
                            caller.Append($" {fileName}:{stack.GetFileLineNumber()}");
                        }
                        property = new LogEventProperty("Caller", new ScalarValue(caller.ToString()));
                    }
                    else
                    {
                        var caller = CreateCallerStructureValue(method, stack);
                        property = new LogEventProperty("Caller", caller);
                    }
                    logEvent.AddPropertyIfAbsent(property);

                    return;
                }

                skipFrames++;
            }
        }

        private static StructureValue CreateCallerStructureValue(MethodBase method, StackFrame stackFrame)
        {
            var properties = new List<LogEventProperty>
            {
                new LogEventProperty("Class", new ScalarValue(method.DeclaringType?.FullName)),
                new LogEventProperty("Method", new StructureValue(new[]
                {
                    new LogEventProperty("Name", new ScalarValue(method.Name)),
                    new LogEventProperty("Parameters", new SequenceValue(method.GetParameters().Select(p => new StructureValue(new[]
                    {
                        new LogEventProperty("Type", new ScalarValue(p.ParameterType.FullName)),
                        new LogEventProperty("Name", new ScalarValue(p.Name)),
                    })))),
                })),
            };
            var path = stackFrame.GetFileName();
            if (path != null)
            {
                properties.Add(new LogEventProperty("File", new StructureValue(new[]
                {
                    new LogEventProperty("Path", new ScalarValue(path)),
                    new LogEventProperty("Line", new ScalarValue(stackFrame.GetFileLineNumber())),
                    new LogEventProperty("Column", new ScalarValue(stackFrame.GetFileColumnNumber())),
                })));
            }
            return new StructureValue(properties);
        }

        private static string GetParameterFullNames(ParameterInfo[] parameterInfos, string separator = ", ")
        {
            int len = parameterInfos?.Length ?? 0;
            var sb = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                sb.Append(parameterInfos[i].ParameterType.FullName);
                if (i < len - 1)
                    sb.Append(separator);
            }
            return sb.ToString();
        }
    }
}
