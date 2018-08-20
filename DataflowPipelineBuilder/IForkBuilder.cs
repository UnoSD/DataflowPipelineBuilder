using System;

namespace DataflowPipelineBuilder
{
    public interface IForkBuilder<in TOrigin, out T>
    {
        IBuilder<TOrigin, Tuple<TLOutput, TROutput>> Then<TO, TLOutput, TROutput>
        (
            Func<IBuilder<TOrigin, T>, IBuilder<TO, TLOutput>> leftBranch,
            Func<IBuilder<TOrigin, T>, IBuilder<TO, TROutput>> rightBranch
        );
    }
}