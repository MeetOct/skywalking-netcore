using SkyWalking.Context.Trace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SkyWalking.Context
{
    public class ConcurrentTraceContext: IConcurrentTraceContext
    {
        private long _lastWarningTimestamp = 0;
        private readonly ISampler _sampler;
        private readonly ITraceSegment _segment;
        private readonly ConcurrentDictionary<string,ISpan> _activeSpanDic;
        private int _spanIdGenerator;
        public ConcurrentTraceContext()
        {
            _sampler = DefaultSampler.Instance;
            _segment = new TraceSegment();
            _activeSpanDic = new ConcurrentDictionary<string,ISpan>();
        }

        /// <summary>
        /// Inject the context into the given carrier, only when the active span is an exit one.
        /// </summary>
        public void Inject(IContextCarrier carrier,string activityId, string rootId)
        {
            var span = InternalActiveSpan(activityId);
            if (!span.IsExit)
            {
                throw new InvalidOperationException("Inject can be done only in Exit Span");
            }

            var spanWithPeer = span as IWithPeerInfo;
            var peer = spanWithPeer.Peer;
            var peerId = spanWithPeer.PeerId;

            carrier.TraceSegmentId = _segment.TraceSegmentId;
            carrier.SpanId = span.SpanId;
            carrier.ParentApplicationInstanceId = _segment.ApplicationInstanceId;

            if (peerId == 0)
            {
                carrier.PeerHost = peer;
            }
            else
            {
                carrier.PeerId = peerId;
            }

            var refs = _segment.Refs;

            var metaValue = GetMetaValue(refs, rootId);

            carrier.EntryApplicationInstanceId = metaValue.entryApplicationInstanceId;

            if (metaValue.operationId == 0)
            {
                carrier.EntryOperationName = metaValue.operationName;
            }
            else
            {
                carrier.EntryOperationId = metaValue.operationId;
            }

            var parentOperationId = RootSpan(rootId).OperationId;
            if (parentOperationId == 0)
            {
                carrier.ParentOperationName = RootSpan(rootId).OperationName;
            }
            else
            {
                carrier.ParentOperationId = parentOperationId;
            }

            carrier.SetDistributedTraceIds(_segment.RelatedGlobalTraces);
        }


        /// <summary>
        /// Extract the carrier to build the reference for the pre segment.
        /// </summary>
        public void Extract(IContextCarrier carrier, string activityId)
        {
            var traceSegmentRef = new TraceSegmentRef(carrier);
            _segment.Ref(traceSegmentRef);
            _segment.RelatedGlobalTrace(carrier.DistributedTraceId);
            var span = InternalActiveSpan(activityId);
            if (span is EntrySpan)
            {
                span.Ref(traceSegmentRef);
            }
        }

        public ISpan ActiveSpan(string activityId) => InternalActiveSpan(activityId);

        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        public void Continued(IContextSnapshot snapshot, string activityId)
        {
            var segmentRef = new TraceSegmentRef(snapshot);
            _segment.Ref(segmentRef);
            InternalActiveSpan(activityId).Ref(segmentRef);
            _segment.RelatedGlobalTrace(snapshot.DistributedTraceId);
        }

        public string GetReadableGlobalTraceId()
        {
            return _segment.RelatedGlobalTraces.FirstOrDefault()?.ToString();
        }

        /// <summary>
        /// Create an entry span
        /// </summary>
        public ISpan CreateEntrySpan(string operationName, string activityId, string parentId)
        {
            if (!EnsureLimitMechanismWorking(activityId,out var noopSpan))
            {
                return noopSpan;
            }

            var parentSpanId = -1;
            if (!string.IsNullOrWhiteSpace(parentId))
            {
                _activeSpanDic.TryGetValue(activityId, out var parentSpan);
                parentSpanId = parentSpan?.SpanId ?? -1;
            }

            var entrySpan = new EntrySpan(_spanIdGenerator++, parentSpanId, operationName);
            entrySpan.Start();
            TryAddSpan(activityId,entrySpan);

            return entrySpan;
        }

        /// <summary>
        /// Create a local span
        /// </summary>
        public ISpan CreateLocalSpan(string operationName, string activityId, string parentId)
        {
            if (!EnsureLimitMechanismWorking(activityId,out var noopSpan))
            {
                return noopSpan;
            }

            var parentSpanId = -1;
            if (!string.IsNullOrWhiteSpace(parentId))
            {
                _activeSpanDic.TryGetValue(activityId, out var parentSpan);
                parentSpanId = parentSpan?.SpanId??-1;
            }

            var span = new LocalSpan(_spanIdGenerator++, parentSpanId, operationName);
            span.Start();
            TryAddSpan(activityId, span);

            return span;
        }

        /// <summary>
        /// Create an exit span
        /// </summary>
        public ISpan CreateExitSpan(string operationName, string remotePeer, string activityId, string parentId)
        {
            var parentSpanId = -1;
            if (!string.IsNullOrWhiteSpace(parentId))
            {
                _activeSpanDic.TryGetValue(activityId, out var parentSpan);
                parentSpanId = parentSpan?.SpanId ?? -1;
            }

            var exitSpan = IsLimitMechanismWorking() ? (ISpan)new NoopExitSpan(remotePeer) : new ExitSpan(_spanIdGenerator++, parentSpanId, operationName, remotePeer);
            TryAddSpan(activityId, exitSpan);

            return exitSpan.Start();
        }

        /// <summary>
        /// Stop the given span, if and only if this one is the top element of {@link #activeSpanStack}. Because the tracing
        /// core must make sure the span must match in a stack module, like any program did.
        /// </summary>
        public void StopSpan(ISpan span, string activityId)
        {
            _activeSpanDic.TryGetValue(activityId,out var lastSpan);
            if (lastSpan == span)
            {
                if (lastSpan is AbstractTracingSpan tracingSpan)
                {
                    if (tracingSpan.Finish(_segment))
                    {
                        TryRemoveSpan(activityId);
                    }
                }
                else
                {
                    TryRemoveSpan(activityId);
                }
            }
            else
            {
                throw new InvalidOperationException("Stopping the unexpected span = " + span);
            }

            if (_activeSpanDic.Count == 0)
            {
                Finish();
            }
        }

        /// <summary>
        /// Capture the snapshot of current context.
        /// </summary>
        public IContextSnapshot Capture(string activityId,string rootId) => InternalCapture(activityId, rootId);

        private ISpan RootSpan(string rootId)
        {
            if (!_activeSpanDic.TryGetValue(rootId, out var span))
            {
                throw new InvalidOperationException($"No root span. RootId:{rootId}");
            }
            return span;
        }
        private ISpan InternalActiveSpan(string activityId)
        {
            if (!_activeSpanDic.TryGetValue(activityId,out var span))
            {
                throw new InvalidOperationException($"No active span. ActivityId:{activityId}");
            }
            return span;
        }

        private (string operationName, int operationId, int entryApplicationInstanceId) GetMetaValue(IEnumerable<ITraceSegmentRef> refs,string rootId)
        {
            if (refs != null && refs.Any())
            {
                var segmentRef = refs.First();
                return (segmentRef.EntryOperationName, segmentRef.EntryOperationId,
                    segmentRef.EntryApplicationInstanceId);
            }
            else
            {
                return (RootSpan(rootId).OperationName, RootSpan(rootId).OperationId, _segment.ApplicationInstanceId);
            }
        }

        private IContextSnapshot InternalCapture(string activityId,string rootId)
        {
            var refs = _segment.Refs;

            var snapshot =
                new ContextSnapshot(_segment.TraceSegmentId, InternalActiveSpan(activityId).SpanId, _segment.RelatedGlobalTraces);

            var metaValue = GetMetaValue(refs, rootId);

            snapshot.EntryApplicationInstanceId = metaValue.entryApplicationInstanceId;

            if (metaValue.operationId == 0)
            {
                snapshot.EntryOperationName = metaValue.operationName;
            }
            else
            {
                snapshot.EntryOperationId = metaValue.operationId;
            }

            if (RootSpan(rootId).OperationId == 0)
            {
                snapshot.ParentOperationName = RootSpan(rootId).OperationName;
            }
            else
            {
                snapshot.ParentOperationId = RootSpan(rootId).OperationId;
            }

            return snapshot;
        }

        private bool EnsureLimitMechanismWorking(string activityId,out ISpan noopSpan)
        {
            if (IsLimitMechanismWorking())
            {
                var span = new NoopSpan();
                TryAddSpan(activityId,span);
                noopSpan = span;
                return false;
            }

            noopSpan = null;

            return true;
        }

        private bool IsLimitMechanismWorking()
        {
            if (_spanIdGenerator < 300)
            {
                return false;
            }
            var currentTimeMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (currentTimeMillis - _lastWarningTimestamp > 30 * 1000)
            {
                //todo log warning
                _lastWarningTimestamp = currentTimeMillis;
            }
            return true;
        }

        private bool TryAddSpan(string activityId,ISpan span)
        {
            return _activeSpanDic.TryAdd(activityId, span);
        }

        private ISpan TryRemoveSpan(string activityId)
        {
            _activeSpanDic.TryRemove(activityId, out var removeSpan);
            return removeSpan;
        }

        private void Finish()
        {
            var finishedSegment = _segment.Finish(IsLimitMechanismWorking());

            if (!_segment.HasRef && _segment.IsSingleSpanSegment)
            {
                if (!_sampler.Sampled())
                {
                    finishedSegment.IsIgnore = true;
                }
            }

            ListenerManager.NotifyFinish(finishedSegment);

            foreach (var item in Properties)
            {
                if (item.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        public static class ListenerManager
        {
            private static readonly IList<ITracingContextListener> _listeners = new List<ITracingContextListener>();


            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void Add(ITracingContextListener listener)
            {
                _listeners.Add(listener);
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void Remove(ITracingContextListener listener)
            {
                _listeners.Remove(listener);
            }

            public static void NotifyFinish(ITraceSegment traceSegment)
            {
                foreach (var listener in _listeners)
                {
                    listener.AfterFinished(traceSegment);
                }
            }
        }
    }
}
