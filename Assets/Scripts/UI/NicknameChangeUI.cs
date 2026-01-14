using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeoSurvive.Network;

namespace NeoSurvive.UI
{
    public class NicknameChangeUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI statusText;

        private GameServerAPI serverAPI;

        private void Start()
        {
            if (GameManager.Instance != null)
                serverAPI = GameManager.Instance.GetComponent<GameServerAPI>();
            
            if (serverAPI == null)
                serverAPI = FindObjectOfType<GameServerAPI>();
                
            if (confirmButton != null)
                confirmButton.onClick.AddListener(TryChangeNickname);
                
            if (cancelButton != null)
                cancelButton.onClick.AddListener(Close);
                
            // 시작 시 닫기
            Close();
        }

        public void Open()
        {
            if (panel != null) panel.SetActive(true);
            if (nicknameInput != null) nicknameInput.text = "";
            if (statusText != null) statusText.text = "";
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void TryChangeNickname()
        {
            if (serverAPI == null || nicknameInput == null) return;
            
            string newNickname = nicknameInput.text;
            if (string.IsNullOrEmpty(newNickname))
            {
                if (statusText != null) statusText.text = "닉네임을 입력해주세요.";
                return;
            }

            if (confirmButton != null) confirmButton.interactable = false;
            if (statusText != null) statusText.text = "변경 중...";

            StartCoroutine(serverAPI.ChangeNickname(newNickname, (response) =>
            {
                if (confirmButton != null) confirmButton.interactable = true;
                
                if (response.success)
                {
                    if (statusText != null) statusText.text = "변경 완료!";
                    Debug.Log("닉네임 변경 성공");
                    Invoke("Close", 1.0f);
                }
                else
                {
                    if (statusText != null) statusText.text = $"실패: {response.error}";
                }
            }));
        }
    }
}