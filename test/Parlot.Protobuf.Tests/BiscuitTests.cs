using System.IO;
using Xunit;
using Parlot.Fluent;
using Parlot.Protobuf;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Parlot.Tests.Calc
{

    public class BiscuitTests
    {
        private static IDictionary<string, Deferred<ParsedMessage, Fluent.Byte.ParseContext, byte>> GetParsers()
        {
            Protobuf.ProtoParser.ProtocolParser.TryParse(FileParseContext.OpenFile(Path.Combine("..", "..", "..", "Biscuit.proto")), out var protocol, out var error);
            Assert.Null(error);
            Assert.NotNull(protocol);
            Assert.Equal(17, protocol.Declarations.Count);
            return protocol.Build().BuildParsers();
        }

        [Fact]
        public void ParseEnum()
        {
            var result = GetParsers()["OpBinary"].Parse(new Fluent.Byte.ParseContext(new Scanner<byte>(new byte[] { 0x08, 0x01 })));
            Assert.NotNull(result);
            var v = Assert.Single(result.Values);
            Assert.Equal("kind", v.Definition.Name);
            Assert.Equal((ulong)1, v.Value);
            Assert.Equal((ulong)1, v.GetValue());
        }

        [Fact]
        public void ParseDynamicEnum()
        {
            dynamic result = GetParsers()["OpBinary"].Parse(new Fluent.Byte.ParseContext(new Scanner<byte>(new byte[] { 0x08, 0x01 })));
            Assert.Equal((ulong)1, result.kind);
        }
    }
}
