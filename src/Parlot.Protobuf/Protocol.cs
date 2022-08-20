namespace Parlot.Protobuf;

using Parlot.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using ParseContext = Parlot.Fluent.Byte.ParseContext;
using Parsers = Parlot.Fluent.Parsers<Parlot.Fluent.Byte.ParseContext, byte>;
using static Parlot.Fluent.Byte.Parsers<Parlot.Fluent.Byte.ParseContext>;

public class Protocol
{
    public readonly List<Declaration> Declarations = new();
    public readonly List<Declaration> PrivateDeclarations = new();

    public void Merge(Protocol protocol, bool isPrivate)
    {
        if (isPrivate)
        {
            PrivateDeclarations.AddRange(protocol.Declarations);
            PrivateDeclarations.AddRange(protocol.PrivateDeclarations);
        }
        else
        {
            Declarations.AddRange(protocol.Declarations);
            PrivateDeclarations.AddRange(protocol.PrivateDeclarations);
        }
    }

    public static Parser<ParsedValue, ParseContext, byte> TypeParser(Property p, Dictionary<string, Deferred<ParsedMessage, ParseContext, byte>> parsers)
    {
        switch (p.TypeCode)
        {
            case TypeCode.Double:
                return Double().Then(v => new ParsedValue { Definition = p, Value = v });
            case TypeCode.Float:
                return Float().Then(v => new ParsedValue { Definition = p, Value = v });
            case TypeCode.SInt32:
            case TypeCode.SInt64:
                return new VarInt<ParseContext>().Then(v => new ParsedValue { Definition = p, Value = v });
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                return new VarUInt<ParseContext>().Then(v => new ParsedValue { Definition = p, Value = v });
            case TypeCode.Fixed32:
                return UInt32().Then(v => new ParsedValue { Definition = p, Value = v });
            case TypeCode.Fixed64:
                return UInt64().Then(v => new ParsedValue { Definition = p, Value = v });
            case TypeCode.Sfixed32:
                return Int32().Then(v => new ParsedValue { Definition = p, Value = v });
            case TypeCode.Sfixed64:
                return Int64().Then(v => new ParsedValue { Definition = p, Value = v });
            case TypeCode.Boolean:
            case TypeCode.String:
                return String(new VarUInt<ParseContext>()).Then(v => new ParsedValue { Definition = p, Value = v });
            case TypeCode.Bytes:
                return Buffer(new VarUInt<ParseContext>()).Then(v => new ParsedValue { Definition = p, Value = v });
            case TypeCode.Map:

            case TypeCode.Declaration:
                if (p.Declaration is Enum<uint>)
                    return new VarUInt<ParseContext>().Then(v => new ParsedValue { Definition = p, Value = v });
                return Sub(new VarUInt<ParseContext>(), parsers[p.Type]).Then(v => new ParsedValue { Definition = p, MessageValue = v });
            default:
                throw new NotSupportedException();
        }
    }

    public static Parser<ParsedValue, ParseContext, byte> PropertyParser(Property p, Dictionary<string, Deferred<ParsedMessage, ParseContext, byte>> parsers)
    {
        ulong prefix = p.Index << 3;
        switch (p.TypeCode)
        {
            case TypeCode.Float:
            case TypeCode.Fixed32:
            case TypeCode.Sfixed32:
                prefix = prefix | 0x5;
                break;
            case TypeCode.Double:
            case TypeCode.Fixed64:
            case TypeCode.Sfixed64:
                prefix = prefix | 0x1;
                break;
            case TypeCode.String:
                prefix = prefix | 0x2;
                break;
            case TypeCode.Bytes:
                prefix = prefix | 0x2;
                break;
            case TypeCode.Declaration:
                if (p.Declaration is not Enum<uint>)
                    prefix = prefix | 0x2;
                break;
                // case TypeCode.Map:
                //     prefix = prefix | 0x;
                //     break;
        }

        return new VarUInt<ParseContext>().When(v => v == prefix).SkipAnd(TypeParser(p, parsers).ElseError("failed to parse property " + p.Name));
    }


    public IDictionary<string, Parlot.Fluent.Deferred<ParsedMessage, ParseContext, byte>> BuildParsers()
    {
        var parsers = Declarations.Where(d => d is not Enum<uint>).ToDictionary(d => d.Name, (d) => Parsers.Deferred<ParsedMessage>());
        foreach (var d in Declarations)
        {
            if (d is Message m)
            {
                parsers[m.Name].Parser = AllOf(m.Properties.Where(p => p.Required).Select(p => PropertyParser(p, parsers)).ToArray())
                .And(AllOf(m.Properties.Where(p => p.Repeated).Select(p => ZeroOrMany(PropertyParser(p, parsers)).Then(values => new ParsedValue { Definition = p, Values = values.Select(v => v.Value).ToArray(), MessageValues = values.Select(v => v.MessageValue).ToArray() })).ToArray()))
                .And(AllOf(m.Properties.Where(p => p.Optional).Select(p => ZeroOrOne(PropertyParser(p, parsers))).ToArray()))
                .Then(t => new ParsedMessage { Definition = m, Values = t.Item1.Union(t.Item2).Union(t.Item3.Where(p => p != null)).ToList() });
            }
            else if (d is OneOf o)
            {
                parsers[d.Name].Parser = OneOf(o.Possibilities.Select(p => TypeParser(p, parsers).Then(p => p.MessageValue)).ToArray());
            }
        }

        return parsers;
    }

    public Protocol Build()
    {
        IEnumerable<(Property, List<Enum<uint>>)> remainingProperties = Declarations.SelectMany(d =>
        {
            if (d is Message m)
                return m.Properties.Select<Property, (Property, List<Enum<uint>>)>(p => new(p, m.Enums));
            if (d is OneOf o)
                return o.Possibilities.Select<Property, (Property, List<Enum<uint>>)>(p => new(p, new List<Enum<uint>>(0)));
            throw new NotSupportedException();
        }).Where(p => p.Item1.TypeCode == TypeCode.Declaration).ToList();
        bool processedOne;
        do
        {
            processedOne = false;
            remainingProperties = remainingProperties.Where(p =>
            {
                p.Item1.Declaration = p.Item2.FirstOrDefault(e => e.Name == p.Item1.Type);
                if (p.Item1.Declaration == null)
                    p.Item1.Declaration = PrivateDeclarations.FirstOrDefault(e => e.Name == p.Item1.Type);
                if (p.Item1.Declaration == null)
                    p.Item1.Declaration = Declarations.FirstOrDefault(e => e.Name == p.Item1.Type);
                if (p.Item1.Declaration == null)
                    return true;
                processedOne = true;
                return false;
            });
        }
        while (processedOne && remainingProperties.Any());
        if (remainingProperties.Any())
        {
            throw new ApplicationException("The protocol is not fully defined");
        }
        return this;
    }
}