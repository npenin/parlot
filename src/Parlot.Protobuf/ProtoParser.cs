namespace Parlot.Protobuf;

using Parlot.Fluent;
using System;
using System.Linq;
using static Parlot.Fluent.StringParsers<FileParseContext>;

public class ProtoParser
{
    static ProtoParser()
    {
        PropertyParser = ZeroOrOne(Terms.Text("repeated")
            .Or(Terms.Text("required"))
            .Or(Terms.Text("optional")))
        .And(Terms.Text("double")
            .Or(Terms.Text("float"))
            .Or(Terms.Text("int32"))
            .Or(Terms.Text("int64"))
            .Or(Terms.Text("uint32"))
            .Or(Terms.Text("uint64"))
            .Or(Terms.Text("sint32"))
            .Or(Terms.Text("sint64"))
            .Or(Terms.Text("fixed32"))
            .Or(Terms.Text("fixed64"))
            .Or(Terms.Text("sfixed32"))
            .Or(Terms.Text("sfixed64"))
            .Or(Terms.Text("bool"))
            .Or(Terms.Text("string"))
            .Or(Terms.Text("bytes"))
            .Or(Terms.Identifier().Then(t => t.ToString()))
            )
        .And(Terms.Identifier().ElseError("bad identifier"))
        .AndSkip(Terms.Char('=').ElseError("missing equals"))
        .And(Terms.Integer().When(t => t >= 0).ElseError("bad index")).Then(t => new Property
        {
            Repeated = t.Item1 == "repeated",
            Optional = t.Item1 == "optional",
            Required = t.Item1 == "required",
            TypeCode = t.Item2 switch
            {
                "double" => TypeCode.Double,
                "float" => TypeCode.Float,
                "int32" => TypeCode.Int32,
                "int64" => TypeCode.Int64,
                "uint32" => TypeCode.UInt32,
                "uint64" => TypeCode.UInt64,
                "sint32" => TypeCode.Int32,
                "sint64" => TypeCode.Int64,
                "fixed32" => TypeCode.Int32,
                "fixed64" => TypeCode.Int64,
                "sfixed32" => TypeCode.Int32,
                "sfixed64" => TypeCode.Int64,
                "bool" => TypeCode.Boolean,
                "string" => TypeCode.String,
                "bytes" => TypeCode.Bytes,
                var x when x.StartsWith("map<") => TypeCode.Map,
                _ => TypeCode.Declaration
            },
            Type = t.Item2,
            IsFixedSize = t.Item2.Contains("fixed"),
            Name = t.Item3.ToString(),
            Index = (uint)t.Item4
        })
        ;

        EnumParser = Terms.Text("enum")
            .SkipAnd(Terms.Identifier().ElseError("invalid identifier"))
            .And(Between(
                Terms.Char('{').ElseError("expected opening curved brace for enum"),
                ZeroOrMany(Terms.Identifier().AndSkip(Terms.Char('=')).And(Terms.Integer()).AndSkip(Terms.Char(';').ElseError("missing semicolon"))),
                Terms.Char('}').ElseError("expected closing curved brace for enum"))
            )
            .Then(t => new Enum<uint>
            {
                Name = t.Item1.ToString(),
                Values = t.Item2.Select(v => new Enum<uint>.EnumValue { Value = (uint)v.Item2, Name = v.Item1.ToString() }).ToList()
            });
        ;

        MessageParser = Terms.Text("message")
            .SkipAnd(Terms.Identifier().ElseError("invalid identifier"))
            .And(Between(
                Terms.Char('{').ElseError("expected opening curved brace for message"),
                ZeroOrMany(EnumParser).And(
                ZeroOrMany(PropertyParser.AndSkip(Terms.Char(';').ElseError("missing semicolon")))),
                Terms.Char('}').ElseError("expected closing curved brace for message"))
            )
            .Then(t => new Message
            {
                Name = t.Item1.ToString(),
                Properties = t.Item2.Item2,
                Enums = t.Item2.Item1
            });

        var oneOfParser = Terms.Text("oneOf")
            .SkipAnd(Terms.Identifier().ElseError("invalid identifier"))
            .And(Between(
                Terms.Char('{').ElseError("expected opening curved brace for oneof"),
                ZeroOrMany(Terms.Identifier().And(Terms.Identifier()).AndSkip(Terms.Char('=')).And(Terms.Integer()).AndSkip(Terms.Char(';').ElseError("missing semicolon"))),
                Terms.Char('}').ElseError("expected closing curved brace for oneof"))
            )
            .Then(t => new OneOf
            {
                Name = t.Item1.ToString(),
                Possibilities = t.Item2.Select(v => new Property { Index = (uint)v.Item3, Name = v.Item1.ToString(), Type = v.Item2.ToString() }).ToList()
            });
        ;

        var syntaxParser = Terms.Text("syntax").SkipAnd(Terms.Char('=').ElseError("expected equals")).SkipAnd(Terms.String().ElseError("expected string").When(s => s.ToString() == "proto2").ElseError("supported syntax is proto2 only"));

        var protocolParser = Deferred<Protocol>();
        ProtocolParser = protocolParser;

        var importParser = Terms.Text("import").SkipAnd(ZeroOrOne(Terms.Text("weak").Or(Terms.Text("public")))).And(Terms.String().ElseError("expected string")).Then((c, t) => c.Protocol.Merge(protocolParser.Parse(c.Import(t.Item2.ToString())), t.Item1 == "private"));

        protocolParser.Parser = syntaxParser.Then<object>(x => x).And(ZeroOrMany(
                importParser.Then<object>(x => x)
            .Or(MessageParser.Then((c, x) => { c.Protocol.Declarations.Add(x); return (object)x; }))
            .Or(EnumParser.Then((c, x) => { c.Protocol.Declarations.Add(x); return (object)x; }))
            .Or(oneOfParser.Then((c, x) => { c.Protocol.Declarations.Add(x); return (object)x; }))))
            .Then((c, o) => c.Protocol.Build());
    }

    public static Parser<Property, FileParseContext, char> PropertyParser { get; }
    public static Parser<Message, FileParseContext, char> MessageParser { get; }
    public static Parser<Enum<uint>, FileParseContext, char> EnumParser { get; }
    public static Parser<Protocol, FileParseContext, char> ProtocolParser { get; }
}