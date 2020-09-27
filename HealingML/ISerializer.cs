using System;
using System.Collections.Generic;

namespace HealingML
{
    public interface ISerializer
    {
        SerializationTarget OverrideTarget { get; }

        // ReSharper disable UnusedParameter.Global
        object Print(object instance, IReadOnlyDictionary<Type, ISerializer> customTypeSerializers, Dictionary<object, int> visited, IndentHelperBase indents, string valueName, bool useRef);
        // ReSharper restore UnusedParameter.Global
    }
}
