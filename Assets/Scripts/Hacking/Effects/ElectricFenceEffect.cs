using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Buff;

/// <summary>
/// 전기 울타리 해킹 성공 효과
/// 플레이어를 에워싼 사각형 전기 벽 4개를 생성합니다.
/// 적이 벽에 닿으면 지속 데미지를 입고, 울타리가 적 접근을 물리적으로 차단합니다.
/// </summary>
public class ElectricFenceEffect : MonoBehaviour
{
    [Header("울타리 설정")]
    [SerializeField] private float fenceSize   = 6f;       // 울타리 한 변의 절반 길이 (플레이어 기준)
    [SerializeField] private float fenceHeight = 0.3f;     // 울타리 두께 (콜라이더 두께)
    [SerializeField] private float duration    = 8f;       // 울타리 지속 시간 (초)

    [Header("데미지 설정")]
    [SerializeField] private float damagePerSecond = 15f;  // 초당 데미지
    [SerializeField] private float damageInterval  = 0.5f; // 데미지 판정 간격 (초)

    [Header("비주얼")]
    [SerializeField] private Color fenceColor = new Color(0.2f, 0.8f, 1f, 0.8f); // 전기 청색

    // 생성된 울타리 벽 4개 참조
    private GameObject[] fenceWalls = new GameObject[4];

    // 현재 울타리에 의해 기절 중인 적 목록 (울타리 벗어나면 즉시 해제하기 위해 추적)
    private HashSet<StatusFlags> stunnedEnemies = new HashSet<StatusFlags>();

    /// <summary>
    /// 외부(HackableObject)에서 호출하여 효과를 시작합니다.
    /// </summary>
    public void Activate()
    {
        StartCoroutine(SpawnFence());
    }

    /// <summary>
    /// 플레이어 주변에 사각형 울타리를 생성하고 지속 데미지를 부여하는 코루틴
    /// </summary>
    private IEnumerator SpawnFence()
    {
        Player player = FindObjectOfType<Player>();
        if (player == null) yield break;

        Vector3 center = player.transform.position;

        // 울타리 4면 생성 (상/하/좌/우)
        // 상단 벽
        fenceWalls[0] = CreateFenceWall(
            center + new Vector3(0f, fenceSize, 0f),
            new Vector2(fenceSize * 2f, fenceHeight)
        );
        // 하단 벽
        fenceWalls[1] = CreateFenceWall(
            center + new Vector3(0f, -fenceSize, 0f),
            new Vector2(fenceSize * 2f, fenceHeight)
        );
        // 좌측 벽
        fenceWalls[2] = CreateFenceWall(
            center + new Vector3(-fenceSize, 0f, 0f),
            new Vector2(fenceHeight, fenceSize * 2f)
        );
        // 우측 벽
        fenceWalls[3] = CreateFenceWall(
            center + new Vector3(fenceSize, 0f, 0f),
            new Vector2(fenceHeight, fenceSize * 2f)
        );

        // 데미지 코루틴 시작
        StartCoroutine(DamageCoroutine());

        // duration 후 울타리 제거
        yield return new WaitForSeconds(duration);

        // 울타리 종료 시 기절 중인 적 전체 해제
        ReleaseAllStuns();

        foreach (var wall in fenceWalls)
        {
            if (wall != null) Destroy(wall);
        }

        Destroy(this);
    }

    /// <summary>
    /// 일정 간격으로 울타리에 닿아있는 적에게 데미지 + 기절을 줍니다.
    /// 울타리를 벗어난 적은 즉시 기절 해제합니다.
    /// </summary>
    private IEnumerator DamageCoroutine()
    {
        float damagePerTick = damagePerSecond * damageInterval;

        while (true)
        {
            yield return new WaitForSeconds(damageInterval);

            // 울타리가 모두 파괴되면 종료 후 기절 전체 해제
            bool anyAlive = false;
            foreach (var wall in fenceWalls)
                if (wall != null) { anyAlive = true; break; }
            if (!anyAlive)
            {
                ReleaseAllStuns();
                yield break;
            }

            // 이번 틱에 울타리에 닿아있는 적 목록
            HashSet<StatusFlags> currentlyTouching = new HashSet<StatusFlags>();

            foreach (var wall in fenceWalls)
            {
                if (wall == null) continue;

                // 벽의 world size = localScale (col.size는 Vector2.one이므로)
                Vector2 worldSize = new Vector2(
                    wall.transform.localScale.x,
                    wall.transform.localScale.y
                ) * 1.2f;

                Collider2D[] hits = Physics2D.OverlapBoxAll(wall.transform.position, worldSize, 0f);

                foreach (var hit in hits)
                {
                    if (!hit.CompareTag("Enemy")) continue;

                    // 데미지 적용
                    Character c = hit.GetComponent<Character>();
                    if (c != null) c.TakeDamage(damagePerTick);

                    // 기절 적용 (이동 차단 + 공격 차단)
                    StatusFlags flags = hit.GetComponent<StatusFlags>();
                    if (flags == null) flags = hit.gameObject.AddComponent<StatusFlags>();

                    flags.moveBlocked   = true;
                    flags.attackBlocked = true;
                    currentlyTouching.Add(flags);
                    stunnedEnemies.Add(flags);
                }
            }

            // 이번 틱에 울타리 밖으로 나간 적은 기절 해제
            HashSet<StatusFlags> toRelease = new HashSet<StatusFlags>();
            foreach (var flags in stunnedEnemies)
            {
                if (!currentlyTouching.Contains(flags))
                    toRelease.Add(flags);
            }
            foreach (var flags in toRelease)
            {
                ReleaseStun(flags);
                stunnedEnemies.Remove(flags);
            }
        }
    }

    /// <summary>
    /// 단일 적의 기절 상태를 해제합니다.
    /// </summary>
    private void ReleaseStun(StatusFlags flags)
    {
        if (flags == null) return;
        flags.moveBlocked   = false;
        flags.attackBlocked = false;
    }

    /// <summary>
    /// 울타리 종료 시 기절 중인 모든 적을 일괄 해제합니다.
    /// </summary>
    private void ReleaseAllStuns()
    {
        foreach (var flags in stunnedEnemies)
            ReleaseStun(flags);
        stunnedEnemies.Clear();
    }

    /// <summary>
    /// 개별 전기 울타리 벽 오브젝트를 생성합니다.
    /// localScale로 크기를 조절하고, BoxCollider2D.size는 Vector2.one으로 고정합니다.
    /// (col.size와 localScale을 모두 size로 설정하면 실제 충돌 크기가 size²가 되는 버그 방지)
    /// </summary>
    private GameObject CreateFenceWall(Vector3 position, Vector2 size)
    {
        GameObject wall = new GameObject("ElectricFenceWall");
        wall.transform.position   = position;
        // scale로 벽 크기 결정 (col.size는 Vector2.one으로 고정)
        wall.transform.localScale = new Vector3(size.x, size.y, 1f);
        wall.tag = "ElectricFence";

        // 물리적 충돌 차단용 BoxCollider2D (크기는 scale이 결정)
        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size      = Vector2.one; // scale이 (size.x, size.y)이므로 world size = size
        col.isTrigger = false;       // 실제 물리 차단

        // Rigidbody2D (Static) - 충돌 계산을 위해 필요
        Rigidbody2D rb = wall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        // 시각화용 SpriteRenderer (단색 전기빛)
        SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
        sr.color        = fenceColor;
        sr.sortingOrder = 5;

        // 흰색 1x1 텍스처 (scale로 크기 조절되므로 1x1로 충분)
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        return wall;
    }
}
