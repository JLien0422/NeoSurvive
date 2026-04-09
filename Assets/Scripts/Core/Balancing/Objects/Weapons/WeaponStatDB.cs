using System.Collections.Generic;

public class WeaponStatDB
{
    public class Row
    {
        public int level;

        public float damage;
        public float range;
        public float angle;
        public float firerate;
        public float radius;

        public float riftduration;      // 추가(LaserSword)
        public float riftradius;        // 추가(LaserSword)
        public float rifttick;          // 추가(LaserSword)
        public float riftdamagefactor;  // 추가(LaserSword)

        public float damageperlevel;    // 추가(LaserSword)
        public float rangeperlevel;     // 추가(LaserSword)
        public float fireratemulperlevel; // 추가(LaserSword)

        public int basemaxtargets;      // 추가(SurgeBlade)
        public int maxtargetsatlv5;     // 추가(SurgeBlade)
        public float mastermultiplier;  // 추가(SurgeBlade)

        public float rotationspeed;     // 추가(EnergyShield)
        public float radiusperlevel;    // 추가(EnergyShield)
        public int baseorbcount;        // 추가(EnergyShield)
        public int maxorbcount;         // 추가(EnergyShield)
        public float masterdamagefactor;    // 추가(EnergyShield)
        public float fireinterval;      // 추가(PlasmaPhoton)
        public float speed;         // 추가(PlasmaPhoton)
        public float lifetime;      // 추가(PlasmaPhoton)
        public float basedamage;    // 추가(PlasmaPhoton)

        public float applyslow;     // 추가(PlasmaPhoton)
        public float slowmul;       // 추가(PlasmaPhoton)
        public float slowduration;  // 추가(PlasmaPhoton)

        public float applystun;     // 추가(PlasmaPhoton)
        public float stunduration;  // 추가(PlasmaPhoton)

        public float applyroot;     // 추가(PlasmaPhoton)
        public float rootduration;  // 추가(PlasmaPhoton)

        public float applyblind;    // 추가(PlasmaPhoton)
        public float blindduration; // 추가(PlasmaPhoton)

        public float applyvulnerable;   // 추가(PlasmaPhoton)
        public float vulnerablemul;     // 추가(PlasmaPhoton)
        public float vulnerableduration;    // 추가(PlasmaPhoton)

        public float destroyonhit;   // 추가(PlasmaPhoton) - 명중 시 파괴 여부

        public float knockbackforce;        // 추가(GravityHammer)
        public float knockbackperlevel;     // 추가(GravityHammer)

        public float blackholeduration;     // 추가(GravityHammer)
        public float blackholepullradius;   // 추가(GravityHammer)
        public float blackholepullforce;    // 추가(GravityHammer)

        public float speedperlevel;     // 추가(BoosterKnuckle)
        public int basecount;       // 추가(BoosterKnuckle) - 기본 발사 개수
        public int maxcount;    // 추가(BoosterKnuckle) - 최대 발사 개수

        public float homingturnspeed;   // 추가(BoosterFist) - 마스터 유도 회전 속도(도 단위)
        public float homingsearchrange;  // 추가(BoosterFist) - 마스터 유도 검색 범위

        public float baseburstinterval;     // 추가(BoltLauncher) - 기본 연발 간격
        public int baseburstcount;      // 추가(BoltLauncher) - 기본 연발 발사 개수
        public float intervaldecreaseperlevel;      // 추가(BoltLauncher) - 레벨당 연발 간격 감소량
        public int countincreaseperlevel;       // 추가(BoltLauncher) - 레벨당 연발 발사 개수 증가량
        public float minburstinterval;      // 추가(BoltLauncher) - 최소 연발 간격
        public int maxburstcount;       // 추가(BoltLauncher) - 최대 연발 발사 개수

        public float spreadangle;       // 추가(ScatterGun) - 산탄 총알 퍼짐 각도

        public int overloadstackstoexplode;     // 추가(Overload) - 폭발을 일으키는 스택 수
        public float overloadexplosionradius;   // 추가(Overload) - 폭발 반경
        public float overloadexplosiondamagefactor;  // 추가(Overload) - 폭발 피해량 계수

        public float auradamage;        // 추가(TeslaCoilArmor) - 오라 피해량
        public float auraradius;    // 추가(TeslaCoilArmor) - 오라 반경
        public float auratick;  // 추가(TeslaCoilArmor) - 오라 틱 간격

        public float puddleinterval;    // 추가(TeslaCoilArmor) - 장판 생성 간격
        public float puddleduration;    // 추가(TeslaCoilArmor) - 장판 지속 시간
        public float puddleradius;  // 추가(TeslaCoilArmor) - 장판 반경
        public float puddletick;    // 추가(TeslaCoilArmor) - 장판 틱 간격
        public float puddledamagefactor;    // 추가(TeslaCoilArmor) - 장판 피해량 계수

        public float lightninginterval;     // 추가(TeslaCoilArmor) - 번개 공격 간격
        public float lightningrange;    // 추가(TeslaCoilArmor) - 번개 공격 범위
        public float lightningdamagefactor;     // 추가(TeslaCoilArmor) - 번개 공격 피해량 계수

        public float hitcooldown;       // 추가(ChainSaw) - 명중 후 재공격까지의 쿨다운
        public float radiusscale;       // 추가(ChainSaw) - 공격 반경 스케일
        public float hitradius;     // 추가(ChainSaw) - 명중 판정 반경 (기본값은 0, ChainSaw.cs에서 radiusScale로 사용)

