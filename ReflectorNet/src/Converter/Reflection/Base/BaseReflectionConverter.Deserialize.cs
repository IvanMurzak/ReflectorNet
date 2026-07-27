/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    public abstract partial class BaseReflectionConverter<T> : IReflectionConverter
    {
        /// <summary>
        /// Performs comprehensive deserialization of SerializedMember data into strongly-typed objects.
        /// This method serves as the main entry point for converting serialized representations back into
        /// live .NET objects with full type preservation and validation.
        ///
        /// Deserialization Process:
        /// 1. Value Deserialization: Attempts to deserialize the core value using TryDeserializeValue
        /// 2. Field Modification: Iterates through serialized fields and applies them to the target object
        /// 3. Property Modification: Iterates through serialized properties and applies them to the target object
        /// 4. Type Validation: Ensures field/property types are compatible with target object
        /// 5. Instance Creation: Creates object instances as needed during the deserialization process
        /// 6. Error Handling: Provides comprehensive error reporting with hierarchical formatting
        ///
        /// Field and Property Handling:
        /// - Uses reflection to locate corresponding fields/properties on the target type
        /// - Supports both public and non-public members based on BindingFlags
        /// - Validates writability for properties before attempting to set values
        /// - Provides detailed warnings for missing or incompatible members
        /// - Recursive deserialization for complex nested objects
        ///
        /// Error Recovery:
        /// - Continues processing remaining members even if individual members fail
        /// - Provides detailed error messages with proper indentation for nested structures
        /// - Logs warnings for non-critical issues while preserving overall deserialization
        /// </summary>
        /// <param name="reflector">The Reflector instance used for recursive deserialization operations.</param>
        /// <param name="data">SerializedMember containing the data to deserialize.</param>
        /// <param name="fallbackType">Optional type to use when type information is missing from data.</param>
        /// <param name="fallbackName">Optional name to use for logging when name is missing from data.</param>
        /// <param name="depth">Current depth in the object hierarchy for proper error message indentation.</param>
        /// <param name="stringBuilder">Optional StringBuilder for accumulating detailed operation logs.</param>
        /// <param name="logger">Optional logger for tracing deserialization operations.</param>
        /// <returns>The deserialized object instance.</returns>
        /// <exception cref="DeserializationException">
        /// The <c>value</c> payload could not be deserialized into the target type. This method has no
        /// success channel other than its return value, so a failure is raised rather than silently
        /// substituted with the target type's default value.
        /// <para>
        /// This is also where the converter chain ENDS: if the value payload was
        /// <see cref="DeserializationOutcome.NotApplicable"/> - no converter in the chain understood
        /// its shape - the terminal failure is raised here, once. Individual links stay quiet
        /// (see <see cref="DeserializationOutcome"/>).
        /// </para>
        /// </exception>
        public virtual object? Deserialize(
            Reflector reflector,
            SerializedMember data,
            Type? fallbackType = null,
            string? fallbackName = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            DeserializationContext? context = null)
        {
            if (reflector == null) throw new ArgumentNullException(nameof(reflector));
            if (data == null) throw new ArgumentNullException(nameof(data));

            // A value payload that IS this converter's shape but is broken throws
            // DeserializationException from TryDeserializeValueInternal and propagates out of here.
            // A payload that is not this converter's shape at all comes back as `outcome ==
            // NotApplicable` and is dealt with below. A `false` return is reserved for the cases
            // where there is nothing to deserialize at all (null data / unresolvable type), which are
            // already reported through `logs` and never yield a fabricated default value.
            if (!TryDeserializeValue(
                reflector,
                data: data,
                result: out var result,
                type: out var type,
                outcome: out var outcome,
                fallbackType: fallbackType,
                depth: depth,
                logs: logs,
                logger: logger))
            {
                return result;
            }

            var padding = StringUtils.GetPadding(depth);

            // ---- Chain end -------------------------------------------------------------------
            // Nobody understood the payload's shape: not this converter (it declined by shape) and
            // not any derived converter layered on top of it (it produced no value). THIS is the one
            // place that is allowed to be loud about it. A derived converter that DOES resolve such a
            // payload - the Unity-MCP object-reference pattern - either produces a value here or
            // overrides Deserialize entirely, and never reaches this branch.
            if (outcome == DeserializationOutcome.NotApplicable)
            {
                var shape = SerializedMemberShape.Classify(data.valueJsonElement);
                // `ValueOrNull()` renders null as the literal "null" and never returns null, so the
                // fallback must be chosen BEFORE it is applied - otherwise the message reads
                // "member 'null'" while the exception below carries the real fallback name.
                var message = $"Failed to deserialize member '{(data.name ?? fallbackName).ValueOrNull()}'"
                    + $" of type '{type!.GetTypeId()}':\n"
                    + $"No converter understood the '{SerializedMember.ValueName}' payload shape."
                    + (shape.UnknownKeys.Count > 0 ? $" {shape.DescribeUnknownKeys()}" : string.Empty);

                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{Consts.Emoji.Fail} {message}");

                logs?.Error(message, depth);

                // Record it too: a caller threading a report must not see NothingRecorded (or, worse,
                // an "Applied" derived from an empty list) for the headline failure of the operation.
                logs.RecordMember(data.name ?? fallbackName, MemberOutcome.ResolutionFailed, message, depth);

                throw new DeserializationException(message, type, data.name ?? fallbackName);
            }

            // Register the object early (before deserializing children) so child references can resolve
            if (result != null && context != null)
                context.Register(result);

            // ---- Two-phase member application -------------------------------------------------
            // RESOLVE every member first, mutating NOTHING; only then APPLY. A member that fails to
            // resolve rejects the whole set, so the target is never left half-written by a payload
            // that was doomed from the start. See MemberApplicationReport for why there is no
            // rollback and why "partially applied" is a real terminal state.
            var resolved = default(List<ResolvedMember>);

            if (data.fields != null)
            {
                if (data.fields.Count > 0)
                    result ??= CreateInstance(reflector, type!);

                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Field} Deserialize '{nameof(SerializedMember.fields)}' type='{type?.GetTypeId().ValueOrNull()}' name='{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'.");

                foreach (var field in data.fields)
                {
                    if (string.IsNullOrEmpty(field.name))
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{padding}{Consts.Emoji.Warn} Field name is null or empty in serialized data: '{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'. Skipping.");

                        logs?.Warning($"Field name is null or empty in serialized data: '{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'. Skipping.", depth);
                        logs.RecordMember(field.name, MemberOutcome.Skipped, "Member name is null or empty.", depth);

                        continue;
                    }

                    var fieldInfo = type!.GetField(field.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldInfo == null)
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{padding}{Consts.Emoji.Warn} Field '{field.name}' not found on type '{type.GetTypeId()}'.");

                        logs?.Warning($"Field '{field.name}' not found on type '{type.GetTypeId()}'.", depth);
                        logs.RecordMember(field.name, MemberOutcome.Skipped, $"Field not found on type '{type.GetTypeId()}'.", depth);

                        continue;
                    }

                    object? fieldValue;
                    try
                    {
                        fieldValue = reflector.Deserialize(
                            data: field,
                            fallbackType: fieldInfo.FieldType,
                            depth: depth + 1,
                            logs: logs,
                            logger: logger,
                            context: context);
                    }
                    catch (Exception ex)
                    {
                        // RESOLVE failed -> reject the whole member set. Nothing has been written yet,
                        // so the target is left exactly as it was.
                        logs.RecordMember(field.name, MemberOutcome.ResolutionFailed, ex.Message, depth);
                        throw;
                    }

                    (resolved ??= new List<ResolvedMember>()).Add(ResolvedMember.ForField(fieldInfo, field.name, fieldValue));
                }
            }
            if (data.props != null)
            {
                if (data.props.Count > 0)
                    result ??= CreateInstance(reflector, type!);

                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Property} Deserialize '{nameof(SerializedMember.props)}' type='{type?.GetTypeId().ValueOrNull()}' name='{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'.");

                foreach (var property in data.props)
                {
                    if (string.IsNullOrEmpty(property.name))
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{padding}{Consts.Emoji.Warn} Property name is null or empty in serialized data: '{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'. Skipping.");

                        logs?.Warning($"Property name is null or empty in serialized data: '{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'. Skipping.", depth);
                        logs.RecordMember(property.name, MemberOutcome.Skipped, "Member name is null or empty.", depth);

                        continue;
                    }

                    var propertyInfo = type!.GetProperty(property.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (propertyInfo == null)
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{padding}{Consts.Emoji.Warn} Property '{property.name}' not found on type '{type.GetTypeId()}'.");

                        logs?.Warning($"Property '{property.name}' not found on type '{type.GetTypeId()}'.", depth);
                        logs.RecordMember(property.name, MemberOutcome.Skipped, $"Property not found on type '{type.GetTypeId()}'.", depth);

                        continue;
                    }
                    if (!propertyInfo.CanWrite)
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{padding}{Consts.Emoji.Warn} Property '{property.name}' on type '{type.GetTypeId()}' is read-only and cannot be set.");

                        logs?.Warning($"Property '{property.name}' on type '{type.GetTypeId()}' is read-only and cannot be set.", depth);
                        logs.RecordMember(property.name, MemberOutcome.Skipped, "Property is read-only.", depth);

                        continue;
                    }

                    object? propertyValue;
                    try
                    {
                        propertyValue = reflector.Deserialize(
                            property,
                            fallbackType: propertyInfo.PropertyType,
                            depth: depth + 1,
                            logs: logs,
                            logger: logger,
                            context: context);
                    }
                    catch (Exception ex)
                    {
                        logs.RecordMember(property.name, MemberOutcome.ResolutionFailed, ex.Message, depth);
                        throw;
                    }

                    (resolved ??= new List<ResolvedMember>()).Add(ResolvedMember.ForProperty(propertyInfo, property.name, propertyValue));
                }
            }

            // ---- APPLY -------------------------------------------------------------------------
            // Every member resolved. Writes may still fail (a setter can throw) - that is the
            // PartiallyApplied terminal state, and the report names exactly what landed. Rolling the
            // earlier writes back is deliberately NOT attempted: a setter with side effects makes
            // "write the old value back" a new mutation, not an undo.
            if (resolved != null)
            {
                foreach (var member in resolved)
                {
                    try
                    {
                        member.Apply(result);
                        logs.RecordMember(member.Name, MemberOutcome.Applied, depth: depth);
                    }
                    catch (Exception ex)
                    {
                        logs.RecordMember(member.Name, MemberOutcome.ApplyFailed, ex.Message, depth);

                        if (logger?.IsEnabled(LogLevel.Error) == true)
                            logger.LogError($"{padding}{Consts.Emoji.Fail} Failed to apply member '{member.Name}' on type '{type!.GetTypeId()}': {ex.Message}");

                        logs?.Error($"Failed to apply member '{member.Name}' on type '{type!.GetTypeId()}': {ex.Message}", depth);
                        throw;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// A member whose value has been resolved but not yet written to the target. Produced by the
        /// RESOLVE phase of <see cref="Deserialize"/> and consumed by its APPLY phase.
        /// </summary>
        readonly struct ResolvedMember
        {
            readonly FieldInfo? _fieldInfo;
            readonly PropertyInfo? _propertyInfo;
            readonly object? _value;

            public string Name { get; }

            ResolvedMember(FieldInfo? fieldInfo, PropertyInfo? propertyInfo, string name, object? value)
            {
                _fieldInfo = fieldInfo;
                _propertyInfo = propertyInfo;
                _value = value;
                Name = name;
            }

            public static ResolvedMember ForField(FieldInfo fieldInfo, string name, object? value)
                => new ResolvedMember(fieldInfo, null, name, value);

            public static ResolvedMember ForProperty(PropertyInfo propertyInfo, string name, object? value)
                => new ResolvedMember(null, propertyInfo, name, value);

            public void Apply(object? target)
            {
                if (_fieldInfo != null)
                    _fieldInfo.SetValue(target, _value);
                else
                    _propertyInfo!.SetValue(target, _value);
            }
        }

        /// <summary>
        /// Attempts to deserialize the value portion of a SerializedMember with comprehensive type resolution and validation.
        /// This method orchestrates the value deserialization process including type resolution, converter selection,
        /// and delegation to appropriate internal deserialization methods.
        ///
        /// Type Resolution Process:
        /// 1. Prioritizes type information from SerializedMember.typeName
        /// 2. Falls back to provided fallbackType parameter
        /// 3. Validates type compatibility and existence
        /// 4. Handles nullable type unwrapping automatically
        ///
        /// Deserialization Strategies:
        /// - Cascade mode: Attempts to deserialize as SerializedMember structure for complex objects
        /// - Direct mode: Deserializes directly from JSON for primitive and simple types
        /// - Error recovery: Provides meaningful error messages and fallback values
        /// - Logging integration: Comprehensive trace logging for debugging and monitoring
        ///
        /// Success/Failure Handling:
        /// - Returns true for successful deserialization with valid result
        /// - Returns false for failed deserialization with appropriate error logging
        /// - Provides detailed error information in StringBuilder for analysis
        /// - Maintains type safety throughout the deserialization process
        /// </summary>
        /// <param name="reflector">The Reflector instance used for type resolution and recursive operations.</param>
        /// <param name="data">The SerializedMember containing the data to deserialize.</param>
        /// <param name="result">Output parameter containing the deserialized object on success.</param>
        /// <param name="type">Output parameter containing the resolved target type.</param>
        /// <param name="fallbackType">Optional fallback type when type resolution from data fails.</param>
        /// <param name="depth">Current depth in object hierarchy for proper error message indentation.</param>
        /// <param name="stringBuilder">Optional StringBuilder for accumulating detailed operation logs.</param>
        /// <param name="logger">Optional logger for tracing deserialization operations.</param>
        /// <returns>True if deserialization succeeded, false otherwise.</returns>
        /// <exception cref="DeserializationException">
        /// The value payload could not be deserialized. Callers that must not throw should use
        /// <see cref="TryDeserializeValueReporting"/>, which converts this into <c>false</c> plus a
        /// <see cref="LogType.Error"/> entry.
        /// </exception>
        protected virtual bool TryDeserializeValue(
            Reflector reflector,
            SerializedMember? data,
            out object? result,
            out Type? type,
            Type? fallbackType = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            if (reflector == null) throw new ArgumentNullException(nameof(reflector));

            if (data == null)
            {
                result = null;
                type = null;
                return false;
            }

            var padding = StringUtils.GetPadding(depth);

            // Get the most appropriate type for deserialization
            type = TypeUtils.GetTypeWithNamePriority(data, fallbackType, out var error);
            if (type == null)
            {
                result = null;
                logs?.Error(error ?? "Unknown error", depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{error}");
                return false;
            }

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{padding}{Consts.Emoji.Start} Deserialize 'value', type='{type.GetTypeId()}' name='{data.name.ValueOrNull()}'.");

            var success = TryDeserializeValueInternal(
                reflector,
                data: data,
                result: out result,
                type: type,
                depth: depth,
                logs: logs,
                logger: logger);

            if (success)
            {
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Done} Deserialized '{type.GetTypeId()}'.");
            }
            else
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{Consts.Emoji.Fail} Deserialization '{type.GetTypeId()}' failed. Converter: {GetType().GetTypeShortName()}");
            }

            return success;
        }

        /// <summary>
        /// <inheritdoc cref="TryDeserializeValue(Reflector, SerializedMember, out object, out Type, Type, int, Logs, ILogger)"/>
        /// <para>
        /// Wrapper that also reports the tri-state <paramref name="outcome"/>. It is what makes
        /// "not my shape, carry on" distinguishable from "my shape, but broken" at the call site.
        /// </para>
        /// </summary>
        /// <param name="outcome">
        /// <see cref="DeserializationOutcome.Handled"/> when a value was produced;
        /// <see cref="DeserializationOutcome.NotApplicable"/> when the payload's shape is foreign to
        /// the <see cref="SerializedMember"/> schema AND no converter in the chain produced a value
        /// for it - the caller at the END of the chain turns that into a single explicit failure;
        /// <see cref="DeserializationOutcome.Failed"/> for the already-reported hard failures
        /// (null data / unresolvable type, or an explicit <c>false</c> from the seam).
        /// </param>
        /// <remarks>
        /// <para>
        /// ⚠ Deliberately NOT <c>virtual</c>, and deliberately a thin wrapper that DELEGATES to the
        /// virtual 8-argument overload rather than duplicating its body. The 8-argument overload is
        /// the single overridable seam; if this wrapper carried the implementation, an external
        /// subclass overriding the 8-argument overload would still compile, still carry
        /// <c>override</c>, and simply never be called - a silent behaviour break with no diagnostic.
        /// </para>
        /// <para>
        /// The <c>NotApplicable</c> verdict is conservative: the base converter never fabricates a
        /// default for a foreign-shaped payload, so <c>result == null</c> after a shape-decline means
        /// "nothing in the chain produced anything". A derived converter that resolves the foreign
        /// shape (the Unity-MCP object-reference pattern) sets a real value and is therefore reported
        /// as <see cref="DeserializationOutcome.Handled"/>. A derived converter that must resolve a
        /// foreign shape to a legitimate <c>null</c> overrides
        /// <see cref="DeclinesValueByShape"/> to return <c>false</c> - see its remarks.
        /// </para>
        /// </remarks>
        protected bool TryDeserializeValue(
            Reflector reflector,
            SerializedMember? data,
            out object? result,
            out Type? type,
            out DeserializationOutcome outcome,
            Type? fallbackType = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            // Computed from `data` alone, so it is equivalent before or after the seam runs.
            var declinedByShape = data != null && DeclinesValueByShape(data);

            var success = TryDeserializeValue(
                reflector,
                data: data,
                result: out result,
                type: out type,
                fallbackType: fallbackType,
                depth: depth,
                logs: logs,
                logger: logger);

            outcome = !success
                ? DeserializationOutcome.Failed
                : declinedByShape && result == null
                    ? DeserializationOutcome.NotApplicable
                    : DeserializationOutcome.Handled;

            return success;
        }

        /// <summary>
        /// <c>true</c> when this converter reads the <c>value</c> payload as a nested
        /// <see cref="SerializedMember"/> and the payload is not that shape at all - a JSON object
        /// carrying unrecognised keys and no STRUCTURAL <see cref="SerializedMember"/> key, e.g. a
        /// consumer object reference <c>{"instanceID":"12345"}</c> or
        /// <c>{"instanceID":"7","typeName":"UnityEngine.Rigidbody"}</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the "not mine" half of the tri-state and is NOT an error: see
        /// <see cref="DeserializationOutcome.NotApplicable"/>. A payload that mixes a STRUCTURAL key
        /// (<c>value</c> / <c>fields</c> / <c>props</c> - see
        /// <see cref="SerializedMemberShape.StructuralKeys"/>) with unrecognised ones is NOT covered
        /// here - it is trying to be a <see cref="SerializedMember"/> and getting it wrong, which is a
        /// hard failure. The descriptive keys <c>name</c> / <c>typeName</c> deliberately do NOT count:
        /// they are ordinary English words that consumer reference shapes carry too, and treating them
        /// as proof of intent rejected legitimate references.
        /// </para>
        /// <para>
        /// <b>Escape hatch.</b> A declined payload is judged "nobody understood it" when no converter
        /// produced a value for it, and "produced no value" is observed as <c>result == null</c>. A
        /// derived converter that OWNS a foreign shape and must be able to resolve it to a legitimate
        /// <c>null</c> (a cleared object reference, a deleted asset, ...) should override this to
        /// return <c>false</c> for the shapes it owns. The base then never classifies those payloads
        /// as declined, so the converter's own <c>null</c> is reported as
        /// <see cref="DeserializationOutcome.Handled"/> instead of raising a terminal failure.
        /// </para>
        /// </remarks>
        protected virtual bool DeclinesValueByShape(SerializedMember data)
            => AllowCascadeSerialization
            && data.valueJsonElement != null
            && SerializedMemberShape.Classify(data.valueJsonElement).IsForeign;

        /// <summary>
        /// Deserializes the <c>value</c> payload of <paramref name="data"/> into
        /// <paramref name="type"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A payload that IS this converter's shape but is BROKEN throws
        /// <see cref="DeserializationException"/> rather than yielding the target type's default
        /// value. Returning a default there would be indistinguishable from a successful
        /// deserialization: the caller has no success channel besides <paramref name="result"/>, and
        /// a boxed <c>default(T)</c> even satisfies <see cref="Type.IsInstanceOfType"/>. Callers that
        /// DO have an error channel (<c>SetField</c> / <c>SetProperty</c>) use
        /// <see cref="TryDeserializeValueReporting"/>, which converts the exception back into
        /// <c>false</c> plus a <see cref="LogType.Error"/> entry.
        /// </para>
        /// <para>
        /// ⚠ A payload that is NOT this converter's shape at all (a JSON object with no STRUCTURAL
        /// <see cref="SerializedMember"/> key, e.g. a consumer object reference
        /// <c>{"instanceID":"12345"}</c> or <c>{"instanceID":"7","typeName":"UnityEngine.Rigidbody"}</c>)
        /// is a completely different thing:
        /// <see cref="DeserializationOutcome.NotApplicable"/>. It does NOT throw, it does NOT log at
        /// Error, and it returns <c>true</c> with a <c>null</c> <paramref name="result"/> - the
        /// "carry on, someone else may understand this" signal that a derived converter relies on
        /// when it calls <c>base.TryDeserializeValueInternal(...)</c> and then applies its own
        /// resolution. Making this branch loud is exactly the regression this design exists to
        /// prevent; the loudness lives at the chain end, in <see cref="Deserialize"/>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// <c>true</c> when a value was produced, and also when the payload was declined by shape
        /// (with <paramref name="result"/> left <c>null</c> so the caller can tell the two apart).
        /// </returns>
        /// <exception cref="DeserializationException">
        /// The value payload IS this converter's shape but could not be deserialized.
        /// </exception>
        protected virtual bool TryDeserializeValueInternal(
            Reflector reflector,
            SerializedMember data,
            out object? result,
            Type type,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            if (reflector == null) throw new ArgumentNullException(nameof(reflector));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (type == null) throw new ArgumentNullException(nameof(type));

            var padding = StringUtils.GetPadding(depth);

            if (AllowCascadeSerialization)
            {
                // Both an absent 'value' and an explicit JSON `null` mean "no value" - that is a
                // legitimate outcome, not a failure.
                if (data.valueJsonElement == null ||
                    data.valueJsonElement.Value.ValueKind == JsonValueKind.Null)
                {
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}'value' is null. Converter: {GetType().GetTypeShortName()}");

                    result = GetDefaultValue(reflector, type);
                    return true;
                }
                if (data.valueJsonElement.Value.ValueKind != JsonValueKind.Object)
                {
                    var message = $"Failed to deserialize member '{data.name.ValueOrNull()}' of type '{type.GetTypeId()}': "
                        + $"'value' is not a JSON object, it is '{data.valueJsonElement.Value.ValueKind}'. "
                        + $"Converter: {GetType().GetTypeShortName()}";

                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{Consts.Emoji.Fail} {message}");

                    logs?.Error(message, depth);

                    throw new DeserializationException(message, type, data.name);
                }

                // ---- Tri-state: NotApplicable -------------------------------------------------
                // The payload is a JSON object that carries not one STRUCTURAL SerializedMember key
                // ('value'/'fields'/'props'). It is not a broken SerializedMember - it is not a
                // SerializedMember at all, and this converter simply has nothing to say about it.
                // Decline QUIETLY (Trace, never Error, never an exception) so a derived converter that
                // DOES understand this shape still gets to resolve it. A 'name'/'typeName' on the
                // payload changes nothing: Unity's own object-reference converters emit them.
                // See DeserializationOutcome / SerializedMemberShape for the full rationale.
                if (SerializedMemberShape.Classify(data.valueJsonElement).IsForeign)
                {
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}'{SerializedMember.ValueName}' is not a '{nameof(SerializedMember)}' shape - declining. Converter: {GetType().GetTypeShortName()}");

                    logs?.Trace($"'{SerializedMember.ValueName}' of member '{data.name.ValueOrNull()}' is not a '{nameof(SerializedMember)}' shape. "
                        + $"Converter '{GetType().GetTypeShortName()}' declines it - another converter may handle it.", depth);

                    // Deliberately NOT GetDefaultValue(type): a fabricated default is exactly what
                    // makes a failure indistinguishable from a success. `null` here means
                    // "nothing produced", which is what the caller needs in order to classify the
                    // outcome as NotApplicable.
                    result = null;
                    return true;
                }

                try
                {
                    result = data.valueJsonElement.DeserializeValueSerializedMember(
                        reflector,
                        type: type,
                        name: data.name,
                        depth: depth + 1,
                        logs: logs,
                        logger: logger);
                    return true;
                }
                catch (JsonException ex)
                {
                    var message = $"Failed to deserialize member '{data.name.ValueOrNull()}' of type '{type.GetTypeId()}':\n{ex.Message}";

                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{Consts.Emoji.Fail} Deserialize 'value', type='{type.GetTypeId()}' name='{data.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");

                    logs?.Error(message, depth);

                    throw new DeserializationException(message, type, data.name, ex);
                }
                catch (NotSupportedException ex)
                {
                    var message = $"Unsupported type '{type.GetTypeId()}' for member '{data.name.ValueOrNull()}':\n{ex.Message}";

                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{Consts.Emoji.Fail} Deserialize 'value', type='{type.GetTypeId()}' name='{data.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");

                    logs?.Error(message, depth);

                    throw new DeserializationException(message, type, data.name, ex);
                }
            }
            else
            {
                try
                {
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}Deserialize as json. Converter: {GetType().GetTypeShortName()}");

                    result = DeserializeValueAsJsonElement(
                        reflector: reflector,
                        data: data,
                        type: type,
                        depth: depth,
                        logs: logs,
                        logger: logger);

                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}{Consts.Emoji.Done} Deserialized as json: {data.valueJsonElement}");

                    return true;
                }
                catch (DeserializationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var message = $"Failed to deserialize value '{data.name.ValueOrNull()}' of type '{type.GetTypeId()}':\n{ex.Message}";

                    logs?.Error(message, depth);
                    if (logger?.IsEnabled(LogLevel.Critical) == true)
                        logger.LogCritical($"{padding}{Consts.Emoji.Fail} Deserialize 'value', type='{type.GetTypeId()}' name='{data.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");

                    throw new DeserializationException(message, type, data.name, ex);
                }
            }
        }

        /// <summary>
        /// Bool-returning wrapper around <see cref="TryDeserializeValue"/> for callers that have a
        /// real error channel (a <c>bool</c> result plus a <see cref="Logs"/> sink) and therefore
        /// report a deserialization failure instead of throwing - <c>SetField</c> / <c>SetProperty</c>
        /// and, through them, <c>Modify</c> / <c>TryModify</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <paramref name="result"/> is never set to the target type's default value on failure:
        /// a boxed <c>default(T)</c> is indistinguishable from a successfully deserialized value.
        /// </para>
        /// <para>
        /// These callers are a CHAIN END - there is no further converter to try - so a
        /// <see cref="DeserializationOutcome.NotApplicable"/> payload becomes an explicit reported
        /// failure here, exactly as it does at the end of <see cref="Deserialize"/>. It is only on
        /// the way THROUGH the chain that <c>NotApplicable</c> stays quiet.
        /// </para>
        /// </remarks>
        /// <returns><c>true</c> when the value was deserialized; otherwise <c>false</c>.</returns>
        protected bool TryDeserializeValueReporting(
            Reflector reflector,
            SerializedMember? data,
            out object? result,
            out Type? type,
            Type? fallbackType = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            try
            {
                var success = TryDeserializeValue(
                    reflector,
                    data: data,
                    result: out result,
                    type: out type,
                    outcome: out var outcome,
                    fallbackType: fallbackType,
                    depth: depth,
                    logs: logs,
                    logger: logger);

                if (!success || outcome != DeserializationOutcome.NotApplicable)
                    return success;

                // Chain end: nobody understood the payload shape.
                var padding = StringUtils.GetPadding(depth);
                var shape = SerializedMemberShape.Classify(data!.valueJsonElement);
                var message = $"Failed to deserialize member '{data.name.ValueOrNull()}' of type '{type!.GetTypeId()}':\n"
                    + $"No converter understood the '{SerializedMember.ValueName}' payload shape."
                    + (shape.UnknownKeys.Count > 0 ? $" {shape.DescribeUnknownKeys()}" : string.Empty);

                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{Consts.Emoji.Fail} {message}");

                logs?.Error(message, depth);
                logs.RecordMember(data.name, MemberOutcome.ResolutionFailed, message, depth);

                result = null;
                return false;
            }
            catch (DeserializationException ex)
            {
                // TryDeserializeValueInternal already wrote the detailed reason into `logs`/`logger`.
                result = null;
                type = ex.TargetType;
                return false;
            }
        }

        protected virtual object? DeserializeValueAsJsonElement(
            Reflector reflector,
            SerializedMember data,
            Type type,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            if (reflector == null) throw new ArgumentNullException(nameof(reflector));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (type == null) throw new ArgumentNullException(nameof(type));

            return reflector.JsonSerializer.Deserialize(
                reflector,
                data.valueJsonElement,
                type);
        }
    }
}