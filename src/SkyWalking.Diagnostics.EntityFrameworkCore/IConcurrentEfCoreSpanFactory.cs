using Microsoft.EntityFrameworkCore.Diagnostics;
using SkyWalking.Context.Trace;

namespace SkyWalking.Diagnostics.EntityFrameworkCore
{
    public interface IConcurrentEfCoreSpanFactory
    {
        ISpan Create(string activityId, string parentId, string operationName, CommandEventData eventData);
    }
}