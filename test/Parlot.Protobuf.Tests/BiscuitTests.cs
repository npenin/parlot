using System.IO;
using Xunit;
using Parlot.Fluent;
using Parlot.Protobuf;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;

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


        [Theory]
        // [InlineData("test10_authorizer_scope.bc?raw=true")]
        // [InlineData("test11_authorizer_authority_caveats.bc?raw=true")]
        // [InlineData("test12_authority_caveats.bc?raw=true")]
        // [InlineData("test13_block_rules.bc?raw=true")]
        // [InlineData("test14_regex_constraint.bc?raw=true")]
        // [InlineData("test15_multi_queries_caveats.bc?raw=true")]
        // [InlineData("test16_caveat_head_name.bc?raw=true")]
        // [InlineData("test17_expressions.bc?raw=true")]
        // [InlineData("test18_unbound_variables_in_rule.bc?raw=true")]
        // [InlineData("test19_generating_ambient_from_variables.bc?raw=true")]
        [InlineData("test1_basic.bc?raw=true")]
        // [InlineData("test20_sealed.bc?raw=true")]
        // [InlineData("test21_parsing.bc?raw=true")]
        // [InlineData("test22_default_symbols.bc?raw=true")]
        // [InlineData("test23_execution_scope.bc?raw=true")]
        // [InlineData("test2_different_root_key.bc?raw=true")]
        // [InlineData("test3_invalid_signature_format.bc?raw=true")]
        // [InlineData("test4_random_block.bc?raw=true")]
        // [InlineData("test5_invalid_signature.bc?raw=true")]
        // [InlineData("test6_reordered_blocks.bc?raw=true")]
        // [InlineData("test7_scoped_rules.bc?raw=true")]
        // [InlineData("test8_scoped_checks.bc?raw=true")]
        // [InlineData("test9_expired_token.bc?raw=true")]
        public async Task ParseSamples(string url)
        {
            HttpClient client = new HttpClient();
            var content = await client.GetByteArrayAsync(new Uri(new Uri("https://github.com/biscuit-auth/biscuit/blob/master/samples/v2/"), url));
            dynamic result = GetParsers()["Biscuit"].Parse(new Fluent.Byte.ParseContext(new Scanner<byte>(content)));
            System.Console.WriteLine(JsonConvert.SerializeObject(result));
        }
    }
}
