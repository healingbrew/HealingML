using System.Collections.Generic;

namespace HealingML
{
    public interface ISerializer
    {
        SerializationTarget OverrideTarget { get; }

        // ReSharper disable UnusedParameter.Global
        object Print(object instance, HashSet<object> visited, IndentHelperBase indent, string fieldName);
        // ReSharper restore UnusedParameter.Global
    }
}
