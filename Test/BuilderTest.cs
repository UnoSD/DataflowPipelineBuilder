using System.Linq;
using System.Threading.Tasks;
using DataflowPipelineBuilder;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Test
{
    [TestFixture]
    class BuilderTest
    {
        [Test]
        public async Task MixedPipeline()
        {
            var builder = 
                new Builder();

            var pipelineBlock = 
                builder.FromEnumerable(Enumerable.Repeat("{ }", 10))
                       .Select(JObject.Parse)
                       .Batch(3)
                       .SelectMany()
                       .Select(j => j)
                       .Batch(3)
                       .End();

            var actual = 
                await pipelineBlock.ReceiveAllAsync();

            Assert.That(actual.Count, Is.EqualTo(4));
            Assert.That(actual.Count(b => b.Count == 3), Is.EqualTo(3));
            Assert.That(actual.Count(b => b.Count == 1), Is.EqualTo(1));
        }
    }
}
