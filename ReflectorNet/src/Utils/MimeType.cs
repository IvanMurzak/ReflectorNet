using System;

namespace com.IvanMurzak.ReflectorNet.Utils
{
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