using UnityEngine;

/// <summary>26-10-22 Xabuna: 특성으로 생성한 분신만 추적해 특성 제거/런 초기화 시 정리합니다.</summary>
public class RunTraitCloneLifetime : MonoBehaviour
{
    private RunTraitManager manager;
    private SpecialTraitEffect effect;
    private int version;

    public static void Track(GameObject clone, SpecialTraitEffect effect)
    {
        if (clone == null || effect == null || RunTraitManager.Instance == null) return;
        var lifetime = clone.GetComponent<RunTraitCloneLifetime>() ?? clone.AddComponent<RunTraitCloneLifetime>();
        lifetime.Unsubscribe();
        lifetime.manager = RunTraitManager.Instance;
        lifetime.effect = effect;
        lifetime.version = lifetime.manager.RunVersion;
        lifetime.manager.OnTraitsChanged += lifetime.OnTraitsChanged;
        lifetime.OnTraitsChanged();
    }

    private void OnTraitsChanged()
    {
        if (manager != null && version == manager.RunVersion && manager.HasEffect(effect)) return;
        // Destroy는 프레임 끝에 실행되므로 판정/공격부터 중지합니다.
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void Unsubscribe()
    {
        if (manager != null) manager.OnTraitsChanged -= OnTraitsChanged;
        manager = null;
    }

    private void OnDisable() => Unsubscribe();
}
