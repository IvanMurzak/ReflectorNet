#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
namespace com.IvanMurzak.ReflectorNet.Data
{
    public class ResponseCallToolContent
    {
        /// <summary>
        /// The type of content. This determines the structure of the content object. Can be "image", "audio", "text", "resource".
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The text content of the message.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// The base64-encoded image data.
        /// </summary>
        public string? Data { get; set; }

        /// <summary>
        /// The MIME type of the image.
        /// </summary>
        public string? MimeType { get; set; }

        /// <summary>
        /// The resource content of the message (if embedded).
        /// </summary>
        public ResponseResourceContent? Resource { get; set; }

        public ResponseCallToolContent() { }
    }
}