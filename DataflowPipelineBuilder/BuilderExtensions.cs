﻿using System;
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

        // This will buffer all the data and call complete
        // what happens if at this stage the other blocks
        // are not linked yet?
        // Change the test BuilderTest.Fork() to use this
        // and you will get only one output, fix the issue.
        // We could potentially keep the information in the
        // builder and once people call End() then we complete
        // the initial source.
        public static IBuilder<T, T> FromEnumerable<T>(this Builder builder, IEnumerable<T> source)
        {
            var buffer = new BufferBlock<T>();

            Task.Run(() => Task.WhenAll(source.Select(buffer.SendAsync))
                               .Then(() => buffer.Complete()));

            return builder.Create(buffer);
        }

        public static IBuilder<TOrigin, Tuple<TLOutput, TROutput>> 
        Fork<TOrigin, TO, T, TLOutput, TROutput>
        (
            this IBuilder<TOrigin, T> source,
            Func<IBuilder<TOrigin, T>, IBuilder<TO, TLOutput>> leftBranch,
            Func<IBuilder<TOrigin, T>, IBuilder<TO, TROutput>> rightBranch
        ) => source.Fork()
                   .Then(leftBranch, rightBranch);
    }
}
