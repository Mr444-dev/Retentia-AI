// -----------------------------------------------------------------------------
// DebugLogger.cs
// Custom logging utility with editor toggle and log levels.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using System.Text;
using UnityEngine;

namespace Retentia.Core
{
    #region Log Level Enum

    /// <summary>
    /// Log severity levels for filtering output.
    /// </summary>
    public enum LogLevel
    {
        Verbose = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        None = 5
    }

    #endregion

    #region Debug Logger

    /// <summary>
    /// Custom debug logger with editor toggle, log levels, and formatted output.
    /// Provides centralized logging control for the entire Retentia system.
    /// </summary>
    public static class DebugLogger
    {
        #region Fields

        /// <summary>
        /// Whether logging is enabled globally.
        /// </summary>
        private static bool _isEnabled = true;

        /// <summary>
        /// Minimum log level to display.
        /// </summary>
        private static LogLevel _minimumLevel = LogLevel.Debug;

        /// <summary>
        /// Whether to include timestamps in log messages.
        /// </summary>
        private static bool _includeTimestamp = true;

        /// <summary>
        /// StringBuilder for efficient string concatenation.
        /// </summary>
        private static readonly StringBuilder _stringBuilder = new StringBuilder(256);

        /// <summary>
        /// Log buffer for recent messages.
        /// </summary>
        private static readonly System.Collections.Generic.Queue<string> _logBuffer = new System.Collections.Generic.Queue<string>(100);

        /// <summary>
        /// Maximum number of messages to keep in buffer.
        /// </summary>
        private const int MAX_BUFFER_SIZE = 100;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether logging is enabled.
        /// </summary>
        public static bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                if (_isEnabled)
                {
                    UnityEngine.Debug.Log("[Retentia] Logging enabled.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum log level.
        /// </summary>
        public static LogLevel MinimumLevel
        {
            get => _minimumLevel;
            set => _minimumLevel = value;
        }

        /// <summary>
        /// Gets or sets whether to include timestamps.
        /// </summary>
        public static bool IncludeTimestamp
        {
            get => _includeTimestamp;
            set => _includeTimestamp = value;
        }

        /// <summary>
        /// Gets recent log messages from buffer.
        /// </summary>
        public static string[] RecentLogs => _logBuffer.ToArray();

        #endregion

        #region Logging Methods

        /// <summary>
        /// Logs a message with the specified level.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="level">Log severity level.</param>
        /// <param name="context">Optional Unity context object.</param>
        public static void Log(string message, LogLevel level = LogLevel.Info, UnityEngine.Object context = null)
        {
            if (!_isEnabled || level < _minimumLevel)
            {
                return;
            }

            var formattedMessage = FormatMessage(message, level);
            AddToBuffer(formattedMessage);

            switch (level)
            {
                case LogLevel.Verbose:
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(formattedMessage, context);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage, context);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(formattedMessage, context);
                    break;
            }
        }

        /// <summary>
        /// Logs a verbose message.
        /// </summary>
        public static void Verbose(string message, UnityEngine.Object context = null)
        {
            Log(message, LogLevel.Verbose, context);
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        public static void Debug(string message, UnityEngine.Object context = null)
        {
            Log(message, LogLevel.Debug, context);
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        public static void Info(string message, UnityEngine.Object context = null)
        {
            Log(message, LogLevel.Info, context);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public static void Warning(string message, UnityEngine.Object context = null)
        {
            Log(message, LogLevel.Warning, context);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void Error(string message, UnityEngine.Object context = null)
        {
            Log(message, LogLevel.Error, context);
        }

        /// <summary>
        /// Logs an exception with stack trace.
        /// </summary>
        public static void Exception(Exception exception, string additionalInfo = null, UnityEngine.Object context = null)
        {
            if (!_isEnabled)
            {
                return;
            }

            var message = string.IsNullOrEmpty(additionalInfo)
                ? $"Exception: {exception.Message}\n{exception.StackTrace}"
                : $"{additionalInfo}\nException: {exception.Message}\n{exception.StackTrace}";

            Log(message, LogLevel.Error, context);
        }

        #endregion

        #region Service Lifecycle Logging

        /// <summary>
        /// Logs service initialization.
        /// </summary>
        public static void LogServiceInit(string serviceName)
        {
            Log($"[{serviceName}] Initializing...", LogLevel.Info);
        }

        /// <summary>
        /// Logs service ready state.
        /// </summary>
        public static void LogServiceReady(string serviceName)
        {
            Log($"[{serviceName}] Ready.", LogLevel.Info);
        }

        /// <summary>
        /// Logs service disposal.
        /// </summary>
        public static void LogServiceDispose(string serviceName)
        {
            Log($"[{serviceName}] Disposing...", LogLevel.Debug);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Formats a log message with timestamp and level prefix.
        /// </summary>
        private static string FormatMessage(string message, LogLevel level)
        {
            _stringBuilder.Clear();

            if (_includeTimestamp)
            {
                _stringBuilder.Append("[");
                _stringBuilder.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
                _stringBuilder.Append("] ");
            }

            _stringBuilder.Append("[");
            _stringBuilder.Append(GetLevelPrefix(level));
            _stringBuilder.Append("] ");
            _stringBuilder.Append(message);

            return _stringBuilder.ToString();
        }

        /// <summary>
        /// Gets the prefix string for a log level.
        /// </summary>
        private static string GetLevelPrefix(LogLevel level)
        {
            return level switch
            {
                LogLevel.Verbose => "VRB",
                LogLevel.Debug => "DBG",
                LogLevel.Info => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                _ => "???"
            };
        }

        /// <summary>
        /// Adds a message to the log buffer.
        /// </summary>
        private static void AddToBuffer(string message)
        {
            if (_logBuffer.Count >= MAX_BUFFER_SIZE)
            {
                _logBuffer.Dequeue();
            }
            _logBuffer.Enqueue(message);
        }

        #endregion

        #region Buffer Operations

        /// <summary>
        /// Clears the log buffer.
        /// </summary>
        public static void ClearBuffer()
        {
            _logBuffer.Clear();
        }

        /// <summary>
        /// Gets all buffered logs as a single string.
        /// </summary>
        public static string GetBufferedLogsAsString()
        {
            return string.Join("\n", _logBuffer);
        }

        #endregion
    }

    #endregion
}
