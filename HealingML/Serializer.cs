using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace HealingML
{
    public static class Serializer
    {
        private static readonly Dictionary<Type, MemberInfo[]> TypeCache = new Dictionary<Type, MemberInfo[]>();
        private static readonly Dictionary<Type, SerializationTarget> TargetCache = new Dictionary<Type, SerializationTarget>();

        public static string Print(object instance, IReadOnlyDictionary<Type, ISerializer> customTypeSerializers = null, bool useRef = true)
        {
            return Print(instance, customTypeSerializers ?? new Dictionary<Type, ISerializer>(), new Dictionary<object, int>(), new SpaceIndentHelper(), null, useRef);
        }

        public static string Print(object instance, IReadOnlyDictionary<Type, ISerializer> customTypeSerializers, Dictionary<object, int> visited, IndentHelperBase indents, string valueName, bool useRef)
        {
            var type = instance?.GetType();
            ISerializer customSerializer = null;
            var target = GetCustomSerializer(customTypeSerializers, type, ref customSerializer);

            var hmlNameTag = string.Empty;
            if (!string.IsNullOrWhiteSpace(valueName)) hmlNameTag = $" hml:name=\"{valueName}\"";

            var innerIndent = indents + 1;

            switch (target)
            {
                case SerializationTarget.Null:
                    return $"{indents}<hml:null{hmlNameTag} />\n";
                case SerializationTarget.Object when type != default && customSerializer != null:
                case SerializationTarget.Array when type != default && customSerializer != null:
                    return customSerializer.Print(instance, customTypeSerializers, visited, indents, valueName, useRef) as string;
                case SerializationTarget.Value when type != default:
                    return $"{indents}<{FormatName(type.Name)}>{FormatTextValueType((customSerializer ?? ToStringSerializer.Default).Print(instance, customTypeSerializers, visited, innerIndent, valueName, useRef))}</{FormatName(type.Name)}>\n";
                case SerializationTarget.Array when type != default:
                case SerializationTarget.Enumerable when type != default:
                    if (!visited.ContainsKey(instance)) 
                    {
                        visited[instance] = visited.Count;
                        
                        var hmlIdTag = string.Empty;
                        if (useRef) {
                            hmlIdTag = $" hml:id=\"{visited[instance]}\"";
                        }
                        
                        var tag = $"{indents}<hml:array{hmlIdTag}{hmlNameTag}>\n";
                        if (target == SerializationTarget.Enumerable && instance is IEnumerable enumerable) instance = enumerable.Cast<object>().ToArray();
                        if (!(instance is Array array))
                            tag += $"{innerIndent}<hml:null />\n";
                        else
                            for (long i = 0; i < array.LongLength; ++i)
                                tag += Print(array.GetValue(i), customTypeSerializers, visited, innerIndent, null, useRef);

                        tag += $"{indents}</hml:array>\n";
                        return tag;
                    }
                    else
                    {
                        var hmlIdTag = string.Empty;
                        if (useRef) {
                            hmlIdTag = $" hml:id=\"{visited[instance]}\"";
                        }
                        
                        return $"{indents}<hml:ref{hmlIdTag}{hmlNameTag}>\n";
                    }
                case SerializationTarget.Object when type != default:
                    if (!visited.ContainsKey(instance))
                    {
                        visited[instance] = visited.Count;
                        
                        var hmlIdTag = string.Empty;
                        if (useRef) {
                            hmlIdTag = $" hml:id=\"{visited[instance]}\"";
                        }
                        
                        var tag = $"{indents}<{FormatName(type.Name)}{hmlIdTag}{hmlNameTag}";
                        var members = GetMembers(type);
                        var complexMembers = new List<(object value, string memberName, ISerializer custom)>();
                        foreach (var member in members)
                        {
                            var value = GetMemberValue(instance, member);
                            var valueType = value?.GetType();
                            ISerializer targetCustomSerializer = null;
                            var targetMemberTarget = GetCustomSerializer(customTypeSerializers, valueType, ref targetCustomSerializer);

                            if (targetMemberTarget >= SerializationTarget.Complex)
                                complexMembers.Add((value, member.Name, targetCustomSerializer));
                            else
                                tag += $" {member.Name}=\"{(targetCustomSerializer != null ? targetCustomSerializer.Print(value, customTypeSerializers, visited, indents, member.Name, useRef) : FormatValueType(value))}\"";
                        }

                        if (complexMembers.Count == 0)
                        {
                            tag += " />\n";
                        }
                        else
                        {
                            tag += ">\n";
                            foreach (var (value, name, custom) in complexMembers) tag += custom != null ? custom.Print(value, customTypeSerializers, visited, innerIndent, name, useRef) : Print(value, customTypeSerializers, visited, innerIndent, name, useRef);

                            tag += $"{indents}</{FormatName(type.Name)}>\n";
                        }

                        return tag;
                    }
                    else
                    {
                        var hmlIdTag = string.Empty;
                        if (useRef) {
                            hmlIdTag = $" hml:id=\"{visited[instance]}\"";
                        }

                        return $"{indents}<hml:ref{hmlIdTag}{hmlNameTag}>\n";
                    }
                case SerializationTarget.Dictionary when type != default:
                    if (!visited.ContainsKey(instance))
                    {
                        visited[instance] = visited.Count;
                        var hmlKeyTag = string.Empty;
                        var hmlValueTag = string.Empty;
                        
                        var hmlIdTag = string.Empty;
                        if (useRef) {
                            hmlIdTag = $" hml:id=\"{visited[instance]}\"";
                        }

                        var @base = type;
                        while (@base != null)
                        {
                            if (@base.IsConstructedGenericType && (@base.GetGenericTypeDefinition().IsEquivalentTo(typeof(IDictionary<,>)) || @base.GetGenericTypeDefinition().IsEquivalentTo(typeof(Dictionary<,>))))
                            {
                                var args = @base.GetGenericArguments();
                                if (args.Length > 1)
                                {
                                    hmlKeyTag = $" hml:key=\"{args[0].Name}\"";
                                    hmlValueTag = $" hml:value=\"{args[1].Name}\"";
                                    break;
                                }
                            }

                            @base = @base.BaseType;
                        }

                        var tag = $"{indents}<hml:map{hmlIdTag}{hmlNameTag}{hmlKeyTag}{hmlValueTag}";

                        if (!(instance is IDictionary dictionary)) return null;

                        if (dictionary.Count == 0)
                        {
                            tag += " />\n";
                            return tag;
                        }

                        tag += ">\n";

                        var innerInnerIndent = innerIndent + 1;

                        var values = dictionary.Values.Cast<object>().ToArray();
                        var keys = dictionary.Keys.Cast<object>().ToArray();

                        for (var i = 0; i < values.Length; ++i)
                        {
                            var value = values.GetValue(i);
                            var key = keys.GetValue(i);

                            var valueType = value?.GetType();
                            var keyType = key?.GetType();

                            ISerializer customValueSerializer = null;
                            ISerializer customKeySerializer = null;

                            var valueTarget = GetCustomSerializer(customTypeSerializers, valueType, ref customValueSerializer);
                            var keyTarget = GetCustomSerializer(customTypeSerializers, keyType, ref customKeySerializer);

                            if (valueTarget == SerializationTarget.Null)
                                tag += $"{innerIndent}<hml:null";
                            else
                                // ReSharper disable once PossibleNullReferenceException
                                tag += $"{innerIndent}<{FormatName(valueType.Name)}";

                            if (keyTarget == SerializationTarget.Null)
                                tag += " />";
                            else if (keyTarget < SerializationTarget.Complex) tag += $" hml:key=\"{FormatTextValueType((customSerializer ?? ToStringSerializer.Default).Print(key, customTypeSerializers, visited, innerIndent, valueName, useRef))}\"";

                            if (valueTarget != SerializationTarget.Null && valueTarget < SerializationTarget.Complex) tag += $" hml:value=\"{FormatTextValueType((customSerializer ?? ToStringSerializer.Default).Print(value, customTypeSerializers, visited, innerIndent, valueName, useRef))}\"";

                            if (valueTarget < SerializationTarget.Complex && keyTarget < SerializationTarget.Complex)
                            {
                                tag += " />\n";
                            }
                            else
                            {
                                tag += ">\n";
                                if (keyTarget >= SerializationTarget.Complex) tag += Print(key, customTypeSerializers, visited, innerInnerIndent, "hml:key", useRef);
                                if (valueTarget >= SerializationTarget.Complex) tag += Print(value, customTypeSerializers, visited, innerInnerIndent, "hml:value", useRef);
                                if (valueTarget == SerializationTarget.Null)
                                    tag += $"{innerIndent}</hml:null>\n";
                                else
                                    // ReSharper disable once PossibleNullReferenceException
                                    tag += $"{innerIndent}</{FormatName(valueType.Name)}>\n";
                            }
                        }

                        tag += $"{indents}</tank:map>\n";
                        return tag;
                    }
                    else
                    {
                        var hmlIdTag = string.Empty;
                        if (useRef) {
                            hmlIdTag = $" hml:id=\"{visited[instance]}\"";
                        }

                        return $"{indents}<hml:ref{hmlIdTag}{hmlNameTag}>\n";
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static SerializationTarget GetCustomSerializer(IReadOnlyDictionary<Type, ISerializer> customTypeSerializers, Type type, ref ISerializer customSerializer)
        {
            SerializationTarget target;
            if (type != null && customTypeSerializers.Any(x => x.Key.IsAssignableFrom(type)))
            {
                customSerializer = customTypeSerializers.First(x => x.Key.IsAssignableFrom(type)).Value;
                target = customSerializer.OverrideTarget;
            }
            else if (type != null && type.IsConstructedGenericType && customTypeSerializers.Any(x => x.Key.IsAssignableFrom(type.GetGenericTypeDefinition())))
            {
                customSerializer = customTypeSerializers.First(x => x.Key.IsAssignableFrom(type.GetGenericTypeDefinition())).Value;
                target = customSerializer.OverrideTarget;
            }
            else
            {
                target = GetSerializationTarget(type);
            }

            return target;
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
                members = type.GetFields().Cast<MemberInfo>().Concat(type.GetProperties()).Where(x => x.GetCustomAttribute<IgnoreDataMemberAttribute>() == null).ToArray();
                TypeCache.Add(type, members);
            }

            return members;
        }

        public static SerializationTarget GetSerializationTarget(Type type)
        {
            // ReSharper disable once InvertIf
            if (type == default || !TargetCache.TryGetValue(type, out var target))
            {
                if (type == default) return SerializationTarget.Null;

                if (type.IsArray || typeof(Array).IsAssignableFrom(type))
                    target = SerializationTarget.Array;
                else if (type.IsEnum || type.IsPrimitive || type == typeof(string))
                    target = SerializationTarget.Value;
                else if (typeof(IDictionary).IsAssignableFrom(type))
                    target = SerializationTarget.Dictionary;
                else if (typeof(IEnumerable).IsAssignableFrom(type))
                    target = SerializationTarget.Enumerable;
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
