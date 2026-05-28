using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class ChainSawEmitter : MonoBehaviour
  {
    [Header("Spawn")]
    public GameObject chainSawPrefab;
    public float spawnRadius = 0.6f;

    private Transform owner;
    private GameObject activeSaw;

    private void Start()
    {
      owner = GetComponentInParent<Player>()?.transform;

      if (owner == null)
        owner = transform;

      Spawn();
    }

    private void Update()
    {
      // ★ ChainSaw가 사라졌을 때만 다시 생성
      if (activeSaw == null)
        Spawn();
    }

    private void Spawn()
    {
      if (chainSawPrefab == null) return;
      if (owner == null) return;

      Vector2 offset =
        Random.insideUnitCircle * spawnRadius;

      Vector3 pos =
        owner.position + (Vector3)offset;

      // ★ 부모 없이 월드에 생성
      // → Player 반전 영향 제거
      activeSaw =
        Instantiate(
          chainSawPrefab,
          pos,
          Quaternion.identity
        );

      ChainSaw saw =
        activeSaw.GetComponent<ChainSaw>();

      if (saw != null)
      {
        // ★ 위치 기준만 전달
        // ★ 부모로 붙이지 않음
        saw.SetOwner(owner);
      }
    }
  }
}