using System.Threading.Tasks.Dataflow;

namespace DataflowPipelineBuilder
{
    public class MiddleBuilder<TOrigin, TSource, TTarget> : IBuilder<TOrigin, TTarget>
    {
        readonly ITargetBlock<TOrigin> _start;
        readonly IPropagatorBlock<TSource, TTarget> _current;

        internal MiddleBuilder(ITargetBlock<TOrigin> start, IPropagatorBlock<TSource, TTarget> current)
        {
            _start = start;
            _current = current;
        }

        public IBuilder<TOrigin, TNewTarget> Then<TNewTarget>(IPropagatorBlock<TTarget, TNewTarget> block)
        {
            _current.LinkTo(block, new DataflowLinkOptions { PropagateCompletion = true });

            return new MiddleBuilder<TOrigin, TTarget, TNewTarget>(_start, block);
        }

        public IPropagatorBlock<TOrigin, TTarget> End() => 
            new WrapperBlock<TOrigin, TTarget>(_start, _current);
    }
}