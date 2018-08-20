using System.Threading.Tasks.Dataflow;

namespace DataflowPipelineBuilder
{
    public interface IBuilder<in TOrigin, out T>
    {
        IBuilder<TOrigin, TTarget> Then<TTarget>(IPropagatorBlock<T, TTarget> batchBlock);

        IPropagatorBlock<TOrigin, T> End();
    }
}