using System;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    /*
     * ReflectorNet
     * Author: Ivan Murzak (https://github.com/IvanMurzak)
     * Copyright (c) 2025 Ivan Murzak
     * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
     */

    public enum MimeType
    {
        TextPlain,
        TextHtml,
        TextJson,
        TextXml,
        TextYaml,
        TextCsv,
        TextMarkdown,
        TextJavascript,
    }

    public static class MimeTypeExtensions
    {
        public static string ToString(this MimeType mimeType) => mimeType switch
        {
            MimeType.TextPlain => "text/plain",
            MimeType.TextHtml => "text/html",
            MimeType.TextJson => "application/json",
            MimeType.TextXml => "application/xml",
            MimeType.TextYaml => "application/x-yaml",
            MimeType.TextCsv => "text/csv",
            MimeType.TextMarkdown => "text/markdown",
            MimeType.TextJavascript => "application/javascript",
            _ => throw new ArgumentOutOfRangeException(nameof(mimeType), mimeType, null)
        };
    }
}