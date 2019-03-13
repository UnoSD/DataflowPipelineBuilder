using System.Threading.Tasks.Dataflow;

namespace DataflowPipelineBuilder
{
    public class MiddleBuilder<TOrigin, TTarget> : IBuilder<TOrigin, TTarget>
    {
        readonly ITargetBlock<TOrigin> _start;
        readonly ISourceBlock<TTarget> _current;

        internal MiddleBuilder(ITargetBlock<TOrigin> start, ISourceBlock<TTarget> current)
        {
            _start = start;
            _current = current;
        }

        public IBuilder<TOrigin, TNewTarget> Then<TNewTarget>(IPropagatorBlock<TTarget, TNewTarget> block)
        {
            _current.LinkTo(block, new DataflowLinkOptions { PropagateCompletion = true });

            return new MiddleBuilder<TOrigin, TNewTarget>(_start, block);
        }

        public EndBuilder Then(ITargetBlock<TTarget> block)
        {
            _current.LinkTo(block, new DataflowLinkOptions { PropagateCompletion = true });
            return new EndBuilder(block);
        }

        public IForkBuilder<TOrigin, TTarget> Fork() => new ForkBuilder<TOrigin, TTarget>(_start, _current);

        public IPropagatorBlock<TOrigin, TTarget> End() => 
            new PropagatorBlockWrapper<TOrigin, TTarget>(_start, _current);
    }
}