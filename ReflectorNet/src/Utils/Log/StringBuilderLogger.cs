using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public class StringBuilderLogger : ILogger
    {
        readonly string _categoryName;
        readonly StringBuilder _stringBuilder;

        public StringBuilderLogger(string categoryName) : this(categoryName, new StringBuilder()) { }
        public StringBuilderLogger(string categoryName, StringBuilder stringBuilder)
        {
            _categoryName = categoryName.Contains(".")
                ? categoryName.Substring(categoryName.LastIndexOf('.') + 1)
                : categoryName;
            _stringBuilder = stringBuilder;
        }

        IDisposable ILogger.BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));
            if (state == null) throw new ArgumentNullException(nameof(state));

            var message = $"{Consts.Log.Tag} {Consts.Log.Color.CategoryStart}{_categoryName}{Consts.Log.Color.CategoryEnd} {Consts.Log.Color.LevelStart}[{logLevel}]{Consts.Log.Color.LevelEnd} {formatter(state, exception)}";
            _stringBuilder.AppendLine(message);
        }
        public void Clear() => _stringBuilder.Clear();
        public override string ToString() => _stringBuilder.ToString();
    }
}