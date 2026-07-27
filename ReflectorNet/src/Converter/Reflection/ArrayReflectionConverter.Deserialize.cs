/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    public partial class ArrayReflectionConverter : BaseReflectionConverter<Array>
    {
        /// <summary>
        /// Deserializes a collection (array or <see cref="IList{T}"/>) from a <c>value</c> payload that
        /// must be a JSON array.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is the CHAIN END for collection types, exactly as
        /// <see cref="BaseReflectionConverter{T}.Deserialize"/> is for everything else, and it obeys the
        /// same contract: it has no success channel other than its return value, so a payload it cannot
        /// turn into a collection is raised as a <see cref="DeserializationException"/> rather than
        /// answered with <c>null</c>, an empty collection, or a short one. Before this was the case, a
        /// <c>value</c> payload that was not a JSON array returned <c>null</c> with nothing louder than
        /// a <see cref="LogType.Warning"/>, and the caller wrote that <c>null</c> over a live collection
        /// while reporting success.
        /// </para>
        /// <para>
        /// Loudness lives HERE and nowhere else on the way in: the per-link work happens in
        /// <see cref="TryDeserializeCollectionValue"/>, which DECLINES a payload shape it does not own
        /// (<see cref="DeserializationOutcome.NotApplicable"/>) at <c>Trace</c> severity so a derived
        /// converter layering its own resolution still gets to see it. See
        /// <see cref="DeserializationOutcome"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="DeserializationException">
        /// The <c>value</c> payload could not be turned into a collection - it is not a JSON array, its
        /// element type cannot be determined, the collection instance cannot be created, or one of its
        /// ELEMENTS failed to deserialize. An element failure abandons the whole collection: see
        /// <see cref="ResolveElement"/> for why all-or-nothing is achievable here and was not for a
        /// member set.
        /// </exception>
        public override object? Deserialize(
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

            var padding = StringUtils.GetPadding(depth);
            var name = StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name;

            // For arrays and lists, we need special handling since the value is a IList<SerializedMember>
            var type = TypeUtils.GetTypeWithNamePriority(data, fallbackType, out var error);
            if (type == null)
            {
                var typeMessage = $"Failed to deserialize member '{name.ValueOrNull()}': {error}";

                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{Consts.Emoji.Fail} {typeMessage}");

                logs?.Error(typeMessage, depth);
                logs.RecordMember(name, MemberOutcome.ResolutionFailed, typeMessage, depth);

                throw new DeserializationException(typeMessage, null, name);
            }

            var outcome = TryDeserializeCollectionValue(
                reflector,
                data: data,
                type: type,
                result: out var result,
                name: name,
                depth: depth,
                logs: logs,
                logger: logger,
                context: context);

            // ---- Chain end -------------------------------------------------------------------
            // Nobody understood the payload's shape: not this converter (it declined by shape) and not
            // any derived converter layered on top of it. THIS is the one place allowed to be loud
            // about it, mirroring BaseReflectionConverter.Deserialize. A derived converter that DOES
            // resolve such a payload overrides TryDeserializeCollectionValue (or Deserialize entirely)
            // and never reaches this branch.
            if (outcome == DeserializationOutcome.NotApplicable)
            {
                var shape = SerializedMemberShape.Classify(data.valueJsonElement);
                var message = $"Failed to deserialize member '{name.ValueOrNull()}'"
                    + $" of type '{type.GetTypeId()}':\n"
                    + $"No converter understood the '{SerializedMember.ValueName}' payload shape."
                    + (shape.UnknownKeys.Count > 0 ? $" {shape.DescribeUnknownKeys()}" : string.Empty);

                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{Consts.Emoji.Fail} {message}");

                logs?.Error(message, depth);
                logs.RecordMember(name, MemberOutcome.ResolutionFailed, message, depth);

                throw new DeserializationException(message, type, name);
            }

            return result;
        }

        /// <summary>
        /// The per-link half of <see cref="Deserialize"/>: turns a <c>value</c> payload into a
        /// collection, or reports that this converter has nothing to say about the payload's shape.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ⚠ This method is QUIET on the decline path by design. A <c>value</c> payload that is a JSON
        /// object carrying no <see cref="SerializedMember"/> key at all - a consumer object reference
        /// such as <c>{"instanceID":"12345"}</c> - is not a broken collection payload, it is not a
        /// collection payload at all: it returns <see cref="DeserializationOutcome.NotApplicable"/> with
        /// a <c>Trace</c> log, never an <c>Error</c> and never an exception. That is the
        /// "carry on, someone else may understand this" signal a derived converter relies on. Making
        /// this branch loud is the exact regression the tri-state design exists to prevent.
        /// </para>
        /// <para>
        /// Override this to own a foreign payload shape for a collection type: return
        /// <see cref="DeserializationOutcome.Handled"/> with the resolved collection and the chain end
        /// in <see cref="Deserialize"/> will not fire.
        /// </para>
        /// </remarks>
        /// <returns>
        /// <see cref="DeserializationOutcome.Handled"/> when a collection (or a legitimate <c>null</c>
        /// for an absent payload) was produced;
        /// <see cref="DeserializationOutcome.NotApplicable"/> when the payload's shape is foreign.
        /// A hard failure is raised as <see cref="DeserializationException"/> rather than returned.
        /// </returns>
        /// <exception cref="DeserializationException">
        /// The payload IS meant for this converter but cannot be turned into a collection.
        /// </exception>
        protected virtual DeserializationOutcome TryDeserializeCollectionValue(
            Reflector reflector,
            SerializedMember data,
            Type type,
            out object? result,
            string? name = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            DeserializationContext? context = null)
        {
            if (reflector == null) throw new ArgumentNullException(nameof(reflector));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (type == null) throw new ArgumentNullException(nameof(type));

            var padding = StringUtils.GetPadding(depth);

            if (logger?.IsEnabled(LogLevel.Trace) == true)
            {
                logger.LogTrace("{padding}{icon} Deserialize 'value', type='{typeName}', collectionType='{collectionType}'",
                    padding,
                    Consts.Emoji.Start,
                    type.GetTypeShortName(),
                    type.IsArray
                        ? "Array"
                        : IsGenericList(type, out var _)
                            ? "IList<>"
                            : "IEnumerable");
            }

            // Both an absent 'value' and an explicit JSON `null` mean "no value" - a legitimate
            // outcome, not a failure. Same rule as BaseReflectionConverter.TryDeserializeValueInternal.
            if (data.valueJsonElement == null ||
                data.valueJsonElement.Value.ValueKind == JsonValueKind.Null)
            {
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}'{SerializedMember.ValueName}' is null. Converter: {GetType().GetTypeShortName()}");

                result = null;
                return DeserializationOutcome.Handled;
            }

            if (data.valueJsonElement.Value.ValueKind != JsonValueKind.Array)
            {
                // ---- Tri-state: NotApplicable ---------------------------------------------------
                // A JSON object that carries not one SerializedMember key is not a malformed
                // collection payload - it is a payload this converter simply has nothing to say
                // about. Decline QUIETLY so a derived converter that DOES understand this shape gets
                // to resolve it; the chain end in Deserialize is what turns an unresolved decline
                // into a single explicit failure.
                if (SerializedMemberShape.Classify(data.valueJsonElement).IsForeign)
                {
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}'{SerializedMember.ValueName}' is not a JSON array and not a '{nameof(SerializedMember)}' shape - declining. Converter: {GetType().GetTypeShortName()}");

                    logs?.Trace($"'{SerializedMember.ValueName}' of member '{name.ValueOrNull()}' is not a '{nameof(SerializedMember)}' shape. "
                        + $"Converter '{GetType().GetTypeShortName()}' declines it - another converter may handle it.", depth);

                    // Deliberately NOT an empty collection and NOT GetDefaultValue(type): a fabricated
                    // value is exactly what makes a failure indistinguishable from a success.
                    result = null;
                    return DeserializationOutcome.NotApplicable;
                }

                var message = $"Failed to deserialize member '{name.ValueOrNull()}' of type '{type.GetTypeId()}': "
                    + $"'{SerializedMember.ValueName}' is not a JSON array, it is '{data.valueJsonElement.Value.ValueKind}'. "
                    + $"Converter: {GetType().GetTypeShortName()}";

                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{Consts.Emoji.Fail} {message}");

                logs?.Error(message, depth);
                logs.RecordMember(name, MemberOutcome.ResolutionFailed, message, depth);

                throw new DeserializationException(message, type, name);
            }

            result = BuildCollection(
                reflector,
                jsonArray: data.valueJsonElement.Value,
                type: type,
                name: name,
                depth: depth,
                logs: logs,
                logger: logger,
                context: context);

            return DeserializationOutcome.Handled;
        }

        /// <summary>
        /// Builds the collection instance and fills it from <paramref name="jsonArray"/>.
        /// </summary>
        /// <exception cref="DeserializationException">
        /// The collection could not be created, or one of its elements failed to deserialize.
        /// </exception>
        object? BuildCollection(
            Reflector reflector,
            JsonElement jsonArray,
            Type type,
            string? name,
            int depth,
            Logs? logs,
            ILogger? logger,
            DeserializationContext? context)
        {
            var padding = StringUtils.GetPadding(depth);
            var length = jsonArray.GetArrayLength();

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (elementType == null)
                    throw Fail($"Failed to get element type for array type '{type.GetTypeId()}'.", type, name, depth, logs, logger);

                var array = Array.CreateInstance(elementType, length);
                if (array == null)
                    throw Fail($"Failed to create array instance for type '{type.GetTypeId()}'.", type, name, depth, logs, logger);

                // Register the array early (before deserializing elements) so child references can
                // resolve. A later element failure abandons the whole array by throwing, so this
                // half-filled instance is never handed to the caller.
                context?.Register(array);

                var index = 0;
                var nullElements = 0;
                foreach (var element in jsonArray.EnumerateArray())
                {
                    var value = ResolveElement(reflector, element, elementType, index, type, name, depth, logs, logger, context);
                    if (value != null)
                        array.SetValue(value, index);
                    else
                        nullElements++;
                    index++;
                }

                ReportNullElements(nullElements, length, name, depth, logs);
                return array;
            }

            if (IsGenericList(type, out var itemType))
            {
                var list = reflector.CreateInstance(type);
                if (list == null)
                    throw Fail($"Failed to create list instance for type '{type.GetTypeId()}'.", type, name, depth, logs, logger);

                var addMethod = type.GetMethod(nameof(IList<object>.Add));
                if (addMethod == null)
                    throw Fail($"Failed to find 'Add' method on list type '{type.GetTypeId()}'.", type, name, depth, logs, logger);

                // Register the list early (before deserializing elements) so child references can resolve
                context?.Register(list);

                var index = 0;
                var nullElements = 0;
                foreach (var element in jsonArray.EnumerateArray())
                {
                    var value = ResolveElement(reflector, element, itemType!, index, type, name, depth, logs, logger, context);
                    if (value == null)
                        nullElements++;

                    addMethod.Invoke(list, new[] { value });
                    index++;
                }

                ReportNullElements(nullElements, length, name, depth, logs);

                if (logger?.IsEnabled(LogLevel.Information) == true)
                    logger.LogInformation("{padding}Successfully created list of type='{typeName}'", padding, list.GetType().GetTypeId());

                return list;
            }

            throw Fail($"Type '{type.GetTypeId()}' is neither an array nor a generic list, so it cannot be deserialized by {GetType().GetTypeShortName()}.",
                type, name, depth, logs, logger);
        }

        /// <summary>
        /// Deserializes a single element of a collection.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>All-or-nothing, and why that differs from a member set.</b> An element that fails to
        /// deserialize abandons the WHOLE collection. <c>BaseReflectionConverter.Deserialize</c>
        /// deliberately settled for an honest <c>PartiallyApplied</c> state on a member set, because by
        /// the time a write fails it has already run consumer setters with side effects that cannot be
        /// undone. A collection has no such problem: it is BUILT here and handed back, and nothing is
        /// written to a live target until the caller assigns it. Atomicity is genuinely achievable, so
        /// pretending otherwise - returning a short or hole-punched collection that looks complete -
        /// would be a fabrication, not a compromise.
        /// </para>
        /// <para>
        /// <b>What is NOT treated as a failure.</b> An element that resolves to <c>null</c> from a
        /// non-null payload is NOT assumed broken when the element type can hold <c>null</c>: a
        /// blacklisted element type resolves to <c>null</c> by design, and a consumer converter resolves
        /// a cleared or deleted object reference to <c>null</c> as its correct answer. Calling those a
        /// failure would re-break exactly the consumers the tri-state design protects. They are
        /// disclosed instead - see <see cref="ReportNullElements"/> - never silently dropped. A
        /// non-nullable VALUE type is different: no converter can legitimately answer <c>null</c> for
        /// one, so that is a hard failure and is raised.
        /// </para>
        /// </remarks>
        /// <exception cref="DeserializationException">The element could not be deserialized.</exception>
        object? ResolveElement(
            Reflector reflector,
            JsonElement element,
            Type elementType,
            int index,
            Type collectionType,
            string? collectionName,
            int depth,
            Logs? logs,
            ILogger? logger,
            DeserializationContext? context)
        {
            var elementName = $"[{index}]";
            try
            {
                var member = ParseElementToMember(element);
                member.name = elementName; // Set array index as name for path tracking

                var value = reflector.Deserialize(
                    data: member,
                    fallbackType: elementType,
                    depth: depth + 1,
                    logs: logs,
                    logger: logger,
                    context: context);

                // A non-nullable value type can never legitimately be null. Leaving the slot at
                // default(T) here is the exact substitution `12f99093` removed elsewhere: the caller
                // would receive a full-length collection with a fabricated 0 / default struct in it.
                if (value == null && elementType.IsValueType && Nullable.GetUnderlyingType(elementType) == null)
                {
                    throw new DeserializationException(
                        $"Element {elementName} of '{collectionName.ValueOrNull()}' resolved to null, "
                            + $"but '{elementType.GetTypeId()}' is a non-nullable value type.",
                        elementType,
                        elementName);
                }

                return value;
            }
            catch (Exception ex)
            {
                var padding = StringUtils.GetPadding(depth);
                var message = $"Failed to deserialize element {elementName} of '{collectionName.ValueOrNull()}' "
                    + $"({collectionType.GetTypeId()}):\n{ex.Message}";

                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{Consts.Emoji.Fail} {message}");

                logs?.Error(message, depth);
                logs.RecordMember(elementName, MemberOutcome.ResolutionFailed, ex.Message, depth);

                // A DeserializationException that already names the member it failed on is rethrown
                // untouched - it carries the index and re-wrapping would only bury it. One that does
                // not (a malformed element rejected before its name was assigned) is re-raised WITH
                // the index, so the failure is always attributable to a position in the collection.
                // Either way the result stays a DeserializationException, which is what the callers
                // that convert it into a reported failure recognise - Reflector.MethodCall, TryModify,
                // TryDeserializeValueReporting. Anything else, a JsonException from a mistyped element
                // for instance, is wrapped for the same reason: those callers do not catch it, so it
                // would otherwise escape the call entirely.
                if (ex is DeserializationException deserializationException &&
                    !string.IsNullOrEmpty(deserializationException.MemberName))
                    throw;

                throw new DeserializationException(message, elementType, elementName, ex);
            }
        }

        /// <summary>
        /// Discloses elements that resolved to <c>null</c>. They are not treated as failures (see
        /// <see cref="ResolveElement"/>), but a collection that silently contains fewer real values than
        /// the payload described is exactly the "looks complete" problem, so the count is always
        /// reported. <c>Info</c>, never <c>Error</c>: a null element is legitimate for a blacklisted
        /// type or a cleared object reference.
        /// </summary>
        static void ReportNullElements(int nullElements, int length, string? name, int depth, Logs? logs)
        {
            if (nullElements <= 0)
                return;

            logs?.Info($"Deserialized '{name.ValueOrNull()}' with {length} element(s); "
                + $"{nullElements} of them resolved to null.", depth);
        }

        DeserializationException Fail(string message, Type type, string? name, int depth, Logs? logs, ILogger? logger)
        {
            var padding = StringUtils.GetPadding(depth);
            var text = $"Failed to deserialize member '{name.ValueOrNull()}' of type '{type.GetTypeId()}': {message}";

            if (logger?.IsEnabled(LogLevel.Error) == true)
                logger.LogError($"{padding}{Consts.Emoji.Fail} {text}");

            logs?.Error(text, depth);
            logs.RecordMember(name, MemberOutcome.ResolutionFailed, text, depth);

            return new DeserializationException(text, type, name);
        }

        /// <summary>
        /// <inheritdoc cref="BaseReflectionConverter{T}.TryDeserializeValueInternal"/>
        /// </summary>
        /// <remarks>
        /// Collection flavour of the overridable seam. It keeps the seam's contract exactly: a payload
        /// whose shape is foreign returns <c>true</c> with a <c>null</c> <paramref name="result"/> (the
        /// quiet decline a derived converter depends on), a payload that IS meant for this converter but
        /// is broken throws, and <paramref name="result"/> is NEVER set to a fabricated value - not
        /// <c>GetDefaultValue(type)</c> and not an empty <c>CreateInstance(type)</c> - on a failure.
        /// </remarks>
        protected override bool TryDeserializeValueInternal(
            Reflector reflector,
            SerializedMember serializedMember,
            out object? result,
            Type type,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (logger?.IsEnabled(LogLevel.Trace) == true)
            {
                logger.LogTrace("{padding}TryDeserializeValueInternal type='{typeName}', name='{name}', AllowCascadeSerialize={AllowCascadeSerialize}, Converter='{ConverterName}'",
                    padding,
                    type.GetTypeId(),
                    serializedMember.name.ValueOrNull(),
                    AllowCascadeSerialization,
                    GetType().Name);
            }

            if (AllowCascadeSerialization)
            {
                // Both an absent 'value' and an explicit JSON `null` mean "no value" - not a failure.
                if (serializedMember.valueJsonElement == null ||
                    serializedMember.valueJsonElement.Value.ValueKind == JsonValueKind.Null)
                {
                    result = reflector.GetDefaultValue(type);

                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                    {
                        logger.LogTrace("{padding}'value' is null for type='{typeName}', name='{name}'. Converter='{ConverterName}'",
                            padding,
                            type.GetTypeId(),
                            serializedMember.name.ValueOrNull(),
                            GetType().Name);
                    }
                    return true;
                }

                var isArray = serializedMember.valueJsonElement.Value.ValueKind == JsonValueKind.Array;
                if (!isArray)
                {
                    // Not a JSON array. If the payload carries no SerializedMember key at all it is a
                    // foreign shape: DECLINE quietly (Trace, never Error, never an exception) so a
                    // derived converter can resolve it. The chain end - TryDeserializeValueReporting,
                    // which backs SetField/SetProperty - reports it once if nobody does.
                    if (SerializedMemberShape.Classify(serializedMember.valueJsonElement).IsForeign)
                    {
                        if (logger?.IsEnabled(LogLevel.Trace) == true)
                            logger.LogTrace($"{padding}'{SerializedMember.ValueName}' is not a '{nameof(SerializedMember)}' shape - declining. Converter: {GetType().GetTypeShortName()}");

                        logs?.Trace($"'{SerializedMember.ValueName}' of member '{serializedMember.name.ValueOrNull()}' is not a '{nameof(SerializedMember)}' shape. "
                            + $"Converter '{GetType().GetTypeShortName()}' declines it - another converter may handle it.", depth);

                        result = null;
                        return true;
                    }

                    var message = $"Failed to deserialize member '{serializedMember.name.ValueOrNull()}' of type '{type.GetTypeId()}': "
                        + $"'{SerializedMember.ValueName}' is not a JSON array, it is '{serializedMember.valueJsonElement.Value.ValueKind}'. "
                        + $"Converter: {GetType().GetTypeShortName()}";

                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{Consts.Emoji.Fail} {message}");

                    logs?.Error(message, depth);

                    // Deliberately NOT `result = GetDefaultValue(type)`: a fabricated value on a failure
                    // path is indistinguishable from a real one.
                    throw new DeserializationException(message, type, serializedMember.name);
                }

                if (TryDeserializeValueListInternal(
                    reflector,
                    jsonElement: serializedMember.valueJsonElement,
                    type: type,
                    result: out var enumerableResult,
                    name: serializedMember.name,
                    depth: depth + 1,
                    logs: logs,
                    logger: logger))
                {
                    result = enumerableResult;

                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}{Consts.Emoji.Done} Deserialized as an enumerable.");

                    return true;
                }

                // The failure is already reported by TryDeserializeValueListInternal. Answering with an
                // empty `CreateInstance(type)` here was the collection flavour of the silent
                // substitution: a caller reading `result` got a plausible, EMPTY collection back.
                result = null;
                return false;
            }
            else
            {
                return base.TryDeserializeValueInternal(
                    reflector,
                    data: serializedMember,
                    result: out result,
                    type: type,
                    depth: depth,
                    logs: logs,
                    logger: logger);
            }
        }

        /// <summary>
        /// Bool-returning collection deserialization for the callers that have a real error channel
        /// (<c>SetValue</c> / <c>TryModify</c> and, through <see cref="TryDeserializeValueInternal"/>,
        /// <c>SetField</c> / <c>SetProperty</c>). It never throws: an element failure is reported as
        /// <c>false</c> plus a <see cref="LogType.Error"/> entry, and <paramref name="result"/> is left
        /// <c>null</c> rather than being filled with a partial collection.
        /// </summary>
        protected virtual bool TryDeserializeValueListInternal(
            Reflector reflector,
            JsonElement? jsonElement,
            Type type,
            out IEnumerable? result,
            string? name = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            DeserializationContext? context = null)
        {
            var padding = StringUtils.GetPadding(depth);
            var paddingNext = StringUtils.GetPadding(depth + 1);

            if (logger?.IsEnabled(LogLevel.Trace) == true)
            {
                logger.LogTrace("{padding}TryDeserializeValueListInternal name='{name}', type='{typeName}'",
                    padding,
                    name.ValueOrNull(),
                    type.GetTypeShortName());
            }

            try
            {
                name = name.ValueOrNull();

                if (jsonElement == null)
                {
                    result = null;
                    return true;
                }

                if (jsonElement.Value.ValueKind != JsonValueKind.Array)
                {
                    result = null;
                    return false;
                }

                var jsonArray = jsonElement.Value;
                var count = jsonArray.GetArrayLength();

                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}Deserializing '{name}' enumerable with {count} items.");

                logs?.Info($"Deserializing '{name}' enumerable with {count} items.", depth);

                var itemType = TypeUtils.GetEnumerableItemType(type);
                if (itemType == null)
                {
                    result = null;
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}Failed to determine element type for '{name}' of type '{type.GetTypeShortName()}'.");

                    logs?.Error($"Failed to determine element type for '{name}'.", depth);
                    return false;
                }

                // Create a properly typed List<T> instead of List<object?>
                var listType = typeof(List<>).MakeGenericType(itemType);
                var list = (IList?)Activator.CreateInstance(listType);
                if (list == null)
                {
                    result = null;
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}Failed to create list instance for type '{type.GetTypeShortName()}'.");

                    logs?.Error($"Failed to create list instance for type '{type.GetTypeShortName()}'.", depth);
                    return false;
                }

                // Register the list early (before deserializing elements) so child references can resolve
                context?.Register(list);

                var index = 0;
                var nullElements = 0;
                foreach (var element in jsonArray.EnumerateArray())
                {
                    var parsedValue = ResolveElement(reflector, element, itemType, index, type, name, depth, logs, logger, context);

                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{paddingNext}Enumerable[{index}] deserialized successfully: {parsedValue?.GetType().GetTypeShortName()}");

                    logs?.Info($"Enumerable[{index}] deserialized successfully.", depth + 1);

                    if (parsedValue == null)
                        nullElements++;

                    list.Add(parsedValue);
                    index++;
                }

                ReportNullElements(nullElements, count, name, depth, logs);

                if (type.IsArray)
                {
                    var typedArray = Array.CreateInstance(itemType, list.Count);
                    for (int j = 0; j < list.Count; j++)
                    {
                        typedArray.SetValue(list[j], j);
                    }
                    result = typedArray;

                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}Deserialized '{name}' as an array with {typedArray.Length} items.");

                    logs?.Success($"Deserialized '{name}' as an array with {typedArray.Length} items.", depth);
                }
                else
                {
                    // Return the properly typed List<T>
                    result = list;

                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}Deserialized '{name}' as a list with {list.Count} items.");

                    logs?.Success($"Deserialized '{name}' as a list with {list.Count} items.", depth);
                }

                return true;
            }
            catch (Exception ex)
            {
                // Never hand back a partially built collection: `result` stays null so a caller that
                // ignores the bool cannot mistake a half-filled list for the real thing.
                result = null;

                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Failed to deserialize '{name}': {ex.Message}\n{ex.StackTrace}");

                if (logs != null)
                    logs.Error($"Failed to deserialize '{name}': {ex.Message}", depth);

                return false;
            }
        }

        /// <summary>
        /// Converts a JsonElement from an array into a SerializedMember for deserialization.
        /// </summary>
        /// <param name="element">The JSON element to parse</param>
        /// <returns>
        /// A SerializedMember carrying the element's type information and value, or - when the element
        /// carries no <see cref="SerializedMember"/> key at all - a minimal SerializedMember holding
        /// the raw payload, so a converter that owns that foreign shape can still resolve it.
        /// </returns>
        /// <remarks>
        /// An element that DOES declare itself a <see cref="SerializedMember"/> (it carries
        /// <c>typeName</c>, <c>fields</c> or <c>props</c>) but fails to parse as one is a hard failure.
        /// It used to be swallowed and silently degraded to the raw-payload form, which threw the
        /// element's declared type and nested members away and then deserialized something else - the
        /// same swallow-and-substitute defect `12f99093` removed from
        /// <c>ExtensionsJsonElement.DeserializeValueSerializedMember</c>.
        /// </remarks>
        /// <exception cref="DeserializationException">
        /// The element declares itself a <see cref="SerializedMember"/> but cannot be parsed as one.
        /// </exception>
        protected virtual SerializedMember ParseElementToMember(JsonElement element)
        {
            SerializedMember? member = null;
            if (element.ValueKind == JsonValueKind.Object &&
                (
                    element.TryGetProperty(nameof(SerializedMember.typeName), out _) ||
                    element.TryGetProperty(nameof(SerializedMember.fields), out _) ||
                    element.TryGetProperty(nameof(SerializedMember.props), out _))
                )
            {
                try
                {
                    member = System.Text.Json.JsonSerializer.Deserialize<SerializedMember>(element.GetRawText());
                    if (member != null && element.TryGetProperty(SerializedMember.ValueName, out var valueProp))
                    {
                        member.valueJsonElement = valueProp;
                    }
                }
                catch (JsonException ex)
                {
                    throw new DeserializationException(
                        $"The element declares itself a '{nameof(SerializedMember)}' but cannot be parsed as one:\n{ex.Message}",
                        typeof(SerializedMember),
                        memberName: null,
                        innerException: ex);
                }
            }
            if (member == null)
            {
                member = new SerializedMember
                {
                    valueJsonElement = element
                };
            }
            return member;
        }
    }
}
