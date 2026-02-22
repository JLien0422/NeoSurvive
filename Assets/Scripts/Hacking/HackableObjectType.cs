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
