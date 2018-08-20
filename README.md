# DataflowPipelineBuilder
Build TPL Dataflow pipelines using a fluent API and helpful extensions

# Rationale
TPL Dataflow is a powerful library, but the code is far less readable without the help of a diagram, this library should help visualize the pipeline in code, too. See the last example for the pipeline in code that reseable a diagram.

# NuGet
https://www.nuget.org/packages/DataflowPipelineBuilder

# Using LINQ-like extension syntax

```csharp
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
```
# Using blocks

```csharp
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
```
# Fork and join

```csharp
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
Assert.That(actual.Select(t => t.Item1), Is.EquivalentTo(new [] { "value1", "value2", "value3" }));
Assert.That(actual.Select(t => t.Item2), Is.EquivalentTo(new [] { 1, 2, 3 }));
```
# Simple extensions

Alternativeli, the project `SimpleDataflowPipelineBuilder` contains extension methods applying to the dataflow blocks directly without the help of a builder (this doesn't support added logging and custom options applied to all links, but it's simple and works well in most cases).

```csharp
var input = BufferBlock(token);

var output = 
    input.Then(ParseDataBlock())
         .ForkJoinThen(source => source.Then(BatchBlock())
                                       .Then(ApiCallToExternalResourceBlock())
                                       .Then(SelectManyBlock()),
                       source => source.Then(ApiCallToExternalResourceThatDoesNotAcceptBatches()))
         .Then(JoinResultsBlock())
         .Then(WriteToStorageBlock());
```
