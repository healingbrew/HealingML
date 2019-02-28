using System;
using System.Collections.Generic;

namespace HealingML
{
    public interface ISerializer
    {
        SerializationTarget OverrideTarget { get; }

        // ReSharper disable UnusedParameter.Global
        object Print(object instance, IReadOnlyDictionary<Type, ISerializer> customTypeSerializers, HashSet<object> visited, IndentHelperBase indents, string valueName);
        // ReSharper restore UnusedParameter.Global
    }
}
