using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity HTTP 보안 설정을 자동으로 수정하는 에디터 스크립트
/// </summary>
public class UnityHttpSecurityFixer
{
    [MenuItem("Tools/NeoSurvive/Enable HTTP Connections")]
    public static void EnableHttpConnections()
    {
        // HTTP 연결 허용 설정
        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
        
        Debug.Log("[HTTPFixer] HTTP connections have been enabled in Player Settings.");
        Debug.Log("[HTTPFixer] You can now connect to http://localhost:5157");
        
        AssetDatabase.SaveAssets();
    }
    
    [MenuItem("Tools/NeoSurvive/Check HTTP Settings")]
    public static void CheckHttpSettings()
    {
        Debug.Log($"[HTTPFixer] Current HTTP setting: {PlayerSettings.insecureHttpOption}");
        
        if (PlayerSettings.insecureHttpOption == InsecureHttpOption.NotAllowed)
        {
            Debug.LogWarning("[HTTPFixer] HTTP connections are BLOCKED. Run 'Tools/NeoSurvive/Enable HTTP Connections' to fix.");
        }
        else
        {
            Debug.Log("[HTTPFixer] HTTP connections are allowed.");
        }
    }
}
