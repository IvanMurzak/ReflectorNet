/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsException
    {
        /// <summary>
        /// Recursively retrieves the deepest inner exception.
        /// </summary>
        /// <param name="ex">The exception to inspect.</param>
        /// <returns>The deepest inner exception, or the exception itself if no inner exception exists.</returns>
        public static Exception GetDeepestInnerException(this Exception ex)
        {
            return ex.InnerException == null
                ? ex
                : ex.InnerException.GetDeepestInnerException();
        }
    }
}
