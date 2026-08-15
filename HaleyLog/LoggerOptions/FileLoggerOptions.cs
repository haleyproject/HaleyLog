using Haley.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Models
{
    public sealed class FileLoggerOptions
    {
        public string OutputDirectory { get; set; }
        public string FileName { get; set; }
        /// <summary>
        /// Writes to a date-suffixed file and switches target files when the
        /// timestamp of an incoming log record moves to another local date.
        /// </summary>
        public bool ShouldGenerateEachDay { get; set; }
        public LogLevel AllowedLogLevel { get; set; }= LogLevel.Information;
        public OutputType Type { get; set; } = OutputType.Text_simple;
        public DirectoryPriority DirPriority { get; set; } = DirectoryPriority.LocalAppData;
        public FileLoggerOptions() { }
    }
}
