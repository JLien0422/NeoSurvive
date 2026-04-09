using UnityEngine;
using NeoSurvive.Balancing.Map;

public class MapCsvDebug : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("===== CSV DEBUG START =====");

        if (MapEnvironmentGimmickLoader.DB == null)
        {
            Debug.LogError("[DEBUG] Loader DB가 아직 null임");
            return;
        }

        var row = MapEnvironmentGimmickLoader.DB.Get("Map1", "cargo_drop_crane");

        if (row == null)
        {
            Debug.LogError("[DEBUG] cargo_drop_crane 없음");
            return;
        }

        Debug.Log($"[DEBUG] DB 값 | playerDamage={row.playerDamage}, enemyDamage={row.enemyDamage}");
    }
}