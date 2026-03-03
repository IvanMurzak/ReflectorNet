using System;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class Reflector
    {
        /// <summary>
        /// Modifies an existing object with data from a SerializedMember using the registered modifier chain.
        /// This method provides in-place deserialization where values from serialized data are applied to
        /// an already instantiated object, with comprehensive error handling and validation.
        ///
        /// Behavior:
        /// - Type resolution: Uses provided dataType parameter, or resolves from data.typeName if not specified
        /// - Validation: Performs extensive validation including null checks, type compatibility, and casting verification
        /// - Error handling: Returns detailed error messages with proper indentation based on depth level
        /// - Modifier chain: Delegates to registered modifiers that handle the actual data transfer logic
        /// - Hierarchical support: Supports nested object modification with depth tracking for proper error formatting
        /// - Non-destructive: Only modifies the object's properties/fields, doesn't replace the object instance
        /// - Type safety: Ensures the target object is compatible with the expected type before modification
        ///
        /// The method uses a StringBuilder to accumulate any errors or messages encountered during modification,
        /// making it suitable for batch operations where you need to track multiple potential issues.
        /// Each error message is properly indented based on the depth parameter for hierarchical error reporting.
        /// </summary>
        /// <param name="obj">The existing object to modify with data. Must not be null and must be compatible with the expected type.</param>
        /// <param name="data">The SerializedMember containing the data to modify the object with.</param>
        /// <param name="fallbackObjType">Optional explicit type for validation. If null, type is resolved from data.typeName.</param>
        /// <param name="depth">The current depth level in the object hierarchy, used for error message indentation. Default is 0.</param>
        /// <param name="stringBuilder">Optional StringBuilder to accumulate error messages and status information. A new one is created if not provided.</param>
        /// <param name="flags">BindingFlags controlling which fields and properties are modified. Default includes public and non-public instance members.</param>
        /// <param name="logger">Optional logger for tracing modification operations and debugging.</param>
        /// <returns>True if modification was successful, false if any errors occurred. If false, stringBuilder contains error messages.</returns>
        public bool TryModify(
            ref object? obj,
            SerializedMember data,
            Type? fallbackObjType = null,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (obj == null && data.IsNull())
            {
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}Object modification skipped: both target object and data are null.");

                return true;
            }

            var objType = TypeUtils.GetTypeWithNamePriority(data, fallbackObjType, out var typeError) ?? obj?.GetType();
            if (objType == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Object modification failed: {typeError}");

                logs?.Error($"Object modification failed: {typeError}", depth);

                return false;
            }

            var converter = Converters.GetConverter(objType);
            if (converter == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}No suitable converter found for type '{objType.GetTypeId().ValueOrNull()}'");

                logs?.Error($"No suitable converter found for type '{objType.GetTypeId().ValueOrNull()}'", depth);

                return false;
            }

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{padding}Modify. {converter.GetType().GetTypeShortName()} used for type='{objType.GetTypeShortName().ValueOrNull()}', name='{data.name.ValueOrNull()}'");

            var success = converter.TryModify(
                this,
                ref obj,
                data: data,
                type: objType,
                depth: depth,
                logs: logs,
                flags: flags,
                logger: logger);

            return success;
        }
    }
}
