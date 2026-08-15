using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Haley.Models;
using Haley.Enums;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;

namespace Haley.Log
{
    public static class LoggerExtensions
    {
        public static ILoggingBuilder AddHaleyFileLogger(this ILoggingBuilder builder)
        {
            builder.AddConfiguration(); //add the configuration here.
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLogProvider>()); //no need to clear previous providers when using this implementation.
            LoggerProviderOptions.RegisterProviderOptions
                <FileLoggerOptions, FileLogProvider>(builder.Services);
            return builder;
        }

        public static ILoggingBuilder AddHaleyFileLogger(this ILoggingBuilder builder,Action<FileLoggerOptions> configure)
        {
            builder.AddHaleyFileLogger(); //set up the base.
            builder.Services.Configure(configure);
            return builder;
        }

    }
}
