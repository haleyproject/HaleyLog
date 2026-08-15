using Haley.Enums;
using Haley.Log;
using Haley.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HaleyLog.Tests;

public sealed class FileLoggerTests
{
    [Fact]
    public void ConfiguredExtensionRegistersOnlyOneFileProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddHaleyFileLogger(_ => { });
            builder.AddHaleyFileLogger(_ => { });
        });

        var registrations = services.Where(descriptor =>
            descriptor.ServiceType == typeof(ILoggerProvider) &&
            descriptor.ImplementationType == typeof(FileLogProvider));

        Assert.Single(registrations);
    }

    [Fact]
    public async Task DailyFileOptionRoutesRecordsByTheirLocalDate()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var logger = new FileLogger(
                "daily-rollover-test",
                new FileLoggerOptions
                {
                    OutputDirectory = directory,
                    FileName = "daily",
                    ShouldGenerateEachDay = true,
                    AllowedLogLevel = LogLevel.Information,
                    Type = OutputType.Text_simple
                });

            logger.Log(CreateLogData(new DateTime(2026, 8, 16, 23, 59, 59), "before-midnight"));
            logger.Log(CreateLogData(new DateTime(2026, 8, 17, 0, 0, 1), "after-midnight"));

            var firstFile = Path.Combine(directory, "daily_2026-08-16.txt");
            var secondFile = Path.Combine(directory, "daily_2026-08-17.txt");
            await WaitForContentAsync(firstFile, "before-midnight");
            await WaitForContentAsync(secondFile, "after-midnight");

            Assert.DoesNotContain("after-midnight", await File.ReadAllTextAsync(firstFile));
            Assert.DoesNotContain("before-midnight", await File.ReadAllTextAsync(secondFile));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task MicrosoftLoggerExceptionIsPreservedInFileOutput()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            using var factory = LoggerFactory.Create(builder =>
                builder.AddHaleyFileLogger(options =>
                {
                    options.OutputDirectory = directory;
                    options.FileName = "exceptions";
                    options.AllowedLogLevel = LogLevel.Information;
                }));
            var logger = factory.CreateLogger($"exception-test-{Guid.NewGuid():N}");
            var exception = new InvalidOperationException("preserved-exception-message");

            logger.LogError(exception, "The operation failed.");

            var file = Path.Combine(directory, "exceptions.txt");
            await WaitForContentAsync(file, "preserved-exception-message");
            var content = await File.ReadAllTextAsync(file);
            Assert.Contains(nameof(InvalidOperationException), content);
            Assert.Contains("The operation failed.", content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static LogData CreateLogData(DateTime timestamp, string message) => new()
    {
        TimeStamp = timestamp,
        Loglevel = LogLevel.Information,
        ModuleName = "HaleyLog.Tests",
        Message = message
    };

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "HaleyLog.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static async Task WaitForContentAsync(string path, string expected)
    {
        var timeout = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < timeout)
        {
            if (File.Exists(path) && (await File.ReadAllTextAsync(path)).Contains(expected, StringComparison.Ordinal))
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException($"The expected Haley log content was not written to '{path}'.");
    }
}
