using System;
using System.Collections.Generic;

namespace HealingML
{
    public class ToStringSerializer : ISerializer
    {
        public static readonly ISerializer Default = new ToStringSerializer();

        public SerializationTarget OverrideTarget => SerializationTarget.Value;

        public object Print(object instance, IReadOnlyDictionary<Type, ISerializer> custom, Dictionary<object, int> visited, IndentHelperBase indent, string name, bool useRef)
        {
            return instance.ToString();
        }
    }
}
