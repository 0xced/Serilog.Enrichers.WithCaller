using Serilog;
using Serilog.Enrichers.WithCaller;

var logger = new LoggerConfiguration()
    .Enrich.WithCaller(CallerPropertyType.Structure, includeFileInfo: true)
    .WriteTo.Console()
    .CreateLogger();

  logger.Information("Hello, who is calling me? {@Caller}");