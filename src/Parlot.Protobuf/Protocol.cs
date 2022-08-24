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

    public string Package;

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
                if (p.Declaration is Enum<ulong>)
                    return new VarUInt<ParseContext>().Then(v => new ParsedValue { Definition = p, Value = v });
                return Sub(new VarUInt<ParseContext>(), parsers[p.Type]).Then(v => new ParsedValue { Definition = p, MessageValue = v });
            default:
                throw new NotSupportedException();
        }
    }

    public IDictionary<string, Parlot.Fluent.Deferred<ParsedMessage, ParseContext, byte>> BuildParsers()
    {
        var parsers = Declarations.Where(d => d is not Enum<ulong>).ToDictionary(d => d.Name, (d) => Parsers.Deferred<ParsedMessage>());
        foreach (var m in Declarations.OfType<Message>())
        {
            if (m.OneOf == null)
                parsers[m.Name].Parser = ZeroOrMany(Parsers.Switch(new VarUInt<ParseContext>(), (c, prefix) =>
                {
                    var wireType = prefix & 0x7;
                    var propertyIndex = prefix >> 3;
                    // System.Console.WriteLine($"message: {m.Name}, wireType:{wireType}, index:{propertyIndex}");
                    // System.Console.WriteLine(string.Join(", ", m.Properties.Select(p => p.Index + ":" + p.Name)));
                    switch (wireType)
                    {
                        case 0:
                            return new VarUInt<ParseContext>().Then(v => new ParsedValue { Definition = m.Properties.First(p => p.Index == propertyIndex), Value = v });
                        case 2:
                        case 1:
                        case 5:
                            return m.Properties.Where(p => p.Index == propertyIndex && p.WireType == wireType).Select(p => TypeParser(p, parsers)
                            // .Then(x => Console.WriteLine("successfully parsed property " + p.Name))
                            .ElseError("Failed to parse property " + p.Name)).First();
                    }
                    return m.Properties.Where(p => p.Index == propertyIndex).Select(p => TypeParser(p, parsers)
                        // .Then(x => Console.WriteLine("successfully parsed property " + p.Name))
                        .ElseError("Failed to parse property " + p.Name)).First();
                }))
                // m.Properties.Where(p => p.Required).Select(p => PropertyParser(p, parsers)).ToArray())
                // .And(AllOf(true, m.Properties.Where(p => p.Repeated).Select(p => ZeroOrMany(PropertyParser(p, parsers)).Then(values => new ParsedValue
                //  {
                //      Definition = p,
                //      Values = values.Select(v => v.Value).ToArray(),
                //      MessageValues = values.Select(v => v.MessageValue).ToArray()
                //  })).ToArray()))
                // .And(AllOf(true, m.Properties.Where(p => p.Optional).Select(p => ZeroOrOne(PropertyParser(p, parsers))).ToArray()))
                .Then(t => new ParsedMessage { Definition = m, Values = t })
                // .Then(c => System.Console.WriteLine($"read {m.Name} message"))
                ;
            else
                parsers[m.Name].Parser = Parsers.Switch(new VarUInt<ParseContext>(), (c, prefix) =>
                {
                    var wireType = prefix & 0x7;
                    var propertyIndex = prefix >> 3;
                    // System.Console.WriteLine($"oneof message: {m.Name}.{m.OneOf.Name}, wireType:{wireType}, index:{propertyIndex}");
                    // System.Console.WriteLine(string.Join(", ", m.OneOf.Possibilities.Select(p => p.Index + ":" + p.Name + "(wireType:" + p.WireType + ", Type:" + p.Type + ")")));
                    switch (wireType)
                    {
                        case 0:
                            return new VarUInt<ParseContext>().Then(v => new ParsedValue { Definition = m.OneOf.Possibilities.First(p => p.Index == propertyIndex), Value = v });
                        case 2:
                        case 1:
                        case 5:
                            return m.OneOf.Possibilities.Where(p => p.Index == propertyIndex && p.WireType == wireType).Select(p => TypeParser(p, parsers)
                            // .Then(x => Console.WriteLine("successfully parsed property " + p.Name))
                            .ElseError("Failed to parse property " + p.Name)).First();
                    }
                    return m.OneOf.Possibilities.Where(p => p.Index == propertyIndex).Select(p => TypeParser(p, parsers)
                    // .Then(x => Console.WriteLine("successfully parsed property " + p.Name))
                    .ElseError("Failed to parse property " + p.Name)).First();
                })
                .Then(v => new ParsedMessage { Definition = m, Values = new() { v } })
                // .Then(c => System.Console.WriteLine($"read {m.Name} oneof message"))
                ;

        }
        return parsers;
    }

    public Protocol Build()
    {
        IEnumerable<(Property, List<Enum<ulong>>)> remainingProperties = Declarations.SelectMany(d =>
        {
            if (d is Message m)
                return m.Properties.Select<Property, (Property, List<Enum<ulong>>)>(p => new(p, m.Enums));
            if (d is OneOf o)
                return o.Possibilities.Select<Property, (Property, List<Enum<ulong>>)>(p => new(p, new List<Enum<ulong>>(0)));
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