// CLI 계산 검증용입니다. Unity 씬·물리·렌더링을 실행하지 않습니다.
using System;
namespace UnityEngine
{
    public class Object
    {
        private static int nextId;
        private readonly int id = ++nextId;
        public int GetInstanceID() => id;
    }
    public class ScriptableObject : Object { }
    public class Component : Object
    {
        public GameObject gameObject = new GameObject();
        public object ParentComponent;
        public Transform transform => gameObject.transform;
        public bool isActiveAndEnabled = true;
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
        public T GetComponentInParent<T>() where T : class => ParentComponent as T ?? gameObject.GetComponentInParent<T>();
        public T GetComponentInChildren<T>(bool includeInactive = false) where T : class => GetComponent<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) where T : class =>
            GetComponent<T>() is T component ? new[] { component } : Array.Empty<T>();
    }
    public class MonoBehaviour : Component
    {
        protected static void Destroy(object value) { }
        protected Coroutine StartCoroutine(System.Collections.IEnumerator value) => new Coroutine();
        protected static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation) => new GameObject();
    }
    public class Coroutine { }
    public class WaitForSeconds { public WaitForSeconds(float value) { } }
    public class GameObject : Object
    {
        private readonly System.Collections.Generic.Dictionary<Type, Component> components = new();
        public string name = "Validation";
        public bool activeSelf = true;
        public void SetActive(bool active)
        {
            if (activeSelf == active) return;
            activeSelf = active;
            foreach (var component in components.Values)
            {
                component.isActiveAndEnabled = active;
                component.GetType().GetMethod(active ? "OnEnable" : "OnDisable",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(component, null);
            }
        }
        public GameObject parent;
        public Transform transform = new Transform();
        public T GetComponent<T>() where T : class
        {
            foreach (var component in components.Values) if (component is T typed) return typed;
            return null;
        }
        public T GetComponentInParent<T>() where T : class => GetComponent<T>() ?? parent?.GetComponentInParent<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) where T : class =>
            GetComponent<T>() is T component ? new[] { component } : Array.Empty<T>();
        public T AddComponent<T>() where T : Component, new()
        {
            T component = new T();
            component.gameObject = this;
            components[typeof(T)] = component;
            return component;
        }
    }
    public class Sprite { }
    public class SerializeField : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    public class TextAreaAttribute : Attribute { }
    public class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    public class MinAttribute : Attribute { public MinAttribute(float min) { } }
    public class CreateAssetMenuAttribute : Attribute { public string fileName; public string menuName; }
    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
    public static class Random
    {
        public static int Range(int min, int max) => min;
        public static float Range(float min, float max) => min;
        public static float value => 1f;
    }
    public static class Time { public static float time, deltaTime; }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2();
        public static Vector2 right => new Vector2(1f, 0f);
        public float sqrMagnitude => x*x + y*y;
        public Vector2 normalized => sqrMagnitude > 0f ? this * (1f / (float)Math.Sqrt(sqrMagnitude)) : zero;
        public static Vector2 operator *(Vector2 v, float scale) => new Vector2(v.x * scale, v.y * scale);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x-b.x, a.y-b.y);
        public static float Distance(Vector2 a, Vector2 b) => (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static implicit operator Vector2(Vector3 v) => new Vector2 { x = v.x, y = v.y };
    }
    public class Transform { public Vector3 position, localScale; }
    public struct Color { public static Color magenta, yellow; }
    public static class Debug { public static void Log(object value) { } public static void LogWarning(object value) { } }
    public class Collider2D : Component { public bool CompareTag(string tag) => true; }
    public static class Physics2D
    {
        public static Collider2D[] OverlapCircleAll(Vector3 center, float radius) => Array.Empty<Collider2D>();
        public static void IgnoreCollision(Collider2D first, Collider2D second, bool value) { }
    }
    public class Rigidbody2D : Component { public Vector2 velocity; }
    public class SpriteRenderer : Component { public int sortingOrder; }
    public struct Quaternion
    {
        public static Quaternion identity => new Quaternion();
        public static Quaternion Euler(float x, float y, float z) => new Quaternion();
    }
    public class Animator : Component
    {
        public AnimatorControllerParameter[] parameters = Array.Empty<AnimatorControllerParameter>();
        public void ResetTrigger(string value) { }
        public void SetTrigger(string value) { }
    }
    public class AnimatorControllerParameter { public AnimatorControllerParameterType type; public string name; }
    public enum AnimatorControllerParameterType { Trigger }
}
public class Player : Character
{
    public static Player Instance;
    public Player() { maxHP = new Stat(100f); currentHP = new Stat(100f); }
    public bool HasTrait(string id) => false;
    public float GetCriticalBreakthroughMultiplier() => 1f;
    public float GetVirusDevelopmentMultiplier() => 1f;
}
public class Enemy : Character { public void SetDead(bool value) => IsDead = value; }
public class TaggedObstacle : Character { }
public class SpriteHitFlash : UnityEngine.Component { public void PlayFlash() { } }
public class UIManager
{
    public static UIManager Instance;
    public void ShowDamageText(UnityEngine.Vector3 position, float value) { }
}
public class SoundManager
{
    public static SoundManager Instance;
    public void PlayPlayerHit() { }
    public void PlayEnemyHit() { }
}
public class WeaponDamageStats
{
    public static WeaponDamageStats Instance;
    public void RecordDamage(NeoSurvive.Weapon.WeaponBase weapon, float value) { }
}
public class MarkedTarget : UnityEngine.Component
{
    public float ApplyHackerWeaponDamageBonus(float amount, NeoSurvive.Weapon.WeaponBase weapon) => amount;
    public void RefreshTransferMetadata() { }
}
public class GameManager { public static GameManager Instance; public void AddKill() { } }
public static class GameAnalyticsTracker { public static void TrackBossDefeated(Boss boss) { } }
public class BossAttackWarningDisplay : UnityEngine.Component
{
    public UnityEngine.GameObject TelegraphCirclePrefab, TelegraphRectPrefab;
}
namespace NeoSurvive.Buff
{
    public class KnockbackDebuff : UnityEngine.Component
    {
        public bool IsKnockbackActive;
        public UnityEngine.Vector2 LastVelocity;
        public float LastDuration;
        public void ApplyKnockback(UnityEngine.Vector2 velocity, float duration)
        {
            LastVelocity = velocity; LastDuration = duration; IsKnockbackActive = true;
        }
    }
}
public static class RunTraitChainKnockback
{
    public static Character LastOrigin;
    public static void Begin(Character target, NeoSurvive.Weapon.WeaponBase weapon, UnityEngine.Vector2 velocity, float duration) => LastOrigin = target;
}
public static class WeaponManager
{
    public static bool AllowMarkedDamage = true;
    public static bool IsMarkedDamageBonusSource(NeoSurvive.Weapon.WeaponBase weapon) => weapon != null && AllowMarkedDamage;
}
// 물리 탐색과 선 렌더링만 대체합니다. 전이 여부/효율/만료/런 경계 로직은 실제 소스를 실행합니다.
public static class RunTraitSpecialRuntime
{
    public static Character Candidate;
    public static int Transfers;
    public static Character LastDamaged;
    public static float LastRawDamage, LastActualDamage;
    public static SpecialTraitEffect Effect(NeoSurvive.Weapon.WeaponBase weapon, RunTraitSpecialKind kind) =>
        RunTraitManager.Instance?.GetSpecialEffect(weapon, kind);
    public static Character Nearest(UnityEngine.Vector2 center, float radius, Character excluded) =>
        Candidate != excluded && Candidate != null && !Candidate.IsDead ? Candidate : null;
    public static void Line(UnityEngine.Vector3 from, UnityEngine.Vector3 to, UnityEngine.Color color) => Transfers++;
    public static void OnDamage(Character target, NeoSurvive.Weapon.WeaponBase weapon, float raw, float actual, bool lethal)
    {
        LastDamaged = target; LastRawDamage = raw; LastActualDamage = actual;
    }
}
