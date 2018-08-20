using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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

        [Test]
        public async Task Fork()
        {
            var builder = 
                new Builder();

            var pipelineBlock = 
                builder.Create(new BufferBlock<string>())
                       .Select(JObject.Parse)
                       .Fork(source => source.Select(s => s.Properties().First().Name),
                             source => source.Select(s => s.Properties().First().Value.Value<int>()))
                       .End();

            await
                Enumerable.Range(1, 3)
                          .Select(index => $@"{{ ""value{index}"": {index} }}")
                          .Select(pipelineBlock.SendAsync)
                          .WhenAll()
                          .Then(() => pipelineBlock.Complete());

            var actual = 
                await pipelineBlock.ReceiveAllAsync();

            Assert.That(actual.Count, Is.EqualTo(3));
            Assert.That(actual.Select(t => t.Item1), Is.EquivalentTo(new [] { "value1", "value2", "value3" }));
            Assert.That(actual.Select(t => t.Item2), Is.EquivalentTo(new [] { 1, 2, 3 }));
        }

        [Test]
        public async Task MixedPipelineBlocks()
        {
            var builder = 
                new Builder();

            var pipelineBlock = 
                builder.Create(new BufferBlock<string>())
                       .Then(new TransformBlock<string, JObject>(json => JObject.Parse(json)))
                       .Then(new BatchBlock<JObject>(3))
                       .End();

            await Task.WhenAll(Enumerable.Repeat("{ }", 10)
                                         .Select(pipelineBlock.SendAsync));
            
            pipelineBlock.Complete();

            var actual = 
                await pipelineBlock.ReceiveAllAsync();

            Assert.That(actual.Count, Is.EqualTo(4));
            Assert.That(actual.Count(b => b.Length == 3), Is.EqualTo(3));
            Assert.That(actual.Count(b => b.Length == 1), Is.EqualTo(1));
        }
    }
}
