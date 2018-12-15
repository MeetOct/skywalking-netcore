using System.Diagnostics;

namespace SkyWalking.Utils
{
    public static class ActivityExtension
    {
        public static string FormatRootId(this Activity activity)
        {
            return $"|{activity.RootId}.";
        }
    }
}