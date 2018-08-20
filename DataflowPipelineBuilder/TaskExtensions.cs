using System;
using System.Threading.Tasks;

namespace DataflowPipelineBuilder
{
    static class TaskExtensions
    {
        public static async Task Then(this Task task, Action action)
        {
            await task.ConfigureAwait(false);

            action();
        }
    }
}
