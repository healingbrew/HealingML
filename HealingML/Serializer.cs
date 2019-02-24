using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HealingML
{
    public static class Serializer
    {
        private static readonly Dictionary<Type, MemberInfo[]> TypeCache = new Dictionary<Type, MemberInfo[]>();
        private static readonly Dictionary<Type, SerializationTarget> TargetCache = new Dictionary<Type, SerializationTarget>();

        public static string Print(object instance, IReadOnlyDictionary<Type, ISerializer> customTypeSerializers = null)
        {
            return Print(instance, customTypeSerializers ?? new Dictionary<Type, ISerializer>(), new HashSet<object>(), new SpaceIndentHelper(), null);
        }

        private static string Print(object instance, IReadOnlyDictionary<Type, ISerializer> customTypeSerializers, HashSet<object> visited, IndentHelperBase indents, string valueName)
        {
            var type = instance?.GetType();
            ISerializer customSerializer = null;
            SerializationTarget target;
            if (type != null && customTypeSerializers.ContainsKey(type))
            {
                customSerializer = customTypeSerializers[type];
                target = customSerializer.OverrideTarget;
            }
            else
            {
                target = GetSerializationTarget(type);
            }

            var hmlNameTag = string.Empty;
            if (!string.IsNullOrWhiteSpace(valueName)) hmlNameTag = $" hml:name=\"{valueName}\"";

            switch (target)
            {
                case SerializationTarget.Null:
                    return $"{indents}<hml:null{hmlNameTag} />\n";
                case SerializationTarget.Value when type != default:
                    if (customSerializer == null) customSerializer = ToStringSerializer.Default;

                    return $"{indents}<{FormatName(type.Name)}>{FormatTextValueType(customSerializer.Print(instance, visited, indents + 1, valueName))}</{FormatName(type.Name)}>\n";
                case SerializationTarget.Array when type != default:
                    if (visited.Add(instance))
                    {
                        var tag = $"{indents}<hml:array hml:id=\"{instance.GetHashCode()}\"{hmlNameTag}>\n";
                        var array = instance as Array;
                        var innerIndent = indents + 1;
                        if (array == default)
                            tag += $"{innerIndent}<hml:null />\n";
                        else
                            for (long i = 0; i < array.LongLength; ++i)
                                tag += Print(array.GetValue(i), customTypeSerializers, visited, innerIndent, null);

                        tag += $"{indents}</hml:array>\n";
                        return tag;
                    }
                    else
                    {
                        return $"{indents}<hml:ref hml:id=\"{instance.GetHashCode()}\"{hmlNameTag} />\n";
                    }
                case SerializationTarget.Object when type != default:
                    if (visited.Add(instance))
                    {
                        var tag = $"{indents}<{FormatName(type.Name)} hml:id=\"{instance.GetHashCode()}\"{hmlNameTag}";
                        var members = GetMembers(type);
                        var complexMembers = new List<(object value, string memberName, ISerializer custom)>();
                        foreach (var member in members)
                        {
                            var value = GetMemberValue(instance, member);
                            var valueType = value?.GetType();
                            ISerializer targetCustomSerializer = null;
                            SerializationTarget targetMemberTarget;
                            if (valueType != null && customTypeSerializers.ContainsKey(valueType))
                            {
                                targetCustomSerializer = customTypeSerializers[valueType];
                                targetMemberTarget = targetCustomSerializer.OverrideTarget;
                            }
                            else
                            {
                                targetMemberTarget = GetSerializationTarget(valueType);
                            }

                            if (targetMemberTarget >= SerializationTarget.Complex)
                                complexMembers.Add((value, member.Name, targetCustomSerializer));
                            else
                                tag += $" {member.Name}=\"{(targetCustomSerializer != null ? targetCustomSerializer.Print(value, visited, indents, member.Name) : FormatValueType(value))}\"";
                        }

                        if (complexMembers.Count == 0)
                        {
                            tag += " />\n";
                        }
                        else
                        {
                            tag += ">\n";
                            var innerIndent = indents + 1;
                            foreach (var (value, name, custom) in complexMembers) tag += custom != null ? custom.Print(value, visited, indents, name) : Print(value, customTypeSerializers, visited, innerIndent, name);

                            tag += $"{indents}</{FormatName(type.Name)}>\n";
                        }

                        return tag;
                    }
                    else
                    {
                        return $"{indents}<hml:ref hml:id=\"{instance.GetHashCode()}\"{hmlNameTag} />\n";
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string FormatTextValueType(object instance)
        {
            return instance == default ? "{null}" : instance.ToString().Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("<", "\\<").Replace(">", "\\>");
        }


        private static string FormatValueType(object instance)
        {
            return instance == default ? "{null}" : instance.ToString().Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\"");
        }

        private static string FormatName(string typeName)
        {
            return typeName.Replace('<', '_').Replace('>', '_').Replace('`', '_');
        }

        private static object GetMemberValue(object instance, MemberInfo member)
        {
            switch (member)
            {
                case FieldInfo field:
                    return field.GetValue(instance);
                case PropertyInfo property:
                    return property.GetValue(instance);
                default:
                    return null;
            }
        }

        private static IEnumerable<MemberInfo> GetMembers(Type type)
        {
            // ReSharper disable once InvertIf
            if (!TypeCache.TryGetValue(type, out var members))
            {
                members = type.GetFields().Cast<MemberInfo>().Concat(type.GetProperties()).ToArray();
                TypeCache.Add(type, members);
            }

            return members;
        }

        private static SerializationTarget GetSerializationTarget(Type type)
        {
            // ReSharper disable once InvertIf
            if (type == default || !TargetCache.TryGetValue(type, out var target))
            {
                if (type == default) return SerializationTarget.Null;

                if (type.IsArray)
                    target = SerializationTarget.Array;
                else if (type.IsValueType || type.IsEnum || type.IsPrimitive || type == typeof(string))
                    target = SerializationTarget.Value;
                else
                    target = SerializationTarget.Object;

                TargetCache.Add(type, target);
            }

            return target;
        }

        public static void ClearCache()
        {
            TypeCache.Clear();
            TargetCache.Clear();
        }
    }
}
