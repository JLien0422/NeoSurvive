using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerClassType
{
    Cyborg,
    Hacker
}

/// <summary>
/// 이 Player 프리팹이 Cyborg인지 Hacker인지 구분만 해주는 "표식" 컴포넌트.
/// 스탯 차이 없더라도 시작 무기 분기 등에 사용.
/// </summary>
public class PlayerClassTag : MonoBehaviour
{
    public PlayerClassType classType = PlayerClassType.Cyborg;
}   