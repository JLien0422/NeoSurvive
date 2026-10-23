/// <summary>
/// 해킹 오브젝트 종류 (5종)
/// </summary>
public enum HackableObjectType
{
    SecurityTurret,    // 보안 터렛     → NumberSequence
    ElectricFence,     // 전기 울타리   → 휠 막대 맞추기
    SatelliteUplink,   // 새틀라이트    → NetworkBridge
    SynapseServer,     // 시냅스 서버   → SynapseSync
    MagneticBeacon     // 마그네틱 비컨 → FrequencyOverride
}

/// <summary>
/// 현재 플레이에 쓰는 미니게임 풀 4종.
/// 넘버 시퀀스, 네트워크 브릿지, 휠 막대 맞추기, 시냅스 동기화.
/// 주파수 오버라이드는 디자인 미완료로 제외.
/// </summary>
public static class ActiveHackMinigames
{
    public static readonly HackableObjectType[] Types =
    {
        HackableObjectType.SecurityTurret,
        HackableObjectType.SatelliteUplink,
        HackableObjectType.ElectricFence,
        HackableObjectType.SynapseServer
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
