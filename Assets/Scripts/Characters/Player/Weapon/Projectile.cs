using UnityEngine;
using NeoSurvive.UI;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 투사체의 기본 클래스
  /// </summary>
  public class Projectile : MonoBehaviour
  {
    public float speed;
    public float damage;
    public float lifeTime = 5f;

    protected Vector3 direction;

    private Transform target;
    private Vector3 destination;
    private WeaponBase sourceWeapon;

    public event System.Action OnHitEvent;

    public virtual void Initialize(Vector3 dir, float dmg, float speed)
    {
      direction = dir.normalized;
      damage = dmg;
      this.speed = speed;
      Destroy(gameObject, lifeTime);
    }

    protected virtual void Update()
    {
      if (target != null)
      {
        direction = (target.position - transform.position).normalized;
      }
      else if (destination != Vector3.zero)
      {
        direction = (destination - transform.position).normalized;
      }
      transform.position += direction * speed * Time.deltaTime;
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
      collision.gameObject.TryGetComponent(out Enemy enemy);
      if (enemy != null)
      {
        enemy.TakeDamage(damage, sourceWeapon);
        OnHitEvent?.Invoke();
        OnHit();
      }
    }

    protected virtual void OnHit()
    {
      // 명중 이펙트
      VisualEffectHelper.CreateCircleEffect(transform.position, 0.3f, Color.white, 0.1f);
      // 기본적으로 명중 시 소멸
      Destroy(gameObject);
    }

    public void SetTarget(Transform t)
    {
      target = t;
    }

    public void SetDestination(Vector3 dest)
    {
      destination = dest;
    }

    public void SetSourceWeapon(WeaponBase weapon)
    {
      sourceWeapon = weapon;
    }
  }
}
