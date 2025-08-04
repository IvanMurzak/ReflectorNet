using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Convertor;

namespace com.IvanMurzak.ReflectorNet
{
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

            IEnumerable<IReflectionConvertor> FindRelevantConvertors(Type type) => _serializers
                .Select(s => (s, s.SerializationPriority(type)))
                .Where(s => s.Item2 > 0)
                .OrderByDescending(s => s.Item2)
                .Select(s => s.s);

            public IReflectionConvertor? GetConvertor(Type type)
            {
                return FindRelevantConvertors(type)
                    .FirstOrDefault();
            }
        }
    }
}
