#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Common.Data.Unity
{
    [Serializable]
    public class SerializedMemberList : List<SerializedMember>
    {
        public SerializedMemberList() { }
        public SerializedMemberList(int capacity) : base(capacity) { }
        public SerializedMemberList(SerializedMember item) : base(1)
        {
            Add(item);
        }
        public SerializedMemberList(IEnumerable<SerializedMember> collection) : base(collection) { }

        public SerializedMember? GetField(string name)
            => this.FirstOrDefault(x => x.name == name);

        public override string ToString()
        {
            if (Count == 0)
                return "No items";

            var stringBuilder = new System.Text.StringBuilder();

            stringBuilder.AppendLine($"Items total amount: {Count}");

            for (int i = 0; i < Count; i++)
                stringBuilder.AppendLine($"Item[{i}] {this[i]}");

            return stringBuilder.ToString();
        }
    }
}