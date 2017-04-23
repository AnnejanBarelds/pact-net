using System;
using System.IO;
using System.Linq;
using System.Text;
using Thinktecture.IO;
using Thinktecture.IO.Adapters;

namespace PactNet.Infrastructure.Logging
{
    internal class LocalRollingLogFileMessageHandler : ILocalLogMessageHandler
    {
        public string LogPath { get; set; }

        private readonly object _sync = new object();
        private readonly IStreamWriter _writer;

        internal LocalRollingLogFileMessageHandler(IFile fileAdapter, string logFilePath)
        {
            LogPath = logFilePath;
            TryCreateDirectory(logFilePath);
            var file = fileAdapter.Open(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriterAdapter(file, Encoding.UTF8);
        }

        internal LocalRollingLogFileMessageHandler(string logFilePath)
            : this(new FileAdapter(), logFilePath)
        {
        }

        public void Handle(LocalLogMessage logMessage)
        {
            var messageFormat = logMessage.MessagePredicate != null ?
                logMessage.MessagePredicate() :
                String.Empty;

            string message;
            if (logMessage.Exception != null)
            {
                message = String.Format("{0}. Exception: {1} - {2}", messageFormat, logMessage.Exception, logMessage.Exception.StackTrace);
            }
            else if (logMessage.FormatParameters != null && logMessage.FormatParameters.Any())
            {
                message = String.Format(messageFormat, logMessage.FormatParameters);
            }
            else
            {
                message = messageFormat;
            }
            
            lock (_sync)
            {
                _writer.WriteLine("{0} [{1}] {2}", logMessage.DateTimeFormatted, logMessage.Level, message);
                _writer.Flush();
            }
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.Dispose();
            }
        }

        private static void TryCreateDirectory(string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not create log directory.", ex);
            }
        }
    }
}