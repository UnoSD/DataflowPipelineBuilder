using System;
using System.Threading.Tasks.Dataflow;

namespace DataflowPipelineBuilder
{
    public class ForkBuilder<TOrigin, TTarget> : IForkBuilder<TOrigin, TTarget>
    {
        readonly ITargetBlock<TOrigin> _start;
        readonly ISourceBlock<TTarget> _current;

        public ForkBuilder(ITargetBlock<TOrigin> start, ISourceBlock<TTarget> current)
        {
            _start = start;
            _current = current;
        }

        public IBuilder<TOrigin, Tuple<TLOutput, TROutput>>
        Then<TO, TLOutput, TROutput>
        (
            Func<IBuilder<TOrigin, TTarget>, IBuilder<TO, TLOutput>> leftBranch,
            Func<IBuilder<TOrigin, TTarget>, IBuilder<TO, TROutput>> rightBranch
        )
        {
            var broadcastBlock = new BroadcastBlock<TTarget>(i => i);

            _current.LinkTo(broadcastBlock, new DataflowLinkOptions { PropagateCompletion = true });

            var broadcastBuilder = new MiddleBuilder<TOrigin, TTarget>(_start, broadcastBlock);

            var left = leftBranch(broadcastBuilder);
            var right = rightBranch(broadcastBuilder);

            var join = new JoinBlock<TLOutput, TROutput>();

            left.End().LinkTo(join.Target1, new DataflowLinkOptions { PropagateCompletion = true });
            right.End().LinkTo(join.Target2, new DataflowLinkOptions { PropagateCompletion = true });

            return new MiddleBuilder<TOrigin, Tuple<TLOutput, TROutput>>(_start, join);
        }
    }
}