using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DataflowPipelineBuilder
{
    public class TimingInterceptorBlock<TInput, TOutput> : IPropagatorBlock<TInput, TOutput>
    {
        readonly IPropagatorBlock<TInput, TOutput> _wrapped;
        readonly Action<string> _beforeProcessing;
        readonly IPropagatorBlock<TOutput, TOutput> _output;
        readonly ConcurrentDictionary<long, Stopwatch> _times;
        long _messageId;

        // Workaround for not being able to implement (even explicitly)
        // both IPropagatorBlock<TInput, TOutput> and IPropagatorBlock<TOutput, TOutput>
        // on TimingBlock
        class TimingBlockInternal : IPropagatorBlock<TOutput, TOutput>
        {
            readonly ConcurrentDictionary<long, Stopwatch> _times;
            readonly Action<string, TimeSpan> _afterProcessing;
            readonly IPropagatorBlock<TOutput, TOutput> _output;

            public TimingBlockInternal
            (
                ConcurrentDictionary<long, Stopwatch> times,
                Action<string, TimeSpan> afterProcessing
            )
            {
                _times = times;
                _afterProcessing = afterProcessing;
                _output = new TransformBlock<TOutput, TOutput>(i => i,
                                                               new ExecutionDataflowBlockOptions
                                                               {
                                                                   NameFormat = "TimingBlockInternal {1}"
                                                               });
            }

            DataflowMessageStatus ITargetBlock<TOutput>.OfferMessage(DataflowMessageHeader messageHeader, TOutput messageValue, ISourceBlock<TOutput> source, bool consumeToAccept)
            {
                if (!_times.TryRemove(messageHeader.Id, out var stopwatch))
                    throw new NotImplementedException();

                _afterProcessing(source.ToString(), stopwatch.Elapsed);

                return _output.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
            }

            void IDataflowBlock.Complete() =>
                _output.Complete();

            void IDataflowBlock.Fault(Exception exception) =>
                _output.Fault(exception);

            Task IDataflowBlock.Completion =>
                _output.Completion;

            TOutput ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed) =>
                _output.ConsumeMessage(messageHeader, target, out messageConsumed);

            IDisposable ISourceBlock<TOutput>.LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions) =>
                _output.LinkTo(target, linkOptions);

            void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target) =>
                _output.ReleaseReservation(messageHeader, target);

            bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target) =>
                _output.ReserveMessage(messageHeader, target);

            public override string ToString() =>
                _output.ToString();
        }

        public TimingInterceptorBlock
        (
            IPropagatorBlock<TInput, TOutput> wrapped,
            Action<string> beforeProcessing,
            Action<string, TimeSpan> afterProcessing
        )
        {
            _wrapped = wrapped;
            _beforeProcessing = beforeProcessing;
            _times = new ConcurrentDictionary<long, Stopwatch>();
            _output = new TimingBlockInternal(_times, afterProcessing);
            _wrapped.LinkTo(_output, new DataflowLinkOptions { PropagateCompletion = true });
        }

        DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput> source, bool consumeToAccept)
        {
            // We cannot use messageHeader.Id (into a Dictionary) as it's not unique
            // multiple messages pushed in before they get out will have the same Id.
            // TODO: Find a way to avoid the nasty lock here but still identify uniquely a message.
            var newMessageHeader = new DataflowMessageHeader(Interlocked.Increment(ref _messageId));

            _times.AddOrUpdate(newMessageHeader.Id,
                               _ => Stopwatch.StartNew(),
                               (_, __) => throw new NotImplementedException());

            _beforeProcessing(_wrapped.ToString());

            return _wrapped.OfferMessage(newMessageHeader, messageValue, source, consumeToAccept);
        }

        void IDataflowBlock.Complete() =>
            _wrapped.Complete();

        void IDataflowBlock.Fault(Exception exception) =>
            _wrapped.Fault(exception);

        public Task Completion =>
            _output.Completion;

        TOutput ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed) =>
            _output.ConsumeMessage(messageHeader, target, out messageConsumed);

        IDisposable ISourceBlock<TOutput>.LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions) =>
            _output.LinkTo(target, linkOptions);

        void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target) =>
            _wrapped.ReleaseReservation(messageHeader, target);

        bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target) =>
            _output.ReserveMessage(messageHeader, target);

        public override string ToString() =>
            $"TimingInterceptor for: {_wrapped}";
    }
}