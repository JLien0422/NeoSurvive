using UnityEngine;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 모든 무기의 기본 클래스
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        [Header("Weapon Settings")]
        public string weaponName;
        public float damage;
        public float attackInterval;
        public float range;
        public int level = 0;

        protected float timer;
        protected float tickTimer; // 장판 데미지용 틱 타이머

        protected virtual void Update()
        {
            timer += Time.deltaTime;
            if (timer >= attackInterval)
            {
                Attack();
                timer = 0f;
            }
        }

        public abstract void Attack();

        protected GameObject FindClosestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            GameObject closest = null;
            float closestDistance = range > 0 ? range : 10f;

            foreach (GameObject enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = enemy;
                }
            }
            return closest;
        }
    }
}
