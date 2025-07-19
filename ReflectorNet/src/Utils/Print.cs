using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static class Print
    {
        public static void FailedToSetNewValue(ref object? obj, Type type, int depth = 0, StringBuilder? stringBuilder = null)
        {
            if (stringBuilder == null)
                return;

            stringBuilder.AppendLine($"{StringUtils.GetPadding(depth)}[Error] Failed to set new value for '{type.FullName}'.");
        }
        public static void SetNewValue<T>(ref object? obj, ref T? newValue, Type type, int depth = 0, StringBuilder? stringBuilder = null)
        {
            if (stringBuilder == null)
                return;

            var originalType = obj?.GetType() ?? type;
            var newType = newValue?.GetType() ?? type;

            stringBuilder.AppendLine($@"{StringUtils.GetPadding(depth)}[Success] Set value
{StringUtils.GetPadding(depth + 1)}was: type='{originalType.FullName ?? "null"}', value='{obj}'
{StringUtils.GetPadding(depth + 1)}new: type='{newType.FullName ?? "null"}', value='{newValue}'.");
        }
        public static void SetNewValueEnumerable(ref object? obj, ref IEnumerable? newValue, Type type, int depth = 0, StringBuilder? stringBuilder = null)
        {
            if (stringBuilder == null)
                return;

            var originalType = obj?.GetType() ?? type;
            var newType = newValue?.GetType() ?? type;

            stringBuilder.AppendLine($@"{StringUtils.GetPadding(depth)}[Success] Set array value
{StringUtils.GetPadding(depth + 1)}was: type='{originalType.FullName ?? "null"}', value='{obj}'
{StringUtils.GetPadding(depth + 1)}new: type='{newType.FullName ?? "null"}', value='{newValue}'.");
        }
        public static void SetNewValueEnumerable<T>(ref object? obj, ref IEnumerable<T>? newValue, Type type, int depth = 0, StringBuilder? stringBuilder = null)
        {
            if (stringBuilder == null)
                return;

            var originalType = obj?.GetType() ?? type;
            var newType = newValue?.GetType() ?? type;

            stringBuilder.AppendLine($@"{StringUtils.GetPadding(depth)}[Success] Set array value
{StringUtils.GetPadding(depth + 1)}was: type='{originalType.FullName ?? "null"}', value='{obj}'
{StringUtils.GetPadding(depth + 1)}new: type='{newType.FullName ?? "null"}', value='{newValue}'.");
        }
    }
}