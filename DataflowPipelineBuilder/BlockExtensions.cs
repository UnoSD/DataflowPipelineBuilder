using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DataflowPipelineBuilder
{
    public static class BlockExtensions
    {
        public static async Task<IReadOnlyCollection<T>> ReceiveAllAsync<T>(this ISourceBlock<T> source)
        {
            var batch =
                new BatchBlock<T>(int.MaxValue);

            source.LinkTo(batch, new DataflowLinkOptions { PropagateCompletion = true });

            return await batch.ReceiveAsync().ConfigureAwait(false);
        }

        public static IPropagatorBlock<TInput, TOutput> WrapInLogger<TInput, TOutput>
        (
            this IPropagatorBlock<TInput, TOutput> source,
            BuilderOptions options
        ) => (options.InLogger as object ?? options.OutLogger) == null ?
             source :
             new TimingInterceptorBlock<TInput, TOutput>(source,
                                                         options.InLogger ?? (_ => { }), 
                                                         options.OutLogger ?? ((_, __) => { }));
    }
}
