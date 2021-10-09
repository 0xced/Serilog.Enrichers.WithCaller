using Serilog.Configuration;

namespace Serilog.Enrichers.WithCaller
{
    public static class LoggerCallerEnrichmentConfiguration
    {
        public static LoggerConfiguration WithCaller(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            return enrichmentConfiguration.With<CallerEnricher>();
        }

        public static LoggerConfiguration WithCaller(this LoggerEnrichmentConfiguration enrichmentConfiguration, bool includeFileInfo)
        {
            return enrichmentConfiguration.WithCaller(CallerPropertyType.String, includeFileInfo);
        }

        public static LoggerConfiguration WithCaller(this LoggerEnrichmentConfiguration enrichmentConfiguration, CallerPropertyType callerPropertyType, bool includeFileInfo)
        {
            return enrichmentConfiguration.With(new CallerEnricher(callerPropertyType, includeFileInfo));
        }
    }
}
