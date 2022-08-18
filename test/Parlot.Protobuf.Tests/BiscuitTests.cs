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
            var parser=protocol.BuildParser();
        }
    }
}
