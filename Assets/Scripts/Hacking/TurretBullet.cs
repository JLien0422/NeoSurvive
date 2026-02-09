using UnityEngine;

/// <summary>
/// 보안 터렛이 발사하는 총알
/// </summary>
public class TurretBullet : MonoBehaviour
{
    private float damage;
    private float speed;
    private float lifeTime;

    public void Init(float damage, float speed, float lifeTime)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifeTime = lifeTime;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // 전방으로 이동
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
