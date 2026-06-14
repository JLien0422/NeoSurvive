using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Buff;

/// <summary>
/// 시냅스 과부하 서버 해킹/점령 성공 효과
/// 화면 내 적의 절반을 즉시 해킹하여 아군으로 전환 → 나머지 적을 공격하게 만듭니다.
/// 해킹된 적은 StatusFlags.frenzy = true 상태가 되어 서로 싸웁니다.
/// </summary>
public class SynapseServerEffect : MonoBehaviour
{
    [Header("아군화 설정")]
    [SerializeField] private float frenzyDuration = 10f;

    [Header("Synapse VFX")]
    [SerializeField] private GameObject synapseVfxPrefab;

    [SerializeField]
    private Vector3 defaultVfxLocalOffset =
        new Vector3(0f, 0.45f, 0f);

    [SerializeField]
    private Vector3 shooterVfxLocalOffset =
        new Vector3(0f, 0.15f, 0f);

    [SerializeField] private float vfxScale = 1f;

    private HackableObject hackableObject;
    private bool isActivated = false;

    private void Awake()
    {
        hackableObject = GetComponent<HackableObject>();

        if (hackableObject != null)
            hackableObject.OnActivated += Activate;
    }

    private void OnDestroy()
    {
        if (hackableObject != null)
            hackableObject.OnActivated -= Activate;
    }

    /// <summary>
    /// HackableObject의 해킹/점령 성공 시 호출됩니다.
    /// </summary>
    public void Activate()
    {
        if (isActivated) return;
        isActivated = true;

        StartCoroutine(HackHalfEnemies());
    }

    private IEnumerator HackHalfEnemies()
    {
        Camera cam = Camera.main;

        if (cam == null)
            yield break;

        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * cam.aspect;
        Vector3 screenCenter = cam.transform.position;

        Collider2D[] cols = Physics2D.OverlapBoxAll(
            screenCenter,
            new Vector2(worldWidth, worldHeight),
            0f
        );

        HashSet<EnemyController> seen =
            new HashSet<EnemyController>();

        List<EnemyController> enemiesInScreen =
            new List<EnemyController>();

        foreach (var col in cols)
        {
            if (!col.CompareTag("Enemy"))
                continue;

            EnemyController ec =
                col.GetComponent<EnemyController>();

            if (ec != null && seen.Add(ec))
                enemiesInScreen.Add(ec);
        }

        if (enemiesInScreen.Count == 0)
            yield break;

        ShuffleList(enemiesInScreen);

        int hackCount =
            Mathf.CeilToInt(enemiesInScreen.Count / 2f);

        for (int i = 0; i < hackCount; i++)
        {
            EnemyController ec = enemiesInScreen[i];

            if (ec == null)
                continue;

            StatusFlags flags =
                ec.GetComponent<StatusFlags>();

            if (flags == null)
                flags = ec.gameObject.AddComponent<StatusFlags>();

            flags.frenzy = true;

            SpriteRenderer sr =
                ec.GetComponentInChildren<SpriteRenderer>();

            Color originalColor =
                sr != null ? sr.color : Color.white;

            if (sr != null)
                sr.color = new Color(0f, 1f, 0.4f, 1f);

            GameObject vfxInstance =
                SpawnSynapseVFX(ec, sr);

            StartCoroutine(
                RemoveFrenzyAfterDelay(
                    flags,
                    sr,
                    originalColor,
                    frenzyDuration,
                    vfxInstance
                )
            );
        }

        Debug.Log(
            $"[SynapseServerEffect] {hackCount}마리 아군화 완료 (전체 {enemiesInScreen.Count}마리)"
        );

        yield return null;

        Destroy(this);
    }

    private GameObject SpawnSynapseVFX(
        EnemyController ec,
        SpriteRenderer sr)
    {
        if (synapseVfxPrefab == null || ec == null)
            return null;

        Transform target =
            sr != null ? sr.transform : ec.transform;

        GameObject vfx =
            Instantiate(
                synapseVfxPrefab,
                target
            );

        Vector3 offset =
            IsShooter(ec)
                ? shooterVfxLocalOffset
                : defaultVfxLocalOffset;

        vfx.transform.localPosition = offset;
        vfx.transform.localRotation = Quaternion.identity;
        vfx.transform.localScale = Vector3.one * vfxScale;

        return vfx;
    }

    private bool IsShooter(EnemyController ec)
    {
        if (ec == null)
            return false;

        return ec.gameObject.name
            .ToLower()
            .Contains("shooter");
    }

    private IEnumerator RemoveFrenzyAfterDelay(
        StatusFlags flags,
        SpriteRenderer sr,
        Color originalColor,
        float delay,
        GameObject vfxInstance)
    {
        yield return new WaitForSeconds(delay);

        if (flags != null)
            flags.frenzy = false;

        if (sr != null)
            sr.color = originalColor;

        if (vfxInstance != null)
            Destroy(vfxInstance);
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}