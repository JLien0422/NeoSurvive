using System.Collections;
using UnityEngine;
using NeoSurvive.Weapon;

public class StartDefaultWeaponsByData : MonoBehaviour
{
  [Header("시작 무기 (WeaponBase 에셋을 직접 드래그해서 넣기)")]
  public WeaponBase linkPistolData;
  public WeaponBase laserSwordData;

  [Header("옵션")]
  public bool onlyLocalPlayer = true; // Coop이면 로컬만 지급 권장

  private WeaponManager wm;
  private Player player;
  private bool done;

  private void Awake()
  {
    wm = GetComponent<WeaponManager>();
    player = GetComponent<Player>();
  }

  private void Start()
  {
    StartCoroutine(GiveDefaultWeapons());
  }

  private IEnumerator GiveDefaultWeapons()
  {
    // 다른 초기화(네트워크/플레이어 세팅) 끝난 다음 프레임에 지급
    yield return null;

    if (done) yield break;
    done = true;

    if (wm == null)
    {
      Debug.LogError("[StartDefaultWeaponsByData] WeaponManager가 없습니다. Player에 WeaponManager가 붙어있는지 확인하세요.");
      yield break;
    }

    // TODO(중요): 나중에 캐릭터 선택(Cyborg/Hacker)이 구현되면
    //            선택된 캐릭터 타입에 따라 지급 무기를 분기해야 함.
    //            지금은 선택이 없어서 둘 다 지급하는 임시 로직임.
    if (onlyLocalPlayer && player != null && !player.IsLocal)
      yield break;

    if (linkPistolData == null || laserSwordData == null)
    {
      Debug.LogError("[StartDefaultWeaponsByData] linkPistolData 또는 laserSwordData가 비어있습니다. Inspector에 WeaponBase 에셋을 넣어주세요.");
      yield break;
    }

    wm.AddWeapon(linkPistolData, sendToServer: true);
    wm.AddWeapon(laserSwordData, sendToServer: true);

    Debug.Log("[StartDefaultWeaponsByData] Default weapons granted by WeaponBase data.");
  }
}
