using UnityEngine;

namespace NeoSurvive.Core
{
    /// <summary>
    /// 데미지를 입을 수 있는 모든 객체를 위한 인터페이스
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }
}
