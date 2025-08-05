using System;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class Reflector
    {
        public T? CreateInstance<T>() => (T?)CreateInstance(typeof(T));
        public object? CreateInstance(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            var convertor = Convertors.GetConvertor(type);
            if (convertor == null)
                throw new ArgumentException($"[Error] Type '{type?.GetTypeName(pretty: false).ValueOrNull()}' not supported for creating instance.");

            return convertor.CreateInstance(this, type);
        }

        public T? GetDefaultValue<T>() => (T?)GetDefaultValue(typeof(T));
        public object? GetDefaultValue(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            var convertor = Convertors.GetConvertor(type);
            if (convertor == null)
                throw new ArgumentException($"[Error] Type '{type?.GetTypeName(pretty: false).ValueOrNull()}' not supported for default value.");

            return convertor.GetDefaultValue(this, type);
        }
    }
}
