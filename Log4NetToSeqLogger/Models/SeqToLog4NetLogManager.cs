using log4net;
using log4net.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace SeqToLog4NetLogger.Models
{
    public static class SeqToLog4NetLogManager
    {
        private const string BulkUploadResource = "api/events/raw";
        private const string ApiKeyHeaderName = "X-Seq-ApiKey";
        private static string SeqServerUrl = null;
        private static string ApiKey = null;
        private static bool LogToLog4Net = true;

        /// <summary>
        /// Configure the log manager
        /// </summary>
        /// <param name="seqServerUrl">The url to your Seq server</param>
        /// <param name="logToLog4Net">If you log to one of the methods that log both to Seq and Log4net. Do you want to log an extra time to log4net? This could cause double logging to Seq if you have a Seq log4net appender that is also logging to Seq</param>
        /// <param name="apiKey">If you have setup Seq to use API keys, then add your api key here. Otherwise leave it blank</param>
        public static void Configure(string seqServerUrl, bool logToLog4Net = true, string apiKey = null)
        {
            SeqServerUrl = seqServerUrl;
            ApiKey = apiKey;
            LogToLog4Net = logToLog4Net;
        }

        /// <summary>
        /// Get the logger. (Standard log4net get logger call)
        /// </summary>
        /// <param name="type">The class type</param>
        /// <param name="logToLog4Net">(Optional) Override the behaviour of what to log. Should we log to log4net?</param>
        /// <param name="logToSeq">(Optional) Override the behaviour of what to log. Should we log to Seq?</param>
        /// <returns>A logger</returns>
        public static IStructuredLogger GetLogger(Type type, bool? logToLog4Net = null)
        {
            return new StructuredLogger(type, logToLog4Net ?? LogToLog4Net);
        }

        /// <summary>
        /// Get the logger. (Standard log4net get logger call)
        /// </summary>
        /// <param name="name">The name of the logger</param>
        /// <param name="logToLog4Net">(Optional) Override the behaviour of what to log. Should we log to log4net?</param>
        /// <param name="logToSeq">(Optional) Override the behaviour of what to log. Should we log to Seq?</param>
        /// <returns>A logger</returns>
        public static IStructuredLogger GetLogger(string name, bool? logToLog4Net = null)
        {
            return new StructuredLogger(name, logToLog4Net ?? LogToLog4Net);
        }

        /// <summary>
        /// Send data to the Seq server (if the server url is set using configure)
        /// </summary>
        /// <param name="logEvents">Events to send</param>
        internal static void SendToSeqServer(LoggingEvent[] logEvents)
        {
            if (string.IsNullOrEmpty(SeqServerUrl)) return;

            try
            {
                var payload = new StringWriter();
                payload.Write("{\"events\":[");
                LoggingEventFormatter.ToJson(logEvents, payload);
                payload.Write("]}");
                var payloadData = payload.ToString();

                var content = new StringContent(payloadData, Encoding.UTF8, "application/json");
                if (!string.IsNullOrWhiteSpace(ApiKey))
                    content.Headers.Add(ApiKeyHeaderName, ApiKey);

                var baseUri = SeqServerUrl;
                if (!baseUri.EndsWith("/"))
                    baseUri += "/";

                using (var httpClient = new HttpClient { BaseAddress = new Uri(baseUri) })
                {
                    var result = httpClient.PostAsync(BulkUploadResource, content).Result;

                    // If desired, add some action if the transaction failed and no data could be sent to the seq log server

                    //if (!result.IsSuccessStatusCode)
                    //    ErrorHandler.Error(string.Format("Received failed result {0}: {1}", result.StatusCode, result.Content.ReadAsStringAsync().Result));
                }
            }
            catch (Exception e)
            {
                // If desired, add some action if the transaction failed and no data could be sent to the seq log server
            }
        }

        public class StructuredLogger : IStructuredLogger
        {
            private ILog Log = null;
            private Type LoggerType = null;
            private bool LogToLog4Net;

            internal StructuredLogger(Type type, bool logToLog4Net)
            {
                Log = LogManager.GetLogger(type);
                LoggerType = type;
                LogToLog4Net = logToLog4Net;
            }

            internal StructuredLogger(string name, bool logToLog4Net)
            {
                Log = LogManager.GetLogger(name);
                LogToLog4Net = logToLog4Net;
            }

            public bool IsDebugEnabled
            {
                get
                {
                    return Log.IsDebugEnabled;
                }
            }

            public bool IsErrorEnabled
            {
                get
                {
                    return Log.IsErrorEnabled;
                }
            }

            public bool IsFatalEnabled
            {
                get
                {
                    return Log.IsFatalEnabled;
                }
            }

            public bool IsInfoEnabled
            {
                get
                {
                    return Log.IsInfoEnabled;
                }
            }

            public bool IsWarnEnabled
            {
                get
                {
                    return Log.IsWarnEnabled;
                }
            }

            public ILogger Logger
            {
                get
                {
                    return Log.Logger;
                }
            }

            public void Debug(object message)
            {
                Log.Debug(message);
            }

            public void Debug(object message, Exception exception)
            {
                Log.Debug(message, exception);
            }

            public void Debug(object message, object structure)
            {
                if (LogToLog4Net)
                    Log.Debug(message);

                var loggingEvent = new LoggingEvent(LoggerType, Logger.Repository, Logger.Name, Level.Debug, message, null);

                if (structure != null)
                {
                    // Change this line if you want to use a different JSON serializer
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
                    loggingEvent.Properties["Data"] = jsonData;
                }
                var eventData = new[] { loggingEvent };
                SendToSeqServer(eventData);
            }

            public void Debug(object message, Exception exception, object structure)
            {
                if (LogToLog4Net)
                    Log.Debug(message, exception);

                var loggingEvent = new LoggingEvent(LoggerType, Logger.Repository, Logger.Name, Level.Debug, message, exception);

                if (structure != null)
                {
                    // Change this line if you want to use a different JSON serializer
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
                    loggingEvent.Properties["Data"] = jsonData;
                }
                var eventData = new[] { loggingEvent };
                SendToSeqServer(eventData);
            }

            public void DebugFormat(string format, object arg0)
            {
                Log.DebugFormat(format, arg0);
            }

            public void DebugFormat(string format, params object[] args)
            {
                Log.DebugFormat(format, args);
            }

            public void DebugFormat(IFormatProvider provider, string format, params object[] args)
            {
                Log.DebugFormat(provider, format, args);                
            }

            public void DebugFormat(string format, object arg0, object arg1)
            {
                Log.DebugFormat(format, arg0, arg1);
            }

            public void DebugFormat(string format, object arg0, object arg1, object arg2)
            {
                Log.DebugFormat(format, arg0, arg1, arg2);
            }

            public void Error(object message)
            {
                Log.Error(message);
            }

            public void Error(object message, Exception exception)
            {
                Log.Error(message, exception);
            }

            public void Error(object message, object structure)
            {
                if (LogToLog4Net)
                    Log.Error(message);

                var loggingEvent = new LoggingEvent(LoggerType, Logger.Repository, Logger.Name, Level.Error, message, null);

                if (structure != null)
                {
                    // Change this line if you want to use a different JSON serializer
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
                    loggingEvent.Properties["Data"] = jsonData;
                }
                var eventData = new[] { loggingEvent };
                SendToSeqServer(eventData);
            }

            public void Error(object message, Exception exception, object structure)
            {
                if (LogToLog4Net)
                    Log.Error(message, exception);

                var loggingEvent = new LoggingEvent(LoggerType, Logger.Repository, Logger.Name, Level.Error, message, exception);

                if (structure != null)
                {
                    // Change this line if you want to use a different JSON serializer
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
                    loggingEvent.Properties["Data"] = jsonData;
                }
                var eventData = new[] { loggingEvent };
                SendToSeqServer(eventData);
            }

            public void ErrorFormat(string format, object arg0)
            {
                Log.ErrorFormat(format, arg0);
            }

            public void ErrorFormat(string format, params object[] args)
            {
                Log.ErrorFormat(format, args);
            }

            public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
            {
                Log.ErrorFormat(provider, format, args);
            }

            public void ErrorFormat(string format, object arg0, object arg1)
            {
                Log.ErrorFormat(format, arg0, arg1);
            }

            public void ErrorFormat(string format, object arg0, object arg1, object arg2)
            {
                Log.ErrorFormat(format, arg0, arg1, arg2);
            }

            public void Fatal(object message)
            {
                Log.Fatal(message);
            }

            public void Fatal(object message, Exception exception)
            {
                Log.Fatal(message, exception);
            }

            public void Fatal(object message, object structure)
            {
                if (LogToLog4Net)
                    Log.Fatal(message);

                var loggingEvent = new LoggingEvent(LoggerType, Logger.Repository, Logger.Name, Level.Fatal, message, null);

                if (structure != null)
                {
                    // Change this line if you want to use a different JSON serializer
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
                    loggingEvent.Properties["Data"] = jsonData;
                }
                var eventData = new[] { loggingEvent };
                SendToSeqServer(eventData);
            }

            public void Fatal(object message, Exception exception, object structure)
            {
                if (LogToLog4Net)
                    Log.Fatal(message, exception);

                var loggingEvent = new LoggingEvent(LoggerType, Logger.Repository, Logger.Name, Level.Fatal, message, exception);

                if (structure != null)
                {
                    // Change this line if you want to use a different JSON serializer
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
                    loggingEvent.Properties["Data"] = jsonData;
                }
                var eventData = new[] { loggingEvent };
                SendToSeqServer(eventData);
            }

            public void FatalFormat(string format, object arg0)
            {
                Log.FatalFormat(format, arg0);
            }

            public void FatalFormat(string format, params object[] args)
            {
                Log.FatalFormat(format, args);
            }

            public void FatalFormat(IFormatProvider provider, string format, params object[] args)
            {
                Log.FatalFormat(provider, format, args);
            }

            public void FatalFormat(string format, object arg0, object arg1)
            {
                Log.FatalFormat(format, arg0, arg1);
            }

            public void FatalFormat(string format, object arg0, object arg1, object arg2)
            {
                Log.FatalFormat(format, arg0, arg1, arg2);
            }

            public void Info(object message)
            {
                Log.Info(message);
            }

            public void Info(object message, Exception exception)
            {
                Log.Info(message, exception);
            }

            public void Info(object message, object structure)
            {
                if (LogToLog4Net)
                    Log.Info(message);

                var loggingEvent = new LoggingEvent(LoggerType, Logger.Repository, Logger.Name, Level.Info, message, null);

                if (structure != null)
                {
                    // Change this line if you want to use a different JSON serializer
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
                    loggingEvent.Properties["Data"] = jsonData;
                }
                var eventData = new[] { loggingEvent };
                SendToSeqServer(eventData);
            }

            public void Info(object message, Exception exception, object structure)
            {
                if (LogToLog4Net)
                    Log.Info(message, exception);

                var loggingEvent = new LoggingEvent(LoggerType, Logger.Repository, Logger.Name, Level.Info, message, exception);

                if (structure != null)
                {
                    // Change this line if you want to use a different JSON serializer
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
                    loggingEvent.Properties["Data"] = jsonData;
                }
                var eventData = new[] { loggingEvent };
                SendToSeqServer(eventData);
            }

            public void InfoFormat(string format, object arg0)
            {
                Log.InfoFormat(format, arg0);
            }

            public void InfoFormat(string format, params object[] args)
            {
                Log.InfoFormat(format, args);
            }

            public void InfoFormat(IFormatProvider provider, string format, params object[] args)
            {
                Log.InfoFormat(provider, format, args);
            }

            public void InfoFormat(string format, object arg0, object arg1)
            {
                Log.InfoFormat(format, arg0, arg1);
            }

            public void InfoFormat(string format, object arg0, object arg1, object arg2)
            {
                Log.InfoFormat(format, arg0, arg1, arg2);
            }

            public void Warn(object message)
            {
                Log.Warn(message);
            }

            public void Warn(object message, Exception exception)
            {
                Log.Warn(message, exception);
            }

            public void Warn(object message, object structure)
            {
                if (LogToLog4Net)
                    Log.Warn(message);

                var loggingEvent = new LoggingEvent(LoggerType, Logger.Repository, Logger.Name, Level.Warn, message, null);

                if (structure != null)
                {
                    // Change this line if you want to use a different JSON serializer
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
                    loggingEvent.Properties["Data"] = jsonData;
                }
                var eventData = new[] { loggingEvent };
                SendToSeqServer(eventData);
            }

            public void Warn(object message, Exception exception, object structure)
            {
                if (LogToLog4Net)
                    Log.Warn(message, exception);

                var loggingEvent = new LoggingEvent(LoggerType, Logger.Repository, Logger.Name, Level.Warn, message, exception);

                if (structure != null)
                {
                    // Change this line if you want to use a different JSON serializer
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
                    loggingEvent.Properties["Data"] = jsonData;
                }
                var eventData = new[] { loggingEvent };
                SendToSeqServer(eventData);
            }

            public void WarnFormat(string format, object arg0)
            {
                Log.WarnFormat(format, arg0);
            }

            public void WarnFormat(string format, params object[] args)
            {
                Log.WarnFormat(format, args);
            }

            public void WarnFormat(IFormatProvider provider, string format, params object[] args)
            {
                Log.WarnFormat(provider, format, args);
            }

            public void WarnFormat(string format, object arg0, object arg1)
            {
                Log.WarnFormat(format, arg0, arg1);
            }

            public void WarnFormat(string format, object arg0, object arg1, object arg2)
            {
                Log.WarnFormat(format, arg0, arg1, arg2);
            }
        }              

        /// <summary>
        /// The LoggingEventFormatter is taken from the Seq client project that can be found here.
        /// https://github.com/continuousit/seq-client
        /// It has been partly modified to allow custom data to be sent in.
        /// </summary>
        internal static class LoggingEventFormatter
        {
            static readonly IDictionary<Type, Action<object, TextWriter>> _literalWriters;
            const uint Log4NetEventType = 0x00010649;

            static LoggingEventFormatter()
            {
                _literalWriters = new Dictionary<Type, Action<object, TextWriter>>
                {
                    { typeof(bool), (v, w) => WriteBoolean((bool)v, w) },
                    { typeof(char), (v, w) => WriteString(((char)v).ToString(CultureInfo.InvariantCulture), w) },
                    { typeof(byte), WriteToString },
                    { typeof(sbyte), WriteToString },
                    { typeof(short), WriteToString },
                    { typeof(ushort), WriteToString },
                    { typeof(int), WriteToString },
                    { typeof(uint), WriteToString },
                    { typeof(long), WriteToString },
                    { typeof(ulong), WriteToString },
                    { typeof(float), WriteToString },
                    { typeof(double), WriteToString },
                    { typeof(decimal), WriteToString },
                    { typeof(string), (v, w) => WriteString((string)v, w) },
                    { typeof(DateTime), (v, w) => WriteDateTime((DateTime)v, w) },
                    { typeof(DateTimeOffset), (v, w) => WriteOffset((DateTimeOffset)v, w) },
                };
            }

            static readonly IDictionary<string, string> _levelMap = new Dictionary<string, string>
            {
                { "DEBUG", "Debug" },
                { "INFO", "Information" },
                { "WARN", "Warning" },
                { "ERROR", "Error" },
                { "FATAL", "Fatal" }
            };

            public static void ToJson(LoggingEvent[] events, StringWriter payload)
            {
                var delim = "";
                foreach (var loggingEvent in events)
                {
                    payload.Write(delim);
                    delim = ",";
                    ToJson(loggingEvent, payload);
                }
            }

            static void ToJson(LoggingEvent loggingEvent, StringWriter payload)
            {
                string level;
                if (!_levelMap.TryGetValue(loggingEvent.Level.Name, out level))
                    level = "Information";

                payload.Write("{");

                var delim = "";
                var offsetTimestamp = new DateTimeOffset(loggingEvent.TimeStamp, DateTimeOffset.Now.Offset);
                WriteJsonProperty("Timestamp", offsetTimestamp, ref delim, payload);
                WriteJsonProperty("Level", level, ref delim, payload);
                WriteJsonProperty("EventType", Log4NetEventType, ref delim, payload);

                var escapedMessage = loggingEvent.RenderedMessage.Replace("{", "{{").Replace("}", "}}");
                WriteJsonProperty("MessageTemplate", escapedMessage, ref delim, payload);

                if (loggingEvent.ExceptionObject != null)
                    WriteJsonProperty("Exception", loggingEvent.ExceptionObject, ref delim, payload);

                payload.Write(",\"Properties\":{");

                var seenKeys = new HashSet<string>();

                var pdelim = "";

                WriteJsonProperty(SanitizeKey("log4net:Logger"), loggingEvent.LoggerName, ref pdelim, payload);

                foreach (DictionaryEntry property in loggingEvent.GetProperties())
                {
                    var sanitizedKey = SanitizeKey(property.Key.ToString());
                    if (seenKeys.Contains(sanitizedKey))
                        continue;

                    seenKeys.Add(sanitizedKey);
                    if (sanitizedKey == "Data")
                        WriteJsonEscapedProperty(sanitizedKey, property.Value as string, ref pdelim, payload);
                    else
                        WriteJsonProperty(sanitizedKey, property.Value, ref pdelim, payload);
                }
                payload.Write("}");
                payload.Write("}");
            }

            static string SanitizeKey(string key)
            {
                return new string(key.Replace(":", "_").Where(c => c == '_' || char.IsLetterOrDigit(c)).ToArray());
            }

            static void WriteJsonProperty(string name, object value, ref string precedingDelimiter, TextWriter output)
            {
                output.Write(precedingDelimiter);
                WritePropertyName(name, output);
                WriteLiteral(value, output);
                precedingDelimiter = ",";
            }

            static void WriteJsonEscapedProperty(string name, string value, ref string precedingDelimiter, TextWriter output)
            {
                output.Write(precedingDelimiter);
                WritePropertyName(name, output);

                if (string.IsNullOrEmpty(value))
                    value = "\"null\"";

                output.Write(value);
                precedingDelimiter = ",";
            }

            static void WritePropertyName(string name, TextWriter output)
            {
                output.Write("\"");
                output.Write(name);
                output.Write("\":");
            }

            static void WriteLiteral(object value, TextWriter output)
            {
                if (value == null)
                {
                    output.Write("null");
                    return;
                }

                Action<object, TextWriter> writer;
                if (_literalWriters.TryGetValue(value.GetType(), out writer))
                {
                    writer(value, output);
                    return;
                }

                WriteString(value.ToString(), output);
            }

            static void WriteToString(object number, TextWriter output)
            {
                output.Write(number.ToString());
            }

            static void WriteBoolean(bool value, TextWriter output)
            {
                output.Write(value ? "true" : "false");
            }

            static void WriteOffset(DateTimeOffset value, TextWriter output)
            {
                output.Write("\"");
                output.Write(value.ToString("o"));
                output.Write("\"");
            }

            static void WriteDateTime(DateTime value, TextWriter output)
            {
                output.Write("\"");
                output.Write(value.ToString("o"));
                output.Write("\"");
            }

            static void WriteString(string value, TextWriter output)
            {
                var content = Escape(value);
                output.Write("\"");
                output.Write(content);
                output.Write("\"");
            }

            static string Escape(string s)
            {
                if (s == null) return null;

                StringBuilder escapedResult = null;
                var cleanSegmentStart = 0;
                for (var i = 0; i < s.Length; ++i)
                {
                    var c = s[i];
                    if (c < (char)32 || c == '\\' || c == '"')
                    {

                        if (escapedResult == null)
                            escapedResult = new StringBuilder();

                        escapedResult.Append(s.Substring(cleanSegmentStart, i - cleanSegmentStart));
                        cleanSegmentStart = i + 1;

                        switch (c)
                        {
                            case '"':
                                {
                                    escapedResult.Append("\\\"");
                                    break;
                                }
                            case '\\':
                                {
                                    escapedResult.Append("\\\\");
                                    break;
                                }
                            case '\n':
                                {
                                    escapedResult.Append("\\n");
                                    break;
                                }
                            case '\r':
                                {
                                    escapedResult.Append("\\r");
                                    break;
                                }
                            case '\f':
                                {
                                    escapedResult.Append("\\f");
                                    break;
                                }
                            case '\t':
                                {
                                    escapedResult.Append("\\t");
                                    break;
                                }
                            default:
                                {
                                    escapedResult.Append("\\u");
                                    escapedResult.Append(((int)c).ToString("X4"));
                                    break;
                                }
                        }
                    }
                }

                if (escapedResult != null)
                {
                    if (cleanSegmentStart != s.Length)
                        escapedResult.Append(s.Substring(cleanSegmentStart));

                    return escapedResult.ToString();
                }

                return s;
            }
        }
    }

    public interface IStructuredLogger : ILog
    {
        void Debug(object message, object structure);
        void Debug(object message, Exception exception, object structure);
        void Error(object message, object structure);
        void Error(object message, Exception exception, object structure);
        void Fatal(object message, object structure);
        void Fatal(object message, Exception exception, object structure);
        void Info(object message, object structure);
        void Info(object message, Exception exception, object structure);
        void Warn(object message, object structure);
        void Warn(object message, Exception exception, object structure);
    }
}