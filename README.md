# DataflowPipelineBuilder
Build TPL Dataflow pipelines using a fluent API and helpful extensions

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
