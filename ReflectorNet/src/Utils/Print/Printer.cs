using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public class Printer : IDisposable
    {
        readonly LinkedList<LazyLine> _lazyLines = new();
        readonly ILogger? _aiLogger;
        readonly ILogger? _systemLogger;

        int lastDepth = -1;

        public Printer(ILogger? logger)
            : this(new StringBuilderLogger(), logger)
        { }

        public Printer(ILogger? aiLogger = null, ILogger? systemLogger = null)
        {
            _aiLogger = aiLogger;
            _systemLogger = systemLogger;
        }

        public void TraceLog(Func<string> lazyLine, int depth = 0)
            => LogLine(LogLevel.Trace, lazyLine, depth);

        public void DebugLog(Func<string> lazyLine, int depth = 0)
            => LogLine(LogLevel.Debug, lazyLine, depth);

        public void InfoLog(Func<string> lazyLine, int depth = 0)
            => LogLine(LogLevel.Information, lazyLine, depth);

        public void WarningLog(Func<string> lazyLine, int depth = 0)
            => LogLine(LogLevel.Warning, lazyLine, depth);

        public void ErrorLog(Func<string> lazyLine, int depth = 0)
            => LogLine(LogLevel.Error, lazyLine, depth);

        public void CriticalLog(Func<string> lazyLine, int depth = 0)
            => LogLine(LogLevel.Critical, lazyLine, depth);

        public void LogLine(LogLevel level, Func<string> lazyLine, int depth = 0)
        {
            var isEnabled = _aiLogger?.IsEnabled(level) == false ||
                            _systemLogger?.IsEnabled(level) == false;

            // Remove lazy lines that are deeper than the current depth
            while (_lazyLines.Count > 0 && _lazyLines.Last!.Value.Depth > depth)
                _lazyLines.RemoveLast();

            if (isEnabled)
            {
                // Print lazy lines that are less deep than the current depth
                while (_lazyLines.Count > 0 && _lazyLines.First!.Value.Depth < depth)
                    PrintAndRemove(_lazyLines.First, level);

                // Remove lazy lines at the same depth as the current one
                while (_lazyLines.Count > 0 && _lazyLines.First!.Value.Depth == depth)
                    _lazyLines.RemoveFirst();

                if (_lazyLines.Count == 0)
                    lastDepth = -1;

                Print(lazyLine, depth, level);
            }
            else
            {
                _lazyLines.AddLast(new LazyLine(lazyLine, depth));
            }

            lastDepth = _lazyLines.Count == 0 ? -1 : depth;
        }

        public void Dispose()
        {
            _lazyLines.Clear();
        }

        void PrintAndRemove(LinkedListNode<LazyLine> node, LogLevel level)
        {
            Print(
                lazyLine: node.Value.Line,
                depth: node.Value.Depth,
                level: level);

            _lazyLines.Remove(node);
        }

        void Print(Func<string> lazyLine, int depth, LogLevel level)
        {
            var padding = StringUtils.GetPadding(depth);
            var line = $"{padding}{lazyLine()}";

            _aiLogger?.Log(level, line);
            _systemLogger?.Log(level, line);
        }

        private class LazyLine
        {
            public Func<string> Line { get; }
            public int Depth { get; }

            public LazyLine(Func<string> line, int depth)
            {
                Line = line;
                Depth = depth;
            }
        }
    }
}