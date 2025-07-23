using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsString
    {
        public static string ValueOrNull(this string? value) => value == null ? StringUtils.Null : value;
    }
}