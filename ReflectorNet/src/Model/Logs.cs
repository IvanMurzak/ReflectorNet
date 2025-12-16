/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System.Collections.Generic;

namespace com.IvanMurzak.ReflectorNet.Model
{
    public class Logs : LinkedList<LogEntry>
    {
        const int DepthPadding = 2;

        public override string ToString()
        {
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var log in this)
                stringBuilder.AppendLine($"{new string(' ', log.Depth * DepthPadding)}[{log.Type}] {log.Message}");

            return stringBuilder.ToString();
        }
    }
    public class LogEntry
    {
        public int Depth { get; set; } = 0;
        public string Message { get; set; } = string.Empty;
        public LogType Type { get; set; } = LogType.Info;
    }
    public enum LogType
    {
        Trce, Dbug, Info, Warn, Errr, Crit
    }
    public static class LogTypeExtensions
    {
        public static Logs Trace(this Logs logs, string message, int depth = 0)
        {
            logs.AddLast(new LogEntry() { Message = message, Type = LogType.Trce, Depth = depth });
            return logs;
        }
        public static Logs Debug(this Logs logs, string message, int depth = 0)
        {
            logs.AddLast(new LogEntry() { Message = message, Type = LogType.Dbug, Depth = depth });
            return logs;
        }
        public static Logs Info(this Logs logs, string message, int depth = 0)
        {
            logs.AddLast(new LogEntry() { Message = message, Type = LogType.Info, Depth = depth });
            return logs;
        }
        public static Logs Warn(this Logs logs, string message, int depth = 0)
        {
            logs.AddLast(new LogEntry() { Message = message, Type = LogType.Warn, Depth = depth });
            return logs;
        }
        public static Logs Error(this Logs logs, string message, int depth = 0)
        {
            logs.AddLast(new LogEntry() { Message = message, Type = LogType.Errr, Depth = depth });
            return logs;
        }
        public static Logs Critical(this Logs logs, string message, int depth = 0)
        {
            logs.AddLast(new LogEntry() { Message = message, Type = LogType.Crit, Depth = depth });
            return logs;
        }
    }
}