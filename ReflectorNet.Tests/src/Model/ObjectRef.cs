using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Tests.Model
{
    [System.Serializable]
    [Description("Reference to UnityEngine.Object instance. It could be GameObject, Component, Asset, etc. Anything extended from UnityEngine.Object.")]
    public class ObjectRef
    {
        [JsonInclude, JsonPropertyName("instanceID")]
        [Description("Instance ID of the UnityEngine.Object. If this is 0 and assetPath is not provided or empty or null, then it will be used as 'null'.")]
        public int instanceID;

        [JsonInclude, JsonPropertyName("assetPath")]
        public string? assetPath;

        [JsonInclude, JsonPropertyName("assetGuid")]
        public string? assetGuid;

        public ObjectRef() : this(id: 0) { }
        public ObjectRef(int id) => instanceID = id;
        public ObjectRef(string assetPath) => this.assetPath = assetPath;

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            if (instanceID != 0)
                stringBuilder.Append($"instanceID={instanceID}");

            if (!StringUtils.IsNullOrEmpty(assetPath))
            {
                if (stringBuilder.Length > 0)
                    stringBuilder.Append(", ");
                stringBuilder.Append($"assetPath={assetPath}");
            }

            if (!StringUtils.IsNullOrEmpty(assetGuid))
            {
                if (stringBuilder.Length > 0)
                    stringBuilder.Append(", ");
                stringBuilder.Append($"assetGuid={assetGuid}");
            }
            if (stringBuilder.Length == 0)
                return $"instanceID={instanceID}";

            return stringBuilder.ToString();
        }
    }
}