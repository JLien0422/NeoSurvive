using System.Collections;
using UnityEngine;
using NeoSurvive.Weapon;

public class StartDefaultWeaponsByClass : MonoBehaviour
{
  [Header("클래스 표식")]
  public PlayerClassTag classTag; // 없으면 자동 탐색

  [Header("기본 무기 (WeaponBase 에셋을 직접 지정)")]
  public WeaponBase cyborgDefaultWeapon; // LaserSword(WeaponBase)
  public WeaponBase hackerDefaultWeapon; // LinkPistol(WeaponBase)

  [Header("옵션")]
  public bool onlyLocalPlayer = true; // Coop이면 로컬만 지급 권장

  private WeaponManager wm;
  private Player player;
  private bool done;

  private void Awake()
  {
    wm = GetComponent<WeaponManager>();
    player = GetComponent<Player>();
    if (classTag == null) classTag = GetComponent<PlayerClassTag>();
  }

  private void Start()
  {
    StartCoroutine(GiveDefaultWeapon());
  }

  private IEnumerator GiveDefaultWeapon()
  {
    yield return null; // 다른 초기화 끝난 뒤 안전하게 지급

    if (done) yield break;
    done = true;

    if (wm == null)
    {
      Debug.LogError("[StartDefaultWeaponsByClass] WeaponManager가 없습니다.");
      yield break;
    }

    if (onlyLocalPlayer && player != null && !player.IsLocal)
      yield break;

    if (classTag == null)
    {
      Debug.LogError("[StartDefaultWeaponsByClass] PlayerClassTag가 없습니다. Player 프리팹에 PlayerClassTag를 붙이세요.");
      yield break;
    }

    // (중요) 나중에 "게임 중 선택"이 생기면 Start에서 주지 말고, 선택 확정 시점에 호출하도록 위치를 옮기면 됨.
    WeaponBase toGive =
      (classTag.classType == PlayerClassType.Cyborg) ? cyborgDefaultWeapon : hackerDefaultWeapon;

    if (toGive == null)
    {
      Debug.LogError("[StartDefaultWeaponsByClass] 기본 무기 WeaponBase가 비어있습니다. Inspector에 LaserSword/LinkPistol WeaponBase를 넣어주세요.");
      yield break;
    }

    wm.AddWeapon(toGive, sendToServer: true);

    Debug.Log($"[StartDefaultWeaponsByClass] Default weapon granted: {toGive.weaponName} ({classTag.classType})");
  }
}
