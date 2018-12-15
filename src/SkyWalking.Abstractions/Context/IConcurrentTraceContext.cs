using SkyWalking.Context.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWalking.Context
{
    public interface IConcurrentTraceContext
    {
        /// <summary>
        /// Inject the context into the given carrier, only when the active span is an exit one.
        /// </summary>
        void Inject(IContextCarrier carrier, string activityId, string rootId);


        /// <summary>
        /// Extract the carrier to build the reference for the pre segment.
        /// </summary>
        void Extract(IContextCarrier carrier, string activityId);

        ISpan ActiveSpan(string activityId);

        void Continued(IContextSnapshot snapshot, string activityId);

        string GetReadableGlobalTraceId();

        IDictionary<string, object> Properties { get; }

        /// <summary>
        /// Create an entry span
        /// </summary>
        ISpan CreateEntrySpan(string operationName, string activityId, string parentId);

        /// <summary>
        /// Create a local span
        /// </summary>
        ISpan CreateLocalSpan(string operationName, string activityId,string parentId);

        /// <summary>
        /// Create an exit span
        /// </summary>
        ISpan CreateExitSpan(string operationName, string remotePeer, string activityId, string parentId);

        /// <summary>
        /// Stop the given span, if and only if this one is the top element of {@link #activeSpanStack}. Because the tracing
        /// core must make sure the span must match in a stack module, like any program did.
        /// </summary>
        void StopSpan(ISpan span, string activityId);

        /// <summary>
        /// Capture the snapshot of current context.
        /// </summary>
        IContextSnapshot Capture(string activityId, string rootId);
    }
}
