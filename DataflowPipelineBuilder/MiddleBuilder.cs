using System.Threading.Tasks.Dataflow;

namespace DataflowPipelineBuilder
{
    public class MiddleBuilder<TOrigin, TTarget> : IBuilder<TOrigin, TTarget>
    {
        readonly ITargetBlock<TOrigin> _start;
        readonly ISourceBlock<TTarget> _current;
        readonly BuilderOptions _options;

        internal MiddleBuilder
        (
            ITargetBlock<TOrigin> start,
            ISourceBlock<TTarget> current, 
            BuilderOptions options
        )
        {
            _start = start;
            _current = current;
            _options = options;
        }

        public IBuilder<TOrigin, TNewTarget> Then<TNewTarget>(IPropagatorBlock<TTarget, TNewTarget> block)
        {
            var wrappedBlock = block.WrapInLogger(_options);

            _current.LinkTo(wrappedBlock, new DataflowLinkOptions { PropagateCompletion = true });

            return new MiddleBuilder<TOrigin, TNewTarget>(_start, wrappedBlock, _options);
        }

        public IForkBuilder<TOrigin, TTarget> Fork() => new ForkBuilder<TOrigin, TTarget>(_start, _current, _options);

        public IPropagatorBlock<TOrigin, TTarget> End() => 
            new WrapperBlock<TOrigin, TTarget>(_start, _current);
    }
}