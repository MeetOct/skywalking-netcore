using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SkyWalking.Components;
using SkyWalking.Context;
using SkyWalking.Context.Trace;

namespace SkyWalking.Diagnostics.EntityFrameworkCore
{
    public class ConcurrentEfCoreSpanFactory : IConcurrentEfCoreSpanFactory
    {
        private readonly IEnumerable<IEfCoreSpanMetadataProvider> _spanMetadataProviders;

        public ConcurrentEfCoreSpanFactory(IEnumerable<IEfCoreSpanMetadataProvider> spanMetadataProviders)
        {
            _spanMetadataProviders = spanMetadataProviders;
        }

        public ISpan Create(string activityId,string parentId, string operationName, CommandEventData eventData)
        {
            foreach (var provider in _spanMetadataProviders)
                if (provider.Match(eventData.Command.Connection)) return CreateSpan(activityId, parentId, operationName, eventData, provider);

            return CreateDefaultSpan(activityId, parentId, operationName, eventData);
        }

        protected virtual ISpan CreateSpan(string activityId, string parentId, string operationName, CommandEventData eventData, IEfCoreSpanMetadataProvider metadataProvider)
        {
            var span = ConcurrentContextManager.CreateExitSpan(operationName, metadataProvider.GetPeer(eventData.Command.Connection), activityId, parentId);
            span.SetComponent(metadataProvider.Component);
            return span;
        }

        private ISpan CreateDefaultSpan(string activityId, string parentId, string operationName, CommandEventData eventData)
        {
            var span = ConcurrentContextManager.CreateLocalSpan(operationName, activityId, parentId);
            span.SetComponent(ComponentsDefine.EntityFrameworkCore);
            return span;
        }
    }
}