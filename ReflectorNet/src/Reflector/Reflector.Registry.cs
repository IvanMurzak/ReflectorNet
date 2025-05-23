#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Reflection.Convertor;

namespace com.IvanMurzak.ReflectorNet.Reflection
{
    /// <summary>
    /// Serializes Unity components to JSON format.
    /// </summary>
    public partial class Reflector
    {
        public class Registry
        {
            ConcurrentBag<IReflectionConvertor> _serializers = new ConcurrentBag<IReflectionConvertor>();

            public Registry()
            {
                // Basics
                Add(new PrimitiveReflectionConvertor());
                Add(new GenericReflectionConvertor<object>());
                Add(new ArrayReflectionConvertor());
            }

            public void Add(IReflectionConvertor serializer)
            {
                if (serializer == null)
                    return;

                _serializers.Add(serializer);
            }
            public void Remove<T>() where T : IReflectionConvertor
            {
                var serializer = _serializers.FirstOrDefault(s => s is T);
                if (serializer == null)
                    return;

                _serializers = new ConcurrentBag<IReflectionConvertor>(_serializers.Where(s => s != serializer));
            }

            public IReadOnlyList<IReflectionConvertor> GetAllSerializers() => _serializers.ToList();

            IEnumerable<IReflectionConvertor> FindRelevantSerializers(Type type) => _serializers
                .Select(s => (s, s.SerializationPriority(type)))
                .Where(s => s.Item2 > 0)
                .OrderByDescending(s => s.Item2)
                .Select(s => s.s);

            public IEnumerable<IReflectionConvertor> BuildSerializersChain(Type type)
            {
                var serializers = FindRelevantSerializers(type);
                foreach (var serializer in serializers)
                {
                    yield return serializer;
                    if (!serializer.AllowCascadeSerialize)
                        break;
                }
            }
            public IReflectionConvertor? BuildDeserializersChain(Type type)
            {
                var serializers = FindRelevantSerializers(type);
                foreach (var serializer in serializers)
                    return serializer;

                return null;
            }
            public IEnumerable<IReflectionConvertor> BuildPopulatorsChain(Type type)
            {
                var serializers = FindRelevantSerializers(type);
                foreach (var serializer in serializers)
                {
                    yield return serializer;
                    if (!serializer.AllowCascadePopulate)
                        break;
                }
            }
        }
    }
}
