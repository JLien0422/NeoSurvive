using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyCharacterSelector : MonoBehaviour
{
    [Header("Load Game Scene")]
    public string gameSceneName = "Trick2Scene";

    // Hacker ��ư OnClick
    public void SelectHacker()
    {
        PlayerClassSelection.Save(PlayerClassType.Hacker);
    }

    // Cyborg ��ư OnClick
    public void SelectCyborg()
    {
        PlayerClassSelection.Save(PlayerClassType.Cyborg);
    }

    // Start ��ư OnClick
    public void StartGame()
    {
        // ?? UI(?? ??/????)?? timeScale? 0?? ?? ??? ??
        Time.timeScale = 1f;
        // Ȥ�� �ƹ��͵� ���� �� ���� ���� �⺻��(Cyborg)�� Load�ǵ��� �Ǿ�����
        SceneManager.LoadScene(gameSceneName);
    }
}
