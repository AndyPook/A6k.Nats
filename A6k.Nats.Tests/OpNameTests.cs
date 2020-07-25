using System;
using System.Text;
using A6k.Nats.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace A6k.Nats.Tests
{
    public class OpNameTests
    {
        private ITestOutputHelper output;

        public OpNameTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData("INFO")]
        [InlineData("CONNECT")]
        [InlineData("PUB")]
        [InlineData("SUB")]
        [InlineData("UNSUB")]
        [InlineData("MSG")]
        [InlineData("PING")]
        [InlineData("PONG")]
        [InlineData("+OK")]
        [InlineData("-ERR")]

        public void NameToId(string op)
        {
            output.WriteLine($"{op} - {NatsOperation.GetOpId(op)}");
            Assert.Equal(NatsOperation.GetOpId(op), NatsOperation.GetOpId(op.ToLowerInvariant()));
        }

        [Fact]
        public void NatsOperationId_verify_enum()
        {
            var names = Enum.GetNames(typeof(NatsOperationId));
            foreach(var name in names)
            {
                var opId = name switch
                {
                    "OK" => NatsOperation.GetOpId("+OK"),
                    "ERR" => NatsOperation.GetOpId("-ERR"),
                    _ => NatsOperation.GetOpId(name)
                };
                var n = Enum.GetName(typeof(NatsOperationId), opId);

                Assert.Equal(name, n);
            }
        }
    }
}
