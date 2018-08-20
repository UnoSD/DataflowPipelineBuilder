using System.Threading.Tasks.Dataflow;

namespace DataflowPipelineBuilder
{
    public class Builder
    {
        public IBuilder<TSource, TTarget> Create<TSource, TTarget>(IPropagatorBlock<TSource, TTarget> target) =>
            new StartBuilder<TSource, TTarget>(target);

        public IBuilder<T, T> Create<T>() =>
            new StartBuilder<T, T>(new BufferBlock<T>());
    }
}