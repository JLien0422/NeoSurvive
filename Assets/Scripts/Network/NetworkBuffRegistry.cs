using System;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Buff;

namespace NeoSurvive.Network
{
  public enum BuffEffectType
  {
    None = 0,
    Slow = 1,
    Stun = 2,
    Root = 3,
    Blind = 4,
    Vulnerable = 5,
    DamageUp = 6,
    MoveSpeedUp = 7,
    Frenzy = 8
  }

  [Serializable]
  public class BuffMapping
  {
    public uint buffId;
    public BuffEffectType effectType = BuffEffectType.None;

    [Tooltip("효과 배율(슬로우: 0.7, 데미지/이속 버프: 1.25 등)")]
    public float magnitude = 1f;
  }

  /// <summary>
  /// 서버 Buff ID -> 실제 버프 클래스를 매핑하는 레지스트리
  /// </summary>
  public class NetworkBuffRegistry : MonoBehaviour
  {
    public static NetworkBuffRegistry Instance { get; private set; }

    [SerializeField] private List<BuffMapping> mappings = new List<BuffMapping>();

    private readonly Dictionary<uint, BuffMapping> mappingTable = new Dictionary<uint, BuffMapping>();

    private void Awake()
    {
      if (Instance == null) Instance = this;
      else
      {
        Destroy(gameObject);
        return;
      }

      mappingTable.Clear();
      foreach (var mapping in mappings)
      {
        if (mapping == null) continue;
        if (!mappingTable.ContainsKey(mapping.buffId))
        {
          mappingTable.Add(mapping.buffId, mapping);
        }
      }
    }

    public bool TryCreateBuff(uint buffId, float duration, uint stacks, out IBuff buff)
    {
      buff = null;

      if (!mappingTable.TryGetValue(buffId, out BuffMapping mapping))
      {
        return false;
      }

      float stackMultiplier = GetStackedMagnitude(mapping.magnitude, stacks);

      switch (mapping.effectType)
      {
        case BuffEffectType.Slow:
          buff = new SlowDebuff(stackMultiplier, duration);
          return true;
        case BuffEffectType.Stun:
          buff = new StunDebuff(duration);
          return true;
        case BuffEffectType.Root:
          buff = new RootDebuff(duration);
          return true;
        case BuffEffectType.Blind:
          buff = new BlindDebuff(duration);
          return true;
        case BuffEffectType.Vulnerable:
          buff = new VulnerableDebuff(stackMultiplier, duration);
          return true;
        case BuffEffectType.DamageUp:
          buff = new DamageBuff(stackMultiplier, duration);
          return true;
        case BuffEffectType.MoveSpeedUp:
          buff = new MoveSpeedBuff(stackMultiplier, duration);
          return true;
        case BuffEffectType.Frenzy:
          buff = new FrenzyDebuff(duration);
          return true;
        default:
          return false;
      }
    }

    private float GetStackedMagnitude(float baseMagnitude, uint stacks)
    {
      if (stacks <= 1) return baseMagnitude;

      float magnitude = baseMagnitude;
      for (int i = 1; i < stacks; i++)
      {
        magnitude *= baseMagnitude;
      }
      return magnitude;
    }
  }
}
