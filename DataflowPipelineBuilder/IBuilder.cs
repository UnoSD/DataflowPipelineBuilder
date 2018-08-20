using System.Threading.Tasks.Dataflow;

namespace DataflowPipelineBuilder
{
    public interface IBuilder<in TOrigin, out T>
    {
        IBuilder<TOrigin, TTarget> Then<TTarget>(IPropagatorBlock<T, TTarget> block);

        IForkBuilder<TOrigin, T> Fork();

        IPropagatorBlock<TOrigin, T> End();
    }
}