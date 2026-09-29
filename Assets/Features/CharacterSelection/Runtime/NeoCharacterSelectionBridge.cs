using UIEffectDemo;
using UnityEngine;
using UnityEngine.UI;

namespace NeoSurvive.UI.CharacterSelection
{
    [DisallowMultipleComponent]
    public sealed class NeoCharacterSelectionBridge : MonoBehaviour
    {
        [SerializeField] private CardSelectionController selectionController;
        [SerializeField] private LobbyCharacterSelector lobbyCharacterSelector;

        private bool isStartingGame;

        public void Configure(CardSelectionController controller, LobbyCharacterSelector lobbySelector)
        {
            selectionController = controller;
            lobbyCharacterSelector = lobbySelector;
        }

        private void Awake()
        {
            if (GetComponent<CanvasGroup>() == null)
                gameObject.AddComponent<CanvasGroup>();

            if (GetComponent<CharacterSelectionTabPresentation>() == null)
                gameObject.AddComponent<CharacterSelectionTabPresentation>();

            if (selectionController == null)
                selectionController = GetComponent<CardSelectionController>();

            if (lobbyCharacterSelector == null)
                lobbyCharacterSelector = FindObjectOfType<LobbyCharacterSelector>(true);
        }

        private void OnEnable()
        {
            if (selectionController == null)
                return;

            selectionController.SelectionChanged += HandleSelectionChanged;
            selectionController.SelectionConfirmed += HandleSelectionConfirmed;
        }

        private void Start()
        {
            if (selectionController == null)
                return;

            int savedIndex = PlayerClassSelection.Load() == PlayerClassType.Hacker ? 0 : 1;
            selectionController.SelectCard(savedIndex);
        }

        private void OnDisable()
        {
            if (selectionController == null)
                return;

            selectionController.SelectionChanged -= HandleSelectionChanged;
            selectionController.SelectionConfirmed -= HandleSelectionConfirmed;
        }

        private static void HandleSelectionChanged(int selectedIndex)
        {
            LobbySoundManager.Instance?.PlayCharacterHover();
        }

        private void HandleSelectionConfirmed(int selectedIndex)
        {
            if (isStartingGame)
                return;

            isStartingGame = true;
            PlayerClassSelection.Save(selectedIndex == 0 ? PlayerClassType.Hacker : PlayerClassType.Cyborg);
            LobbySoundManager.Instance?.PlayCharacterSelect();

            if (lobbyCharacterSelector == null)
                lobbyCharacterSelector = FindObjectOfType<LobbyCharacterSelector>(true);

            if (lobbyCharacterSelector != null)
            {
                lobbyCharacterSelector.StartGame();
                return;
            }

            isStartingGame = false;
            Debug.LogError("[CharacterSelection] LobbyCharacterSelector를 찾지 못해 게임을 시작할 수 없습니다.");
        }
    }
}
