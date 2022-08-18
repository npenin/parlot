using System.IO;
using Xunit;
using Parlot.Fluent;
using System;

namespace Parlot.Tests.Calc
{

    public class ProtoTests
    {
        [Fact]
        public void ShouldParseMessage()
        {
            var proto = @"
                message AuthorizerPolicies {
                    repeated string symbols = 1;
                    optional uint32 version = 2;
            }";

            var m = Protobuf.ProtoParser.MessageParser.Parse(new Protobuf.FileParseContext(proto, false));
            Assert.NotNull(m);
            Assert.Equal("AuthorizerPolicies", m.Name);
            Assert.Equal(2, m.Properties.Count);
            Assert.Equal("symbols", m.Properties[0].Name);
            Assert.Equal(Protobuf.TypeCode.String, m.Properties[0].TypeCode);
            Assert.Equal<uint>(1, m.Properties[0].Index);
            Assert.Equal("version", m.Properties[1].Name);
            Assert.Equal(Protobuf.TypeCode.UInt32, m.Properties[1].TypeCode);
            Assert.Equal<uint>(2, m.Properties[1].Index);
        }


        [Fact]
        public void ShouldParseEnumInMessage()
        {
            var proto = @"               
message OpBinary {
  enum Kind {
    LessThan = 0;
    GreaterThan = 1;
    LessOrEqual = 2;
    GreaterOrEqual = 3;
    Equal = 4;
    Contains = 5;
    Prefix = 6;
    Suffix = 7;
    Regex = 8;
    Add = 9;
    Sub = 10;
    Mul = 11;
    Div = 12;
    And = 13;
    Or = 14;
    Intersection = 15;
    Union = 16;
  }

  required Kind kind = 1;
}";

            var m = Protobuf.ProtoParser.MessageParser.Parse(new Protobuf.FileParseContext(proto, false));
            Assert.NotNull(m);
            Assert.Equal("OpBinary", m.Name);
            var p = Assert.Single(m.Properties);
            Assert.Equal("kind", p.Name);
            Assert.Equal(Protobuf.TypeCode.Declaration, p.TypeCode);
            Assert.Equal("Kind", p.Type);
            var e = Assert.Single(m.Enums);
            Assert.Equal(17, e.Values.Count);
        }


        [Fact]
        public void ShouldParseEnum()
        {
            var proto = @"enum Kind {
                Allow = 0;
                Deny = 1;
            }";

            var m = Protobuf.ProtoParser.EnumParser.Parse(new Protobuf.FileParseContext(proto, false));
            Assert.NotNull(m);
            Assert.Equal("Kind", m.Name);
            Assert.Equal(2, m.Values.Count);
            Assert.Equal("Allow", m.Values[0].Name);
            Assert.Equal<uint>(0, m.Values[0].Value);
            Assert.Equal("Deny", m.Values[1].Name);
            Assert.Equal<uint>(1, m.Values[1].Value);
        }


        [Theory]
        [InlineData("repeated string symbols = 1", true, false, Protobuf.TypeCode.String, false, "symbols", 1)]
        [InlineData("optional uint32 version = 2", false, true, Protobuf.TypeCode.UInt32, false, "version", 2)]
        public void ShouldParseProperty(string proto, bool repeated, bool optional, Protobuf.TypeCode type, bool isFixed, string name, uint index)
        {
            Assert.True(Protobuf.ProtoParser.PropertyParser.TryParse(new Protobuf.FileParseContext(proto), out var p));
            Assert.NotNull(p);
            Assert.Equal(repeated, p.Repeated);
            Assert.Equal(optional, p.Optional);
            Assert.Equal(isFixed, p.IsFixedSize);
            Assert.Equal(name, p.Name);
            Assert.Equal(type, p.TypeCode);
            Assert.Equal<uint>(index, p.Index);
        }
    }
}
