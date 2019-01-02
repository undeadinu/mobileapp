using System;

namespace Toggl.Foundation.Sync
{
    public interface ILeakyBucket
    {
        bool TryClaimFreeSlot(out TimeSpan timeToFreeSlot);
        bool TryClaimFreeSlots(int numberOfSlots, out TimeSpan timeToFreeSlot);
    }
}
