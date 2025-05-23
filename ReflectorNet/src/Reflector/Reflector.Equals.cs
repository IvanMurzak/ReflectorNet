#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Reflection
{
    /// <summary>
    /// Serializes Unity components to JSON format.
    /// </summary>
    public partial class Reflector
    {
        public bool AreEqual(object? a, object? b)
        {
            if (a == null && b == null) return true;
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.GetType() != b.GetType()) return false;

            var type = a.GetType();
            var convertor = Convertors.BuildSerializersChain(a.GetType()).First();

            var fields = convertor.GetSerializableFields(this, type);
            if (fields != null)
            {
                foreach (var prop in fields)
                {
                    var aValue = prop.GetValue(a);
                    var bValue = prop.GetValue(b);
                    if (!AreEqual(aValue, bValue))
                        return false;
                }
            }

            var properties = convertor.GetSerializableFields(this, type);
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    var aValue = prop.GetValue(a);
                    var bValue = prop.GetValue(b);
                    if (!AreEqual(aValue, bValue))
                        return false;
                }
            }

            return true;
        }
    }
}
