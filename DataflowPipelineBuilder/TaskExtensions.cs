using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataflowPipelineBuilder
{
    static class TaskExtensions
    {
        internal static async Task Then(this Task task, Action action)
        {
            await task.ConfigureAwait(false);

            action();
        }

        internal static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks) =>
            Task.WhenAll(tasks);
    }
}
