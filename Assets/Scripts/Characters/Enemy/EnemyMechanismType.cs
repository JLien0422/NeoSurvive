/// <summary>
/// 적 메커니즘 타입
/// </summary>
public enum EnemyMechanismType
{
    Basic,      // 일반형 (기본 EnemyController와 동일한 추적)
    Shooter,    // 원거리 공격형 - 일반 Enemy처럼 이동하지만 공격 사거리가 길고 투사체 발사
    Rusher,     // 고속 돌진형 - 빠른 속도로 직선 추격 (체력 낮음)
    Bomber,     // 폭발 사망형 - 사망 시 원형 범위 폭발
    Tanker      // 탱커/벽형 - 매우 느리지만 덩치가 커서 길목을 가로막음
}
