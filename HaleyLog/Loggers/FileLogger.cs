using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using Haley.Utils;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace Haley.Log
{
    public sealed class FileLogger : MicroLoggerBase
    {
        //If a logger of a certain output (say JSON) is writing to a file, then another logger of output type (say Text) should not be encouraged.
        //if two loggers tri

        private static ConcurrentDictionary<string, IProducerConsumerService> _targetServices = new ConcurrentDictionary<string, IProducerConsumerService>();

        //Each loggerbase will have it's own Producer Consumer Implementation. 
        //The different methods (via different threads) should/could produce and add it to the collection
        //One single thread will consume and then write to the files.
        #region ATTRIBUTES
        OutputType _outputType { get; set; }
        string _outputDirectory { get; set; }
        string _fileName { get; set; }
        bool _shouldGenerateEachDay { get; set; }
        IProducerConsumerService? _producerService; //Each non-rolling target file has one single producer consumer service.
        ConcurrentDictionary<DateTime, IProducerConsumerService> _dailyProducerServices =
            new ConcurrentDictionary<DateTime, IProducerConsumerService>();
        #endregion

        #region Private Helper Methods
        private bool checkDirectoryAccess()
        {
            try
            {
                //if directory doesn't exist, try to create it. If unable to create, then it means, access is denied.
                if (!Directory.Exists(_outputDirectory)) Directory.CreateDirectory(_outputDirectory);

                var _tempName = Path.GetFileName(Path.GetTempFileName());
                string tempfilepath = Path.Combine(_outputDirectory, _tempName);
                //if directory exists, then we need to check if the user has write access to it.
                using (FileStream fs = File.Create(tempfilepath)) { }
                File.Delete(tempfilepath);
                return true;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($@"Log writer doesn't have sufficient rights to write in the directory {_outputDirectory}", ex);
            }
        } //First step to be done.
        #endregion

        #region Overridden Methods
        public override string GetOutputLocation()
        {
            return _outputDirectory;
        }

        public override void Log(LogData data)
        {
            //Don't write directly using the writer. Use a producer/consumer pattern based implementation.
            //Write all log to a collection. Consumer will then consume them and write using the writer.
            if (data == null) return;
            var producer = _shouldGenerateEachDay
                ? _dailyProducerServices.GetOrAdd(
                    data.TimeStamp.Date,
                    date => GetOrCreateProducerService(DefineLogWriter(GetTargetFileName(date))))
                : _producerService;
            producer?.Produce(data);
        }

        #endregion

        #region Initiations
      
        string GetSubFolder() {
            var asmly = Assembly.GetEntryAssembly();
            return asmly.GetInfo(AssemblyInfo.Configuration) + "_" + asmly.GetInfo(AssemblyInfo.Version);
        }

        private bool ProcessOutputDirectory(FileLoggerOptions options)
        {
            if (options.DirPriority == DirectoryPriority.LocalAppData) {
                //Last Fall back preference
                if (string.IsNullOrWhiteSpace(options.OutputDirectory)) {
                    options.OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HaleyLogs", AppDomain.CurrentDomain?.FriendlyName ?? "AppLogs", GetSubFolder() ?? "Default" );
                }
            }

            //First preference (ENTRY ASSEMBLY FOLDER).
            if (string.IsNullOrWhiteSpace(options.OutputDirectory))
            {
                var _entryAssembly = Assembly.GetEntryAssembly();
                if (_entryAssembly != null)
                {
                    options.OutputDirectory = Path.Combine(Path.GetDirectoryName(_entryAssembly.Location), "Logs");
                }
            }

            //Second preference
            if (string.IsNullOrWhiteSpace(options.OutputDirectory))
            {
                options.OutputDirectory = Path.Combine(AppDomain.CurrentDomain?.BaseDirectory, "Logs"); ;
            }

            //Last Fall back preference
            if (string.IsNullOrWhiteSpace(options.OutputDirectory))
            {
                options.OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Haley", AppDomain.CurrentDomain?.FriendlyName ?? "AppLogs", GetSubFolder() ?? "Default");
            }

            ////Add a subfolder to the outputdirectory
            //if (!string.IsNullOrWhiteSpace(options.OutputDirectory))
            //{
            //    options.OutputDirectory = Path.Combine(options.OutputDirectory, "Logs");
            //}
            _outputDirectory = options.OutputDirectory; //Get directory

            checkDirectoryAccess();
            if (string.IsNullOrWhiteSpace(_outputDirectory)) return false;
            return true;
        }

        public FileLogger(string name,FileLoggerOptions options) :base(name ?? "HLogger",options.AllowedLogLevel)
        {
            _outputType = options.Type;
            _shouldGenerateEachDay = options.ShouldGenerateEachDay;
            if (!ProcessOutputDirectory(options))
            {
                throw new ArgumentException($@"Unable to process output directory {_outputDirectory}");
            }

            _fileName = options.FileName;
            if (string.IsNullOrWhiteSpace(_fileName))
            {
                _fileName = AppDomain.CurrentDomain?.FriendlyName ?? "AppLog";
                //If the filename is not provided, use the friendly name of the current domain.
            }

            if (!_shouldGenerateEachDay)
            {
                //Preserve the original default naming convention for non-rolling logs.
                if (string.IsNullOrWhiteSpace(options.FileName))
                {
                    _fileName = GetTargetFileName(DateTime.Now.Date, includeDate: true);
                }
                _producerService = GetOrCreateProducerService(DefineLogWriter(_fileName));
            }
        }

        private string GetTargetFileName(DateTime date, bool includeDate = false)
        {
            if (!_shouldGenerateEachDay && !includeDate) return _fileName;
            return $@"{_fileName}_{date:yyyy-MM-dd}";
        }

        private IProducerConsumerService GetOrCreateProducerService(IFileLogWriter writer)
        {
            //FILE NAME IS THE MOST IMPORTANT KEY.
            //FOR EACH UNIQUE FILE, DIFFERENT THREADS PRODUCE INTO ONE BLOCKING COLLECTION.
            //FOR EACH UNIQUE FILE, ONE CONSUMER WRITES THE QUEUED ITEMS.
            return _targetServices.GetOrAdd(
                writer.OutputFilePath,
                _ => new ProducerConsumerService(writer));
        }

        private IFileLogWriter DefineLogWriter(string fileName)
        {
            switch (_outputType)
            {
                case OutputType.Json:
                    return new JSONLogWriter(_outputDirectory, fileName);
                case OutputType.Xml:
                    return new XMLLogWriter(_outputDirectory, fileName);
                case OutputType.Text_detailed:
                    return new DetailedTextLogWriter(_outputDirectory, fileName);
                case OutputType.Text_simple:
                    return new SimpleTextWriter(_outputDirectory, fileName);
                default:
                    throw new ArgumentOutOfRangeException(nameof(_outputType), _outputType, "Unsupported log output type.");
            }
        }
        
        #endregion
    }
}
