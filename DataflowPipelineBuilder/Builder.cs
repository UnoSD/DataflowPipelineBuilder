using System.Threading.Tasks.Dataflow;

namespace DataflowPipelineBuilder
{
    public class Builder
    {
        readonly BuilderOptions _options;

        public Builder() : this(new BuilderOptions()) { }

        public Builder(BuilderOptions options) => _options = options;

        public IBuilder<TSource, TTarget> Create<TSource, TTarget>(IPropagatorBlock<TSource, TTarget> target) =>
            new StartBuilder<TSource, TTarget>(target.WrapInLogger(_options), _options);

        public IBuilder<T, T> Create<T>() =>
            new StartBuilder<T, T>(new BufferBlock<T>().WrapInLogger(_options), _options);
    }
}