
using System.Threading.Tasks.Dataflow;

namespace DataflowPipelineBuilder
{
    public class StartBuilder<TOrigin, TSource> : IBuilder<TOrigin, TSource>
    {
        readonly IPropagatorBlock<TOrigin, TSource> _start;

        internal StartBuilder(IPropagatorBlock<TOrigin, TSource> start) => _start = start;

        public IBuilder<TOrigin, TTarget> Then<TTarget>(IPropagatorBlock<TSource, TTarget> block)
        {
            _start.LinkTo(block, new DataflowLinkOptions { PropagateCompletion = true });

            return new MiddleBuilder<TOrigin, TTarget>(_start, block);
        }

        public EndBuilder Then(ITargetBlock<TSource> block)
        {
            _start.LinkTo(block, new DataflowLinkOptions { PropagateCompletion = true });
            return new EndBuilder(block);
        }

        public IForkBuilder<TOrigin, TSource> Fork() => 
            new ForkBuilder<TOrigin, TSource>(_start, _start);

        public IPropagatorBlock<TOrigin, TSource> End() => _start;
    }
}
