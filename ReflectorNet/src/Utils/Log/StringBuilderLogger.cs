using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    /*
     * ReflectorNet
     * Author: Ivan Murzak (https://github.com/IvanMurzak)
     * Copyright (c) 2025 Ivan Murzak
     * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
     */

    public class StringBuilderLogger : ILogger
    {
        readonly string? _categoryName;
        readonly StringBuilder _stringBuilder;

        public StringBuilderLogger(string? categoryName = null)
            : this(new StringBuilder(), categoryName)
        { }

        public StringBuilderLogger(StringBuilder stringBuilder, string? categoryName = null)
        {
            _stringBuilder = stringBuilder;
            _categoryName = categoryName?.Contains(".") == true
                ? categoryName?.Substring(categoryName.LastIndexOf('.') + 1)
                : categoryName;
        }

        IDisposable ILogger.BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));
            if (state == null) throw new ArgumentNullException(nameof(state));

            var logLevelLabel = logLevel switch
            {
                LogLevel.Trace => Consts.Log.Trce,
                LogLevel.Debug => Consts.Log.Dbug,
                LogLevel.Information => Consts.Log.Info,
                LogLevel.Warning => Consts.Log.Warn,
                LogLevel.Error => Consts.Log.Fail,
                LogLevel.Critical => Consts.Log.Crit,
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
            };

            var message = string.IsNullOrEmpty(_categoryName)
                ? $"{Consts.Log.Tag} {Consts.Log.Color.LevelStart}{logLevelLabel}{Consts.Log.Color.LevelEnd} {formatter(state, exception)}"
                : $"{Consts.Log.Tag} {Consts.Log.Color.CategoryStart}{_categoryName}{Consts.Log.Color.CategoryEnd} {Consts.Log.Color.LevelStart}{logLevelLabel}{Consts.Log.Color.LevelEnd} {formatter(state, exception)}";

            _stringBuilder.AppendLine(message);
        }
        public void Clear() => _stringBuilder.Clear();
        public override string ToString() => _stringBuilder.ToString();
    }
}