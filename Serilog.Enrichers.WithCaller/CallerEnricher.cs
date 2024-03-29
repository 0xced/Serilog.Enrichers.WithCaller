﻿using System;
using System.Reflection;
using System.Diagnostics;

using Serilog.Core;
using Serilog.Events;
using System.Text;

namespace Serilog.Enrichers.WithCaller
{
    public class CallerEnricher : ILogEventEnricher
    {
        public static int SkipFramesCount { get; set; } = 3;
        public static int MaxFrameCount { get; set; } = 128;

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            int skipFrames = SkipFramesCount;
            while (skipFrames < MaxFrameCount)
            {
                StackFrame stack = new StackFrame(skipFrames);
                if (!stack.HasMethod())
                {
                    logEvent.AddPropertyIfAbsent(new LogEventProperty("Caller", new ScalarValue("<unknown method>")));
                    return;
                }

                MethodBase method = stack.GetMethod();
                if (method.DeclaringType.Assembly != typeof(Log).Assembly)
                {
                    string caller = $"{method.DeclaringType.FullName}.{method.Name}({GetParameterFullNames(method.GetParameters())})";
                    logEvent.AddPropertyIfAbsent(new LogEventProperty("Caller", new ScalarValue(caller)));
                    return;
                }

                skipFrames++;
            }
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
