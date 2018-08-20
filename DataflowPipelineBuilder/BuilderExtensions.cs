using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DataflowPipelineBuilder
{
    public static class BuilderExtensions
    {
        public static IBuilder<TOrigin, IReadOnlyCollection<T>> Batch<TOrigin, T>
        (
            this IBuilder<TOrigin, T> builder,
            int size
        ) => builder.Then(new BatchBlock<T>(size));

        public static IBuilder<TOrigin, T> SelectMany<TOrigin, T>
        (
            this IBuilder<TOrigin, IEnumerable<T>> builder
        ) => builder.Then(new TransformManyBlock<IEnumerable<T>, T>(enumerable => enumerable));

        public static IBuilder<TOrigin, TResult> Select<TOrigin, T, TResult>
        (
            this IBuilder<TOrigin, T> builder,
            Func<T, TResult> selector
        ) => builder.Then(new TransformBlock<T, TResult>(selector));

        public static IBuilder<T, T> FromEnumerable<T>(this Builder builder, IEnumerable<T> source)
        {
            var buffer = new BufferBlock<T>();

            Task.Run(() => Task.WhenAll(source.Select(buffer.SendAsync))
                               .Then(() => buffer.Complete()));

            return builder.Create(buffer);
        }
    }
}
