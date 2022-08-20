using System.IO;
using Xunit;
using Parlot.Fluent;
using Parlot.Protobuf;

namespace Parlot.Tests.Calc
{

    public class BiscuitTests
    {
        [Fact]
        public void Parse()
        {
            var protocol = Protobuf.ProtoParser.ProtocolParser.Parse(FileParseContext.OpenFile(Path.Combine("..", "..", "..", "Biscuit.proto")));
            Assert.NotNull(protocol);
            protocol.Build();
            var parsers = protocol.BuildParsers();

            // parsers["OpBinary"].Parse(new Fluent.Byte.ParseContext(new Scanner<byte>(new[] { 0x12, 0x07, 0x74, 0x65, 0x73, 0x74, 0x69, 0x6e, 0x67 })))
        }
    }
}
