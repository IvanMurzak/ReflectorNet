using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace ConsoleApp
{
    public class CustomConsoleFormatter : ConsoleFormatter
    {
        private readonly IDisposable? _optionsReloadToken;
        private CustomConsoleFormatterOptions _formatterOptions;

        public CustomConsoleFormatter(IOptionsMonitor<CustomConsoleFormatterOptions> options)
            : base(nameof(CustomConsoleFormatter))
        {
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
            _formatterOptions = options.CurrentValue;
        }

        private void ReloadLoggerOptions(CustomConsoleFormatterOptions options) => _formatterOptions = options;

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
            var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            // Apply color based on log level
            var colors = GetLogLevelConsoleColors(logEntry.LogLevel);
            var logLevelString = GetLogLevelString(logEntry.LogLevel);

            // Save original colors to restore later
            var originalForeground = Console.ForegroundColor;
            var originalBackground = Console.BackgroundColor;

            // Set color for log level
            if (colors.Foreground.HasValue)
                Console.ForegroundColor = colors.Foreground.Value;
            if (colors.Background.HasValue)
                Console.BackgroundColor = colors.Background.Value;

            // Write the log level with colors
            Console.Write(logLevelString);
            Console.Write(' ');

            // Reset colors for the message
            Console.ResetColor();

            // Write the message
            Console.WriteLine(message);

            // Handle exception if present
            if (logEntry.Exception != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(logEntry.Exception.ToString());
                Console.ResetColor();
            }
        }

        private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
        {
            if (_formatterOptions.ColorBehavior == LoggerColorBehavior.Disabled)
                return new ConsoleColors(null, null);

            return logLevel switch
            {
                LogLevel.Trace => new ConsoleColors(ConsoleColor.Gray, null),
                LogLevel.Debug => new ConsoleColors(ConsoleColor.Cyan, null),
                LogLevel.Information => new ConsoleColors(ConsoleColor.DarkGreen, null),
                LogLevel.Warning => new ConsoleColors(ConsoleColor.Yellow, null),
                LogLevel.Error => new ConsoleColors(ConsoleColor.Red, null),
                LogLevel.Critical => new ConsoleColors(ConsoleColor.DarkRed, null),
                _ => new ConsoleColors(null, null)
            };
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "trce",
                LogLevel.Debug => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "fail",
                LogLevel.Critical => "crit",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }

        private readonly struct ConsoleColors
        {
            public ConsoleColors(ConsoleColor? foreground, ConsoleColor? background)
            {
                Foreground = foreground;
                Background = background;
            }

            public ConsoleColor? Foreground { get; }
            public ConsoleColor? Background { get; }
        }
    }

    public class CustomConsoleFormatterOptions : ConsoleFormatterOptions
    {
        public LoggerColorBehavior ColorBehavior { get; set; } = LoggerColorBehavior.Enabled;
    }
}