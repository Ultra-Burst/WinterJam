using UnityEngine;

namespace Fungus
{
    /// <summary>
    /// Shared guard used by project UI to stop a recycled submit press
    /// from advancing a Say command immediately after a menu closes.
    /// </summary>
    public static class DialogueAdvanceInputBlocker
    {
        private static float blockedUntilUnscaledTime;

        public static bool IsBlocked => Time.unscaledTime < blockedUntilUnscaledTime;

        public static void BlockForSeconds(float duration)
        {
            if (duration <= 0f)
                return;

            blockedUntilUnscaledTime = Mathf.Max(blockedUntilUnscaledTime, Time.unscaledTime + duration);
        }

        public static void Clear()
        {
            blockedUntilUnscaledTime = 0f;
        }
    }
}
