using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsSerializedMember
    {
        public static bool TryDeserialize(this SerializedMember? serializedMember, out object? result)
        {
            if (serializedMember == null)
            {
                result = null;
                return false;
            }
            var type = TypeUtils.GetType(serializedMember.typeName);
            if (type == null)
            {
                result = null;
                return false;
            }
            try
            {
                result = serializedMember.valueJsonElement.Deserialize(type);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
        public static bool TryDeserialize(this SerializedMember? serializedMember, Type targetType, out object? result)
        {
            if (serializedMember == null)
            {
                result = null;
                return false;
            }
            try
            {
                result = serializedMember.valueJsonElement.Deserialize(targetType);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        public static bool TryDeserialize<T>(this SerializedMember? serializedMember, out T? result)
        {
            if (serializedMember == null)
            {
                result = default;
                return false;
            }
            try
            {
                result = serializedMember.valueJsonElement.Deserialize<T>();
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }

        public static bool TryDeserialize(this SerializedMember? serializedMember, out object? result, out string? error)
        {
            if (serializedMember == null)
            {
                result = null;
                error = "SerializedMember is null.";
                return false;
            }
            var type = TypeUtils.GetType(serializedMember.typeName);
            if (type == null)
            {
                result = null;
                error = $"Type '{serializedMember.typeName}' not found for member '{serializedMember.name}'.";
                return false;
            }
            try
            {
                result = serializedMember.valueJsonElement.Deserialize(type);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to deserialize member '{serializedMember.name}' of type '{serializedMember.typeName}': {ex.Message}";
                result = null;
                return false;
            }
        }
        public static bool TryDeserialize(this SerializedMember? serializedMember, Type targetType, out object? result, out string? error)
        {
            if (serializedMember == null)
            {
                result = null;
                error = "SerializedMember is null.";
                return false;
            }
            try
            {
                result = serializedMember.valueJsonElement.Deserialize(targetType);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to deserialize member '{serializedMember.name}' of type '{serializedMember.typeName}': {ex.Message}";
                result = null;
                return false;
            }
        }

        public static bool TryDeserialize<T>(this SerializedMember? serializedMember, out T? result, out string? error)
        {
            if (serializedMember == null)
            {
                result = default;
                error = "SerializedMember is null.";
                return false;
            }
            try
            {
                result = serializedMember.valueJsonElement.Deserialize<T>();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to deserialize member '{serializedMember.name}' of type '{serializedMember.typeName}': {ex.Message}";
                result = default;
                return false;
            }
        }
        public static bool TryDeserializeEnumerable(this SerializedMember? serializedMember, Type type, out IEnumerable<object?>? result, StringBuilder? stringBuilder = null)
        {
            try
            {
                if (!serializedMember.TryDeserialize<SerializedMemberList>(out var parsedList, out var errorMessage))
                {
                    result = null;
                    if (stringBuilder != null)
                        stringBuilder.AppendLine(serializedMember == null
                            ? $"[Error] Failed to deserialize 'null': {errorMessage}"
                            : $"[Error] Failed to deserialize '{serializedMember.name}': {errorMessage}");
                    return false;
                }
                if (stringBuilder != null)
                    stringBuilder.AppendLine(parsedList == null
                        ? $"Deserializing '{serializedMember!.name}' enumerable with 'null' value."
                        : $"Deserializing '{serializedMember!.name}' enumerable with {parsedList.Count} items.");

                var success = true;
                var enumerable = parsedList
                    ?.Select((element, i) =>
                    {
                        if (!element.TryDeserialize(out var parsedValue, out var errorMessage))
                        {
                            success = false;
                            if (stringBuilder != null)
                                stringBuilder.AppendLine($"  [Error] Enumerable[{i}] deserialization failed: {errorMessage}");
                            return null;
                        }
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"  Enumerable[{i}] deserialized successfully.");
                        return parsedValue;
                    });

                if (!success)
                {
                    result = null;
                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"[Error] Failed to deserialize '{serializedMember!.name}': Some elements could not be deserialized.");
                    return false;
                }

                if (type.IsArray)
                {
                    result = enumerable?.ToArray();
                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"[Success] Deserialized '{serializedMember!.name}' as an array with {result.Count()} items.");
                }
                else // if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    result = enumerable?.ToList();
                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"[Success] Deserialized '{serializedMember!.name}' as a list with {result.Count()} items.");
                }

                return true;
            }
            catch (Exception ex)
            {
                result = null;
                if (stringBuilder != null)
                    stringBuilder.AppendLine($"[Error] Failed to deserialize '{serializedMember?.name}': {ex.Message}");
                return false;
            }
        }
    }
}