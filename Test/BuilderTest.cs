using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
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
                builder.Create<string>()
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
            Assert.That(actual.Select(t => t.Item1), Is.EquivalentTo(new[] { "value1", "value2", "value3" }));
            Assert.That(actual.Select(t => t.Item2), Is.EquivalentTo(new[] { 1, 2, 3 }));
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

        [Test]
        public async Task PipelineLogging()
        {
            var logs = new List<string>();

            var builder =
                new Builder(new BuilderOptions
                {
                    InLogger = blockName => Debug.WriteLine($"IN,{blockName}"),
                    OutLogger = (blockName, elapsed) => Debug.WriteLine($"OUT,{blockName},{elapsed}")
                });

            var pipelineBlock =
                builder.Create<string>()
                       .Select(JObject.Parse)
                       .Batch(3)
                       .SelectMany()
                       .End();

            await Task.WhenAll(Enumerable.Repeat("{ }", 2)
                                         .Select(pipelineBlock.SendAsync))
                      .Then(() => pipelineBlock.Complete());

            var _ = await pipelineBlock.ReceiveAllAsync();

            await pipelineBlock.Completion;
        }

        [Test]
        [Category("Work in progress")]
        public async Task InterceptManyToOne()
        {
            // if [ 1, 2, 3 ] and [ 4, 5 ] as input
            // output is "1, 2, 3, 4, 5" how do you know
            // which output came from which input on
            // the TransformManyBlock?
            // Logic should be: elapsed 5 = time from
            // [ 4, 5 ] arrived to time 5 was popped
            // how do you match it without knowing
            // when one collection finished and another
            // started?

            var many =
                new TransformManyBlock<IEnumerable<int>, int>
                (
                     ints => ints,
                     new ExecutionDataflowBlockOptions
                     {
                         NameFormat = "MyTransform"
                     }
                );

            var interceptor = 
                new TimingInterceptorBlock<IEnumerable<int>, int>
                (
                     many,
                     s => { }, // Should be called once
                     (s, span) => { } // Should be called twice
                );

            await interceptor.SendAsync(new[] { 11, 22 });
            await interceptor.ReceiveAllAsync();
            await interceptor.Completion;
        }

        [Test]
        [Category("Work in progress")]
        public async Task InterceptOneToMany()
        {
            var batch =
                new BatchBlock<int>
                (
                     2,
                     new GroupingDataflowBlockOptions
                     {
                         NameFormat = "MyTransform"
                     }
                );

            var interceptor = 
                new TimingInterceptorBlock<int, int[]>
                (
                     batch,
                     s => { }, // Should be called twice
                     (s, span) => { } // Should be called once
                );

            await interceptor.SendAsync(11);
            await interceptor.SendAsync(22);
            await interceptor.ReceiveAllAsync();
            await interceptor.Completion;
        }

        [Test]
        [Category("Time based")]
        public static async Task LoggingBlock()
        {
            var messages = new List<string>();

            var blockToLog = new TransformBlock<string, int>(async message =>
            {
                messages.Add("Message got into the block");

                await Task.Delay(3000);

                messages.Add("Message getting out of the block");

                return message.Length;
            }, new ExecutionDataflowBlockOptions { NameFormat = "MyBlock" });

            var logInput = new ActionBlock<string>(blockName =>
                messages.Add($"Message getting into {blockName}"));

            var logOutput = new ActionBlock<(string target, TimeSpan elapsed)>(tuple =>
                messages.Add($"Message (from x to {tuple.target}) took {tuple.elapsed} to process"));

            var interceptor =
                new TimingInterceptorBlock<string, int>(blockToLog,
                                                        item => logInput.Post(item),
                                                        (target, elapsed) => logOutput.Post((target, elapsed)));

            await interceptor.SendAsync("pippo");

            var output = await interceptor.ReceiveAsync();

            Assert.That(output, Is.EqualTo(5));
            Assert.That(messages[0], Is.EqualTo("Message getting into MyBlock"));
            Assert.That(messages[1], Is.EqualTo("Message got into the block"));
            Assert.That(messages[2], Is.EqualTo("Message getting out of the block"));
            Assert.That(messages[3], Does.Match(@"Message \(from x to MyBlock\) took [0-9:\.]* to process"));

            var timeString =
                Regex.Match(messages[3], @"Message \(from x to MyBlock\) took ([0-9:\.]*) to process")
                     .Groups
                     .Last()
                     .Value;

            var time = TimeSpan.Parse(timeString);

            Assert.That(time, Is.EqualTo(TimeSpan.FromSeconds(3))
                                .Within(TimeSpan.FromSeconds(1)));
        }
    }
}
