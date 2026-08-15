# Haley.Log

Haley's producer/consumer-backed file provider for Microsoft.Extensions.Logging.

## Registration

```csharp
builder.Logging.AddHaleyFileLogger(options =>
{
    options.OutputDirectory = Path.Combine(builder.Environment.ContentRootPath, "Logs");
    options.AllowedLogLevel = LogLevel.Information;
    options.ShouldGenerateEachDay = true;
});
```

Both registration overloads are idempotent. Calling either overload more than once still installs one `FileLogProvider` in the dependency-injection container.

When `ShouldGenerateEachDay` is enabled, Haley routes each record to the file identified by the record timestamp's local date. A continuously running host therefore starts writing to the next date-stamped file at midnight without a restart. When it is disabled, an explicit `FileName` remains fixed for the logger lifetime; otherwise the historical application-name and creation-date filename is used.

Exceptions supplied through the standard `ILogger` methods are retained and written by the Haley file pipeline.
