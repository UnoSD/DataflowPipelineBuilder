using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DataflowPipelineBuilder
{
    class PropagatorBlockWrapper<TSource, TTarget> : IPropagatorBlock<TSource, TTarget>
    {
        readonly ISourceBlock<TTarget> _output;
        readonly ITargetBlock<TSource> _input;

        public PropagatorBlockWrapper(ITargetBlock<TSource> input, ISourceBlock<TTarget> output)
        {
            _output = output;
            _input = input;
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TSource messageValue, ISourceBlock<TSource> source, bool consumeToAccept) =>
            _input.OfferMessage(messageHeader, messageValue, source, consumeToAccept);

        public void Complete() => _input.Complete();

        public void Fault(Exception exception) => _input.Fault(exception);

        public Task Completion => _output.Completion;

        public IDisposable LinkTo(ITargetBlock<TTarget> target, DataflowLinkOptions linkOptions) =>
            _output.LinkTo(target, linkOptions);

        public TTarget ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TTarget> target, out bool messageConsumed) =>
            _output.ConsumeMessage(messageHeader, target, out messageConsumed);

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TTarget> target) =>
            _output.ReserveMessage(messageHeader, target);

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TTarget> target) =>
            _output.ReleaseReservation(messageHeader, target);
    }
}