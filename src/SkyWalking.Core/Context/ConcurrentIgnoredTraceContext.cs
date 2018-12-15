using SkyWalking.Context.Trace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SkyWalking.Context
{
    public class ConcurrentIgnoredTraceContext : IConcurrentTraceContext
    {
        private static readonly NoopSpan noopSpan = new NoopSpan();
        private static readonly NoopEntrySpan noopEntrySpan = new NoopEntrySpan();

        private readonly ConcurrentDictionary<string, ISpan>  _spans = new ConcurrentDictionary<string,ISpan>();

        public ISpan ActiveSpan(string activityId)
        {
            _spans.TryGetValue(activityId, out var span);
            return span;
        }

        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        public IContextSnapshot Capture(string activityId, string rootId)
        {
            return null;
        }

        public void Continued(IContextSnapshot snapshot, string activityId)
        {

        }

        public ISpan CreateEntrySpan(string operationName, string activityId, string rootId)
        {
            _spans.TryAdd(activityId,noopEntrySpan);
            return noopEntrySpan;
        }

        public ISpan CreateExitSpan(string operationName, string remotePeer, string activityId, string rootId)
        {
            var exitSpan = new NoopExitSpan(remotePeer);
            _spans.TryAdd(activityId, exitSpan);
            return exitSpan;
        }

        public ISpan CreateLocalSpan(string operationName, string activityId, string rootId)
        {
            _spans.TryAdd(activityId, noopSpan);
            return noopSpan;
        }

        public void Extract(IContextCarrier carrier, string activityId)
        {
        }

        public string GetReadableGlobalTraceId()
        {
            return string.Empty;
        }

        public void Inject(IContextCarrier carrier, string activityId, string rootId)
        {
        }

        public void StopSpan(ISpan span, string activityId)
        {
            _spans.TryRemove(activityId,out _);
            if (_spans.Count == 0)
            {
                ListenerManager.NotifyFinish(this);
                foreach (var item in Properties)
                {
                    if (item.Value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        public static class ListenerManager
        {
            private static readonly List<IIgnoreTracerContextListener> _listeners = new List<IIgnoreTracerContextListener>();

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void Add(IIgnoreTracerContextListener listener)
            {
                _listeners.Add(listener);
            }

            public static void NotifyFinish(IConcurrentTraceContext tracerContext)
            {
                foreach (var listener in _listeners)
                {
                    listener.AfterFinish(tracerContext);
                }
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void Remove(IIgnoreTracerContextListener listener)
            {
                _listeners.Remove(listener);
            }
        }
    }
}
