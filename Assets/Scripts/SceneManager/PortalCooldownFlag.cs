// PortalCooldownFlag.cs  (put on Player)
using UnityEngine;

public class PortalCooldownFlag : MonoBehaviour
{
    public static float BlockUntilTime { get; private set; }

    [Tooltip("Seconds to block portals right after a teleport.")]
    public float cooldown = 0.3f;

    public void MarkTeleportedNow()
    {
        BlockUntilTime = Time.unscaledTime + cooldown;
    }

    public static bool IsBlockedNow => Time.unscaledTime < BlockUntilTime;
}
