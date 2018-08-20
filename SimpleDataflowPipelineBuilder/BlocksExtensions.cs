using System;
using System.Threading.Tasks.Dataflow;

namespace SimpleDataflowPipelineBuilder
{
    public static class BlocksExtensions
    {
        static readonly DataflowLinkOptions LinkOptions =
            new DataflowLinkOptions { PropagateCompletion = true };

        public static ISourceBlock<TOut> Then<TIn, TOut>
        (
            this ISourceBlock<TIn> source,
            IPropagatorBlock<TIn, TOut> target
        )
        {
            source.LinkTo(target, LinkOptions);

            return target;
        }

        public static IDataflowBlock Then<TIn>
        (
            this ISourceBlock<TIn> source,
            ITargetBlock<TIn> target
        )
        {
            source.LinkTo(target, LinkOptions);

            return target;
        }

        public static ISourceBlock<Tuple<TOut, TOut>> ForkJoinThen<TIn, TOut>
        (
            this ISourceBlock<TIn> source,
            Func<ISourceBlock<TIn>, ISourceBlock<TOut>> left,
            Func<ISourceBlock<TIn>, ISourceBlock<TOut>> right
        )
        {
            var fork =
                new BroadcastBlock<TIn>(data => data,
                                               new DataflowBlockOptions
                                               {
                                                   NameFormat = "Fork {1}"
                                               });

            source.LinkTo(fork, LinkOptions);

            var join = new JoinBlock<TOut, TOut>();

            left(fork).LinkTo(join.Target1, LinkOptions);

            right(fork).LinkTo(join.Target2, LinkOptions);

            return join;
        }
    }
}
