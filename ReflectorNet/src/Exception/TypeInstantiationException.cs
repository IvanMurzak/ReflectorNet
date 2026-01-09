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
    /// Exception thrown when a type cannot be instantiated during deserialization.
    /// This typically occurs when attempting to deserialize non-null data to an interface
    /// or abstract class type, which cannot be directly instantiated.
    /// </summary>
    public class TypeInstantiationException : Exception
    {
        /// <summary>
        /// Gets the type that could not be instantiated.
        /// </summary>
        public Type? TargetType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeInstantiationException"/> class.
        /// </summary>
        public TypeInstantiationException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeInstantiationException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TypeInstantiationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeInstantiationException"/> class
        /// with a specified error message and the type that could not be instantiated.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="targetType">The type that could not be instantiated.</param>
        public TypeInstantiationException(string message, Type targetType)
            : base(message)
        {
            TargetType = targetType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeInstantiationException"/> class
        /// with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TypeInstantiationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeInstantiationException"/> class
        /// with a specified error message, the type that could not be instantiated,
        /// and a reference to the inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="targetType">The type that could not be instantiated.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TypeInstantiationException(string message, Type targetType, Exception innerException)
            : base(message, innerException)
        {
            TargetType = targetType;
        }
    }
}
