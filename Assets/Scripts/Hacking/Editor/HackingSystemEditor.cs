using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HackingSystem))]
public class HackingSystemEditor : Editor
{
    private static readonly Color ZoneLineColor = new Color(0f, 0.85f, 0.95f, 0.95f);
    private static readonly Color ZoneFillColor = new Color(0f, 0.85f, 0.95f, 0.12f);
    private static readonly Color HackRangeColor = new Color(1f, 0.92f, 0.2f, 0.9f);

    public override void OnInspectorGUI()
    {
        HackingSystem system = (HackingSystem)target;

        EditorGUILayout.LabelField("ES3 로컬 저장", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "플레이 중 인스펙터에서 바꾼 값은 플레이 종료 시 ES3에 자동 저장됩니다.\n" +
            "에디터 모드에서는 아래 버튼으로 수동 저장/불러오기 하세요.",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("ES3 불러오기"))
        {
            system.LoadCyborgSettings();
            EditorUtility.SetDirty(system);
        }

        if (GUILayout.Button("ES3 저장"))
        {
            system.SaveCyborgSettings();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(6f);

        DrawDefaultInspector();
    }

    private void OnSceneGUI()
    {
        HackingSystem system = (HackingSystem)target;
        SerializedProperty radiusProp = serializedObject.FindProperty("cyborgZoneRadius");
        SerializedProperty timeProp = serializedObject.FindProperty("cyborgTotalTime");
        SerializedProperty maxEnemyProp = serializedObject.FindProperty("cyborgMaxEnemyCount");

        if (radiusProp == null)
            return;

        Vector3 center = system.transform.position;
        float radius = radiusProp.floatValue;
        Vector3 planeNormal = Vector3.forward;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        Handles.color = ZoneFillColor;
        Handles.DrawSolidDisc(center, planeNormal, radius);

        Handles.color = ZoneLineColor;
        Handles.DrawWireDisc(center, planeNormal, radius);

        EditorGUI.BeginChangeCheck();
        Vector3 radiusHandlePos = center + Vector3.right * radius;
        float handleSize = HandleUtility.GetHandleSize(center) * 0.08f;
        var fmh_37_13_639179012068004572 = Quaternion.identity; Vector3 newHandlePos = Handles.FreeMoveHandle(
            radiusHandlePos,
            handleSize,
            Vector3.zero,
            Handles.DotHandleCap
        );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(system, "Change Cyborg Zone Radius");
            float newRadius = Vector2.Distance(
                new Vector2(center.x, center.y),
                new Vector2(newHandlePos.x, newHandlePos.y)
            );
            radiusProp.floatValue = Mathf.Clamp(newRadius, 0.5f, 15f);
            serializedObject.ApplyModifiedProperties();
        }

        string label =
            $"Cyborg Zone\nR={radiusProp.floatValue:F2}\n" +
            $"T={(timeProp != null ? timeProp.floatValue : 0f):F0}s  MaxEnemy={(maxEnemyProp != null ? maxEnemyProp.intValue : 0)}";
        Handles.Label(center + Vector3.up * (radius + 0.35f), label, EditorStyles.whiteBoldLabel);

        DrawNearbyHackableRanges(center, radius);
    }

    private static void DrawNearbyHackableRanges(Vector3 zoneCenter, float zoneRadius)
    {
        HackableObject[] hackables = Object.FindObjectsOfType<HackableObject>();
        if (hackables.Length == 0)
            return;

        Handles.color = HackRangeColor;

        foreach (HackableObject hackable in hackables)
        {
            if (hackable == null)
                continue;

            float distance = Vector2.Distance(
                new Vector2(zoneCenter.x, zoneCenter.y),
                new Vector2(hackable.transform.position.x, hackable.transform.position.y)
            );

            if (distance > zoneRadius + 8f)
                continue;

            SerializedObject so = new SerializedObject(hackable);
            SerializedProperty hackRangeProp = so.FindProperty("hackRange");
            float hackRange = hackRangeProp != null ? hackRangeProp.floatValue : 3f;

            Handles.DrawWireDisc(hackable.transform.position, Vector3.forward, hackRange);
            Handles.Label(
                hackable.transform.position + Vector3.up * 0.25f,
                $"E Range {hackRange:F1}",
                EditorStyles.miniLabel
            );
        }
    }
}

[CustomEditor(typeof(HackableObject))]
public class HackableObjectEditor : Editor
{
    private void OnSceneGUI()
    {
        HackableObject hackable = (HackableObject)target;
        SerializedProperty hackRangeProp = serializedObject.FindProperty("hackRange");
        if (hackRangeProp == null)
            return;

        Vector3 center = hackable.transform.position;
        float hackRange = hackRangeProp.floatValue;
        float zoneRadius = HackingZoneGizmoDraw.ResolveCyborgZoneRadius();

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        Handles.color = new Color(1f, 0.92f, 0.2f, 0.9f);
        Handles.DrawWireDisc(center, Vector3.forward, hackRange);

        Handles.color = new Color(0f, 0.85f, 0.95f, 0.12f);
        Handles.DrawSolidDisc(center, Vector3.forward, zoneRadius);

        Handles.color = new Color(0f, 0.85f, 0.95f, 0.95f);
        Handles.DrawWireDisc(center, Vector3.forward, zoneRadius);

        Handles.Label(
            center + Vector3.up * (zoneRadius + 0.35f),
            $"Cyborg Zone R={zoneRadius:F2}\nE Range={hackRange:F1}",
            EditorStyles.whiteBoldLabel
        );
    }
}
