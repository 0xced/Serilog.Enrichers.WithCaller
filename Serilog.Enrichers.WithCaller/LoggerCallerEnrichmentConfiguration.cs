﻿using Serilog.Configuration;

namespace Serilog.Enrichers.WithCaller
{
    public static class LoggerCallerEnrichmentConfiguration
    {
        public static LoggerConfiguration WithCaller(this LoggerEnrichmentConfiguration enrichmentConfiguration, bool includeFileInfo = false, int maxDepth = 3)
        {
            return enrichmentConfiguration.With(new CallerEnricher(includeFileInfo));
        }
    }
}
