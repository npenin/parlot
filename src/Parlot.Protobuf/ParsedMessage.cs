namespace Parlot.Protobuf;

using System;
using System.Collections.Generic;
using System.Reflection;

public class ParsedMessage
{
    public Message Definition;

    public List<ParsedValue> Values = new();

    public T To<T>()
    where T : new()
    {
        var result = new T();

        foreach (var value in Values)
        {
            var member = typeof(T).GetMember(value.Definition.Name);
            if (member == null || member.Length != 1)
            {
            }
            else
            {
                switch (member[0].MemberType)
                {
                    case System.Reflection.MemberTypes.Field:
                        ((FieldInfo)member[0]).SetValue(result, value.Value);
                        break;
                    case System.Reflection.MemberTypes.Property:
                        ((PropertyInfo)member[0]).SetValue(result, value.Value);
                        break;
                    default:
                        throw new NotSupportedException();
                }

            }
        }

        return result;
    }
}