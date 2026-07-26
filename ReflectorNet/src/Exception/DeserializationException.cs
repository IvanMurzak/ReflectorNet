/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * SPDX-License-Identifier: Apache-2.0
 * Copyright (c) 2024-2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;

namespace com.IvanMurzak.ReflectorNet
{
    /// <summary>
    /// Exception thrown when the <c>value</c> payload of a
    /// <see cref="com.IvanMurzak.ReflectorNet.Model.SerializedMember"/> cannot be turned into an
    /// instance of the target type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception exists to guarantee that a failed deserialization is never mistaken for a
    /// successful one. Previously such a failure was swallowed and the target type's default value
    /// was returned instead, so the caller received a well-typed but meaningless value
    /// (<c>0</c>, <c>null</c>, <c>default(TStruct)</c>, ...) with no indication that anything went
    /// wrong. A boxed <c>default(T)</c> even satisfies <see cref="Type.IsInstanceOfType"/>, so
    /// parameter verification accepted it and the method was invoked with the wrong value.
    /// </para>
    /// <para>
    /// Value-returning APIs (<c>Reflector.Deserialize</c> and the converter <c>Deserialize</c>
    /// chain) have no success channel other than the returned value, so they throw this exception.
    /// The member-setting APIs (<c>SetField</c> / <c>SetProperty</c> / <c>Modify</c> /
    /// <c>TryModify</c>) do have a boolean result plus a <see cref="Model.Logs"/> sink, so they
    /// keep reporting the same failure as <c>false</c> plus a
    /// <see cref="Model.LogType.Error"/> entry instead of throwing.
    /// </para>
    /// </remarks>
    public class DeserializationException : Exception
    {
        /// <summary>
        /// Gets the type the value was supposed to be deserialized into.
        /// </summary>
        public Type? TargetType { get; }

        /// <summary>
        /// Gets the name of the member (field, property or method parameter) that failed to
        /// deserialize, when known.
        /// </summary>
        public string? MemberName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeserializationException"/> class.
        /// </summary>
        public DeserializationException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeserializationException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DeserializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeserializationException"/> class
        /// with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DeserializationException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeserializationException"/> class
        /// with a specified error message, the target type and the member name.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="targetType">The type the value was supposed to be deserialized into.</param>
        /// <param name="memberName">The name of the member that failed to deserialize.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DeserializationException(string message, Type? targetType, string? memberName = null, Exception? innerException = null)
            : base(message, innerException)
        {
            TargetType = targetType;
            MemberName = memberName;
        }
    }
}
