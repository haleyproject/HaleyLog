using Haley.Enums;
using Haley.Models;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Haley.Utils;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Haley.Abstractions
{
    public abstract class MicroLoggerBase : IMicroLogger, IDisposable
    {
        public string Name { get; }
        public LogLevel AllowedLevel { get; protected set; }
        internal protected void SetAllowedLevel(LogLevel newLevel)
        {
            AllowedLevel = newLevel;
        }
        #region DebugMethods
        public void Debug(string message, string title = null)
        {
            Log(message, LogLevel.Debug, title);
        }
        public void Debug(Exception exception,string message = null, string title = null )
        {
            Log(message,exception,LogLevel.Debug, title);
        }
        public void Debug(string key, string value, string title = null)
        {
            Log(key, value,LogLevel.Debug, title);
        }

        public void Trace(string message, string title = null)
        {
            Log(message, LogLevel.Trace, title);
        }

        public void Trace(Exception exception,string message = null, string title = null)
        {
            Log(message,exception,LogLevel.Trace, title);
        }

        public void Trace(string key, string value, string title = null)
        {
            Log(key, value,LogLevel.Trace, title);
        }
        public void Log(string message, LogLevel log_level, string title = null)
        {
            Log(message, null, log_level, default(EventId), title); //No exception, no event information.
        }
        public void Info(string message, string title = null)
        {
            Log(message, LogLevel.Information, title);
        }

        public void Warn(string message, string title = null)
        {
            Log(message, LogLevel.Warning, title);
        }

        public void Error(string message, string title = null)
        {
            Log(message, LogLevel.Error, title);
        }

        public void Exception(Exception exception,string message = null, string title = null)
        {
            Log(message,exception, LogLevel.Critical, title); //This is a critical exception.
        }

        public void Exception(Exception exception, EventId eventId, string title = null)
        {
            Log(null, exception, LogLevel.Critical, eventId, title);
        }

        public void Critical(string message, string title = null)
        {
            Log(message, LogLevel.Critical, title);
        }

        public void Trace(Exception exception, EventId eventId, string title = null)
        {
            Log(null, exception, LogLevel.Trace, eventId, title);
        }

        public void Debug(Exception exception, EventId eventId, string title = null)
        {
            Log(null, exception, LogLevel.Debug, eventId, title);
        }
       
        public void Log(string message, Exception exception, LogLevel log_level, string title = null)
        {
            Log(message, exception, log_level, default(EventId), title);
        }
        public void Log(string message, Exception exception, LogLevel log_level, EventId eventId, string title)
        {
            if (!IsEnabled(log_level)) return; //Do not proceed, if not enabled.
            //Covert information to LogData and send to write.
            Log(GetLogData(message, exception, log_level, eventId, title));
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }
        #endregion

        #region Abstract Methods
        public abstract string GetOutputLocation();
        public abstract void Log(LogData data); //Whole magic should happen here.
        #endregion

        #region Virtual Methods
        public virtual void Log(string key, string value, LogLevel log_level, string title = null)
        {
            //
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                Log($@" {key} : {value} ", log_level, title);
            }
        }
        public virtual bool IsEnabled(LogLevel logLevel)
        {
            //say, allowed level is Information (2), and the loglevel is Debug(1), then we should not write the debug information.
            //incoming loglevel is lower than the allowed level, so we cannot allow this. In other words, allowed level should always be lower than the incoming log level.
            return (logLevel== AllowedLevel || AllowedLevel < logLevel); 
        }
        
        public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return; //Do not even process the formatter and waste resources
            //Virtual method.
            if (formatter == null)
            {
                System.Diagnostics.Debug.WriteLine("Formatter is null. Cannot get the message.");
                return;
            }
            try
            {
                var _msg = formatter.Invoke(state, exception);
                //ILogger formatters normally render state only. Preserve the exception
                //as a separate value so file writers can include its type, message and trace.
                Log(_msg, exception, logLevel, eventId, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
        public virtual void Dispose()
        {
            //To do when the item is disposed
        }

        #endregion
        protected LogData GetLogData(string message, Exception exception, LogLevel log_level, EventId eventId, string title)
        {
            LogData _data = new LogData()
            {
                Message = message,
                Exception = exception,
                Loglevel = log_level,
                EventId = eventId,
                Title = title,
                TimeStamp = DateTime.Now,//Timer when this is created.
                ModuleName = Name, //Name of the logger
            };
            return _data;
        }
        #region Initiations
        public MicroLoggerBase(string name,LogLevel allowed_level)
        { 
            Name = name;
            //Allowed level can be changed during runtime
            AllowedLevel = allowed_level; 
        }
        #endregion
    }
}
