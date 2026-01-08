using UnityEngine;

namespace NeoSurvive.Exp
{
    /// <summary>
    /// 플레이어 경험치/레벨업 관리 컴포넌트
    /// PlayerController는 유지하고, Player에 이 컴포넌트를 추가로 붙여서 사용
    /// </summary>
    public class PlayerExperience : MonoBehaviour
    {
        [Header("Level")]
        [SerializeField] private int level = 1;
        [SerializeField] private int currentExp = 0;
        [SerializeField] private int expToNextLevel = 5;

        [Header("Growth")]
        [SerializeField] private float growthMultiplier = 1.35f;

        public int Level => level;
        public int CurrentExp => currentExp;
        public int ExpToNextLevel => expToNextLevel;

        public void AddExp(int amount)
        {
            if (amount <= 0) return;

            currentExp += amount;

            while (currentExp >= expToNextLevel)
            {
                currentExp -= expToNextLevel;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            level++;
            expToNextLevel = Mathf.CeilToInt(expToNextLevel * growthMultiplier);

            Debug.Log($"[EXP] 레벨업! 현재 레벨: {level}, 다음 필요 EXP: {expToNextLevel}");

            // TODO: 여기서 스킬 선택 UI/스탯 증가 호출하면 뱀서 느낌 완성
        }
    }
}
