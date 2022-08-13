namespace Parlot.Protobuf;

using Parlot.Fluent;
using System;
using static Parlot.Fluent.StringParsers<Parlot.Fluent.ParseContextWithScanner<char>>;

public class ProtoParser
{
    static ProtoParser()
    {
        PropertyParser = Terms.Text("string").Then(t => new Property { TypeCode = TypeCode.String })
        .Or(Terms.Text("double").Then(t => new Property { TypeCode = TypeCode.Double }))
        .Or(Terms.Text("float").Then(t => new Property { TypeCode = TypeCode.Single }))
        .Or(Terms.Text("int32").Then(t => new Property { TypeCode = TypeCode.Int32 }))
        .Or(Terms.Text("int64").Then(t => new Property { TypeCode = TypeCode.Int64 }))
        .Or(Terms.Text("uint32").Then(t => new Property { TypeCode = TypeCode.UInt32 }))
        .Or(Terms.Text("uint64").Then(t => new Property { TypeCode = TypeCode.UInt64 }))
        .Or(Terms.Text("sint32").Then(t => new Property { isFixedSize = true, TypeCode = TypeCode.Int32 }))
        .Or(Terms.Text("sint64").Then(t => new Property { isFixedSize = true, TypeCode = TypeCode.Int64 }))
        .Or(Terms.Text("fixed32").Then(t => new Property { isFixedSize = true, TypeCode = TypeCode.UInt32 }))
        .Or(Terms.Text("fixed64").Then(t => new Property { isFixedSize = true, TypeCode = TypeCode.UInt64 }))
        .Or(Terms.Text("sfixed32").Then(t => new Property { isFixedSize = true, TypeCode = TypeCode.Int32 }))
        .Or(Terms.Text("sfixed64").Then(t => new Property { isFixedSize = true, TypeCode = TypeCode.Int64 }))
        .Or(Terms.Text("bool").Then(t => new Property { TypeCode = TypeCode.Boolean }))
        .Or(Terms.Text("string").Then(t => new Property { TypeCode = TypeCode.String }))
        .Or(Terms.Text("bytes").Then(t => new Property { TypeCode = TypeCode.Byte }))
        .And(Terms.Identifier()).Then(t => { t.Item1.Name = t.Item2.ToString(); return t.Item1; })
        .AndSkip(Terms.Char('='))

        ;

        MessageParser = Terms.Text("message").SkipAnd(Terms.Identifier()).AndSkip(Between(Terms.Char('{'), Separated(Terms.Char(';'), PropertyParser), Terms.Char('}')))
        .Then(t => new Message { Name = t.ToString() })
        ;
    }

    public static Parser<Property, Parlot.Fluent.ParseContextWithScanner<char>> PropertyParser { get; }
    public static Parser<Message, Parlot.Fluent.ParseContextWithScanner<char>> MessageParser { get; }
}