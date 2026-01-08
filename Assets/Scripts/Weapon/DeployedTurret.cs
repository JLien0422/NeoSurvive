using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    public class DeployedTurret : MonoBehaviour
    {
        private float damage;
        private float range;
        private float fireRate;
        private GameObject projectilePrefab;
        
        private float fireTimer;

        public void Initialize(float damage, float range, float fireRate, float lifeTime, GameObject projectilePrefab)
        {
            this.damage = damage;
            this.range = range;
            this.fireRate = fireRate;
            this.projectilePrefab = projectilePrefab;
            
            Destroy(gameObject, lifeTime);
            
            // Setup Visuals if missing
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
                sr.color = Color.gray;
                sr.sortingOrder = 4;
            }
        }

        private void Update()
        {
            fireTimer += Time.deltaTime;
            if (fireTimer >= fireRate)
            {
                Attack();
                fireTimer = 0f;
            }
        }

        private void Attack()
        {
            if (projectilePrefab == null) return;

            GameObject target = FindClosestEnemy();
            if (target == null) return;

            Vector3 dir = (target.transform.position - transform.position).normalized;
            GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            obj.SetActive(true);
            Projectile p = obj.GetComponent<Projectile>();
            if (p != null) p.Initialize(dir, damage);
        }

        private GameObject FindClosestEnemy()
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
