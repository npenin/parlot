namespace Parlot.Protobuf;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

public class ParsedMessage : DynamicObject
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

    public override IEnumerable<string> GetDynamicMemberNames()
    {
        System.Console.WriteLine("GetDynamicMemberNames");
        if (Definition.OneOf != null)
            return new[] { Definition.OneOf.Name };
        return Definition.Properties.Select(p => p.Name);
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        var values = Values.Where(v => v.Definition.Name == binder.Name).ToList();
        switch (values.Count)

        {
            case 0:
                result = null;
                return false;
            case 1:
                result = values[0].GetValue();
                return true;
            default:
                result = values.SelectMany(v => v.GetValues());
                return true;
        }
    }
}