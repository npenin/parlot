namespace Parlot.Protobuf;

using Parlot.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using ParseContext = Fluent.ParseContextWithScanner<byte>;
using Parsers = Parlot.Fluent.Parsers<Fluent.ParseContextWithScanner<byte>, byte>;
using static Parlot.Fluent.ByteParsers<Fluent.ParseContextWithScanner<byte>>;

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

    public IDictionary<string, Parlot.Fluent.Deferred<ParsedMessage, ParseContext, byte>> BuildParser()
    {
        var parsers = Declarations.ToDictionary(d => d.Name, (d) => Parsers.Deferred<ParsedMessage>());
        foreach (var d in Declarations)
        {
            if (d is Message m)
            {
                m.Properties.OrderBy(p => p.Index).Select(p =>
                {
                    switch (p.TypeCode)
                    {
                        case TypeCode.Double:
                            return Double().Then(v => new ParsedValue { Definition = p, Value = v });
                        case TypeCode.Float:
                            return Float().Then(v => new ParsedValue { Definition = p, Value = v });
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.SInt32:
                        case TypeCode.SInt64:
                            return new VarInt<ParseContext>().Then(v => new ParsedValue { Definition = p, Value = v });
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
                        case TypeCode.Map:

                        case TypeCode.Declaration:
                            return parsers[p.Type].Then(v => new ParsedValue { Definition = p, MessageValue = v });
                        default:
                            throw new NotSupportedException();
                    }
                });
            }
            else if (d is OneOf o)
            {

            }
            else if (d is Enum<uint> e)
            {

            }
        }

        return parsers;
    }

    public Protocol Build()
    {
        uint hasRemaining;
        do
        {
            hasRemaining = 0;
            foreach (var d in Declarations)
            {
                if (d is Message m)
                    foreach (var p in m.Properties.Where(p => p.TypeCode == TypeCode.Declaration))
                    {
                        p.Declaration = m.Enums.FirstOrDefault(e => e.Name == p.Type);
                        if (p.Declaration == null)
                            p.Declaration = PrivateDeclarations.FirstOrDefault(e => e.Name == p.Type);
                        if (p.Declaration == null)
                            p.Declaration = Declarations.FirstOrDefault(e => e.Name == p.Type);
                        if (p.Declaration == null)
                            hasRemaining++;
                    }
                if (d is OneOf o)
                {
                    foreach (var p in o.Possibilities.Where(p => p.TypeCode == TypeCode.Declaration))
                    {
                        if (p.Declaration == null)
                            p.Declaration = PrivateDeclarations.FirstOrDefault(e => e.Name == p.Type);
                        if (p.Declaration == null)
                            p.Declaration = Declarations.FirstOrDefault(e => e.Name == p.Type);
                        if (p.Declaration == null)
                            hasRemaining++;
                    }
                }
            }
        }
        while (hasRemaining > 0);
        return this;
    }
}