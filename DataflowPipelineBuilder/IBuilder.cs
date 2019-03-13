using System.Threading.Tasks.Dataflow;

namespace DataflowPipelineBuilder
{
    public interface IBuilder<in TOrigin, out T>
    {
        IBuilder<TOrigin, TTarget> Then<TTarget>(IPropagatorBlock<T, TTarget> block);

        EndBuilder Then(ITargetBlock<T> block);

        IForkBuilder<TOrigin, T> Fork();

        IPropagatorBlock<TOrigin, T> End();
    }

        public class EndBuilder
        {
        private readonly IDataflowBlock _block;

        public EndBuilder(IDataflowBlock block)
        {
            _block = block;
        }

        public IDataflowBlock End()
        {
            return _block;
        }
    }
}