using System;
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
        public const string CallerPropertyName = "Caller";
        public const string CallerInfoPropertyName = "CallerInfo";
        public const string ClassPropertyName = "Class";
        public const string NamespacePropertyName = "Namespace";
        public const string MethodPropertyName = "Method";
        public const string NamePropertyName = "Name";
        public const string TypePropertyName = "Type";
        public const string ParametersPropertyName = "Parameters";
        public const string FilePropertyName = "File";
        public const string PathPropertyName = "Path";
        public const string LinePropertyName = "Line";
        public const string ColumnPropertyName = "Column";

        private readonly bool _includeFileInfo;
        private readonly int _maxDepth;
        private Predicate<MethodBase> _filter;

        public CallerEnricher()
            : this(false, 1)
        {
            // added default constructor again so one can use the generic Enrich.With<CallerEnricher>() method
        }

        public CallerEnricher(bool? includeFileInfo, int maxDepth)
            : this(includeFileInfo, maxDepth, method => method.DeclaringType.Assembly == typeof(Log).Assembly)
        {
        }

        public CallerEnricher(bool? includeFileInfo, int maxDepth, Predicate<MethodBase> filter)
        {
            _includeFileInfo = includeFileInfo ?? false;    // Ignored - adjust outputTemplate accordingly
            _maxDepth = Math.Max(1, maxDepth);
            _filter = filter;
        }

        public static int SkipFramesCount { get; set; } = 3;
        public static int MaxFrameCount { get; set; } = 128;

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            int foundFrames = 0;
            StringBuilder caller = new StringBuilder();

            int skipFrames = SkipFramesCount;
            while (skipFrames < MaxFrameCount)
            {
                StackFrame stack = new StackFrame(skipFrames, _includeFileInfo);
                if (!stack.HasMethod())
                {
                    logEvent.AddPropertyIfAbsent(new LogEventProperty(CallerPropertyName, new ScalarValue("<unknown method>")));
                    logEvent.AddPropertyIfAbsent(new LogEventProperty(CallerInfoPropertyName, new StructureValue(Enumerable.Empty<LogEventProperty>())));
                    return;
                }

                MethodBase method = stack.GetMethod();

                if (_filter(method))
                {
                    skipFrames++;
                    continue;
                }

                if (foundFrames > 0)
                {
                    caller.Append(" at ");
                }

                var callerType = $"{method.DeclaringType.FullName}";
                var callerMethod = $"{method.Name}";
                if (!(stack.GetFileName() is string callerFileName))
                {
                    callerFileName = "";
                }

                var callerLineNo = stack.GetFileLineNumber();
                var callerParameters = GetParameterFullNames(method.GetParameters());

                caller.Append($"{callerType}.{callerMethod}({callerParameters})");
                if (!string.IsNullOrEmpty(callerFileName))
                {
                    caller.Append($" {callerFileName}:{callerLineNo}");
                }

                foundFrames++;

                if (foundFrames == 1)
                {
                    logEvent.AddPropertyIfAbsent(new LogEventProperty(CallerInfoPropertyName, CreateCallerStructureValue(method, stack)));
                }

                if (_maxDepth <= foundFrames)
                {
                    logEvent.AddPropertyIfAbsent(new LogEventProperty(CallerPropertyName, new ScalarValue(caller.ToString())));
                    return;
                }

                skipFrames++;
            }
        }

        private static StructureValue CreateCallerStructureValue(MethodBase method, StackFrame stackFrame)
        {
            var properties = new List<LogEventProperty>
            {
                new LogEventProperty(ClassPropertyName, new ScalarValue(method.DeclaringType?.Name)),
                new LogEventProperty(NamespacePropertyName, new ScalarValue(method.DeclaringType?.Namespace)),
                new LogEventProperty(MethodPropertyName, new StructureValue(new[]
                {
                    new LogEventProperty(NamePropertyName, new ScalarValue(method.Name)),
                    new LogEventProperty(ParametersPropertyName, new SequenceValue(method.GetParameters().Select(p =>
                    {
                        return new StructureValue(new[]
                        {
                            new LogEventProperty(TypePropertyName, new ScalarValue(p.ParameterType.FullName)),
                            new LogEventProperty(NamePropertyName, new ScalarValue(p.Name)),
                        });
                    }))),
                })),
            };
            var path = stackFrame.GetFileName();
            if (path != null)
            {
                properties.Add(new LogEventProperty(FilePropertyName, new StructureValue(new[]
                {
                    new LogEventProperty(PathPropertyName, new ScalarValue(path)),
                    new LogEventProperty(LinePropertyName, new ScalarValue(stackFrame.GetFileLineNumber())),
                    new LogEventProperty(ColumnPropertyName, new ScalarValue(stackFrame.GetFileColumnNumber())),
                })));
            }
            return new StructureValue(properties);
        }

        private string GetParameterFullNames(ParameterInfo[] parameterInfos, string separator = ", ")
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
