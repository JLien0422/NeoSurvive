using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Buff;

/// <summary>
/// 마그네틱 비컨 해킹/점령 성공 효과
/// 화면 내 모든 적을 우측(3시 방향) 끝으로 강제 견인하여 한곳에 몰아버립니다.
/// </summary>
public class MagneticBeaconEffect : MonoBehaviour
{
    [Header("견인 설정")]
    [SerializeField] private float pullSpeed = 20f;
    [SerializeField] private float pullDuration = 3f;
    [SerializeField] private float gatherOffset = 3f;
    [SerializeField] private float stopDistance = 0.08f;

    [Header("새 적 감지")]
    [SerializeField] private float refreshInterval = 0.2f;

    [Header("Beacon VFX")]
    [SerializeField] private GameObject beaconVfxPrefab;
    [SerializeField] private float vfxScale = 1f;

    private HackableObject hackableObject;
    private bool isActivated = false;

    private GameObject beaconVfxInstance;

    private class PulledEnemy
    {
        public GameObject enemyObject;
        public StatusFlags flags;
        public Rigidbody2D rb;
    }

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

    public void Activate()
    {
        if (isActivated) return;
        isActivated = true;

        StartCoroutine(PullAllEnemiesToRight());
    }

    private IEnumerator PullAllEnemiesToRight()
    {
        Player player = FindObjectOfType<Player>();

        Vector2 gatherPoint = GetGatherPoint(player);

        SpawnBeaconVFX(gatherPoint);

        List<PulledEnemy> pulledEnemies =
            new List<PulledEnemy>();

        RefreshPulledEnemies(pulledEnemies);

        float elapsed = 0f;
        float refreshTimer = 0f;

        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;
            refreshTimer += Time.deltaTime;

            gatherPoint = GetGatherPoint(player);

            if (beaconVfxInstance != null)
                beaconVfxInstance.transform.position =
                    gatherPoint;

            if (refreshTimer >= refreshInterval)
            {
                RefreshPulledEnemies(pulledEnemies);
                refreshTimer = 0f;
            }

            foreach (PulledEnemy pulled in pulledEnemies)
            {
                if (pulled == null) continue;
                if (pulled.enemyObject == null) continue;
                if (pulled.flags == null) continue;

                Vector2 enemyPos =
                    pulled.rb != null
                        ? pulled.rb.position
                        : (Vector2)pulled.enemyObject.transform.position;

                Vector2 toCenter =
                    gatherPoint - enemyPos;

                float dist =
                    toCenter.magnitude;

                if (dist < stopDistance)
                {
                    if (pulled.rb != null)
                        pulled.rb.velocity = Vector2.zero;

                    pulled.flags.pullVelocity = Vector2.zero;
                    continue;
                }

                Vector2 pullDir =
                    toCenter.normalized;

                Vector2 targetVelocity =
                    pullDir * pullSpeed;

                pulled.flags.isPulled = true;
                pulled.flags.pullVelocity = targetVelocity;

                if (pulled.rb != null &&
                    pulled.rb.bodyType == RigidbodyType2D.Dynamic)
                {
                    pulled.rb.velocity = Vector2.Lerp(
                        pulled.rb.velocity,
                        targetVelocity,
                        Time.deltaTime * 6f
                    );

                    pulled.rb.MovePosition(
                        Vector2.MoveTowards(
                            pulled.rb.position,
                            gatherPoint,
                            pullSpeed * Time.deltaTime
                        )
                    );
                }
                else
                {
                    pulled.enemyObject.transform.position =
                        Vector3.MoveTowards(
                            pulled.enemyObject.transform.position,
                            gatherPoint,
                            pullSpeed * Time.deltaTime
                        );
                }
            }

            yield return null;
        }

        foreach (PulledEnemy pulled in pulledEnemies)
        {
            if (pulled == null) continue;
            if (pulled.flags == null) continue;

            pulled.flags.isPulled = false;
            pulled.flags.pullVelocity = Vector2.zero;

            if (pulled.rb != null)
                pulled.rb.velocity = Vector2.zero;
        }

        if (beaconVfxInstance != null)
            Destroy(beaconVfxInstance);

        Destroy(this);
    }

    private void RefreshPulledEnemies(
        List<PulledEnemy> pulledEnemies)
    {
        GameObject[] enemies =
            GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null)
                continue;

            bool alreadyExists = false;

            foreach (PulledEnemy pulled in pulledEnemies)
            {
                if (pulled == null)
                    continue;

                if (pulled.enemyObject == enemy)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (alreadyExists)
                continue;

            StatusFlags flags =
                enemy.GetComponent<StatusFlags>();

            if (flags == null)
                flags = enemy.AddComponent<StatusFlags>();

            Rigidbody2D rb =
                enemy.GetComponent<Rigidbody2D>();

            flags.isPulled = true;
            flags.pullVelocity = Vector2.zero;

            pulledEnemies.Add(
                new PulledEnemy
                {
                    enemyObject = enemy,
                    flags = flags,
                    rb = rb
                }
            );
        }
    }

    private Vector2 GetGatherPoint(Player player)
    {
        if (player != null)
        {
            return
                (Vector2)player.transform.position +
                Vector2.right * gatherOffset;
        }

        return
            (Vector2)transform.position +
            Vector2.right * gatherOffset;
    }

    private void SpawnBeaconVFX(Vector2 position)
    {
        if (beaconVfxPrefab == null)
            return;

        beaconVfxInstance =
            Instantiate(
                beaconVfxPrefab,
                position,
                Quaternion.identity
            );

        beaconVfxInstance.transform.localScale =
            Vector3.one * vfxScale;
    }
}