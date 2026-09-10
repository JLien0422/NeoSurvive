/// <summary>
/// 해킹 오브젝트 종류 (5종)
/// </summary>
public enum HackableObjectType
{
    SecurityTurret,    // 보안 터렛     → NumberSequence
    ElectricFence,     // 전기 울타리   → CommandBypass
    SatelliteUplink,   // 새틀라이트    → NetworkBridge
    SynapseServer,     // 시냅스 서버   → SynapseSync
    MagneticBeacon     // 마그네틱 비컨 → FrequencyOverride
}

/// <summary>
/// 현재 플레이에 쓰는 미니게임 풀. 지금은 NumberSequence / CommandBypass 2종만.
/// </summary>
public static class ActiveHackMinigames
{
    public static readonly HackableObjectType[] Types =
    {
        HackableObjectType.SecurityTurret,
        HackableObjectType.ElectricFence
    };

    public static bool IsActive(HackableObjectType type)
    {
        for (int i = 0; i < Types.Length; i++)
        {
            if (Types[i] == type)
                return true;
        }

        return false;
    }

    public static HackableObjectType GetRandom()
    {
        return Types[UnityEngine.Random.Range(0, Types.Length)];
    }
}