        public float sizeperlevel;      // 추가(ChainSaw) - 레벨당 크기 증가량
        public float cooldownmulperlevel;   // 추가(ChainSaw) - 레벨당 쿨다운 감소 배율
        public float lifetimeperlevel;    // 추가(ChainSaw) - 레벨당 수명 증가량
        public float mastersizefactor;      // 추가(ChainSaw) - 마스터 크기 배수

        public float halfwidth;     // 추가(ChainSaw) - 공격 반경 반 너비 (기본값은 0, ChainSaw.cs에서 radiusScale로 사용)
        public float halfheight;    // 추가(ChainSaw) - 공격 반경 반 높이 (기본값은 0, ChainSaw.cs에서 radiusScale로 사용)

        public float spawninterval;     // 추가(ChainSaw) - 체인톱 생성 간격
        public int spawncount;      // 추가(ChainSaw) - 한 번에 생성되는 체인톱 개수
        public float spawnradius;   // 추가(ChainSaw) - 체인톱 생성 반경
        public float hpratiodamage;   // 추가(ChainSaw) - 적 체력 비례 데미지 계수

        public float damagepertick;     // 추가(BlastBreath)
        public float width;             // 추가(BlastBreath)
        public float tickinterval;      // 추가(BlastBreath)
        public float areaduration;      // 추가(BlastBreath)
        public float aimrange;          // 추가(BlastBreath)

        public float widthperlevel;    // 추가(BlastBreath) - 레벨당 공격 범위 증가량

        public float freezeduration;    // 추가(BlastBreath) - 냉기 모드 빙결 지속 시간
        public float freezeasstun;      // 추가(BlastBreath) - 냉기 모드 빙결을 속박이 아닌 기절로 할지 여부(true면 기절, false면 속박)
        public float applyslowincold;   // 추가(BlastBreath) - 냉기 모드일 때 슬로우 적용 여부

        public float bulletspeed;   // 추가(LinkPistol) - 총알 속도

        public float followdistance;    // 추가(AIDrone) - 플레이어 따라가는 거리
        public float followspeed;       // 추가(AIDrone) - 플레이어 따라가는 속도
        public float detectionrange;    // 추가(AIDrone) - 캐릭터 기준 감지 범위
        public float attackspeed;       // 추가(AIDrone) - 공격 속도
        public float projectilespeed;   // 추가(AIDrone) - 발사체 속도
        public int subdronecount;       // 추가(AIDrone) - 하위 드론 개수

        public float installcooldown;   // 추가(AutoTurret) - 설치 쿨다운

        public float duration;      // 추가(DataScrambler) - 혼란 효과 지속 시간
        public float durationperlevel;  // 추가(DataScrambler) - 레벨당 지속 시간 증가량
        public float frenzyduration;    // 추가(DataScrambler) - 광폭화 지속 시간
        public float explosionradius;   // 추가(DataScrambler) - 폭발 반경

        public float areasize;      // 추가(EMPPulseGenerator) - 장판 크기
        public float areasizeperlevel;   // 추가(EMPPulseGenerator) - 레벨당 장판 크기 증가량

        public int penetration;     // 추가(PlasmaRifle) - 관통 수

        public float hp;    // 추가(HologramDecoy) - 분신 체력
        public float cooldown;  // 추가(HologramDecoy) - 분신 소환 쿨다운
        public float prisonduration;    // 추가(HologramDecoy) - 데이터 감옥 지속 시간
        public float hpperlevel;    // 추가(HologramDecoy) - 레벨당 분신 체력 증가량
        public float cooldownreductionperlevel; // 추가(HologramDecoy) - 레벨당 분신 소환 쿨다운 감소량
        public float prisonrange;   // 추가(HologramDecoy) - 데이터 감옥 범위

        public float spawndistance;     // 추가(NanoWire) - 나노와이어 생성 거리
        public float activationdelay;   // 추가(NanoWire) - 나노와이어 활성화 지연 시간
        public float tickrate;      // 추가(NanoWire) - 나노와이어 피해 틱 간격

        public float archeight;         // EMPGrenade
        public float traveltime;        // EMPGrenade

                // DataOptimization 전용 스탯
        public float fieldduration;             // 데이터 최적화 필드 지속시간
        public float fieldwidth;                // 데이터 최적화 필드 너비
        public float fieldheight;               // 데이터 최적화 필드 높이
        public float damagebuffmultiplier;      // 투사체 데미지 배율 (예: 1.2 ~ 2.0)
        public float buffduration;              // 투사체 버프 지속시간
        public float masterslowmul;             // Lv5 필드 접촉 슬로우 배율
        public float masterslowduration;        // Lv5 슬로우 지속시간(접촉형이면 참고값)

        // DigitalShield 전용 스탯
        public float knockbackduration;         // 디지털 실드 넉백 지속시간
        public float collisiondamagemultiplier; // Lv5 충돌 피해 배율
        public float collisiondetectradius;     // Lv5 충돌 감지 반경
        public float collisionimpactradius;     // Lv5 충돌 피해 반경
        public float collisioncarrierduration;  // Lv5 충돌 캐리어 유지시간
    }

    public Dictionary<string, Dictionary<int, Row>> rows
        = new Dictionary<string, Dictionary<int, Row>>();
}
