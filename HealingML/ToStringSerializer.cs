using System.Collections.Generic;

namespace HealingML
{
    public class ToStringSerializer : ISerializer
    {
        public static readonly ToStringSerializer Default = new ToStringSerializer();
        public SerializationTarget OverrideTarget => SerializationTarget.Value;

        public object Print(object instance, HashSet<object> visited, IndentHelperBase indent, string name)
        {
            return instance.ToString();
        }
    }
}
