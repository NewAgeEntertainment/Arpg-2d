#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy_Stats))]
public class EnemyStatsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Enemy_Stats stats = (Enemy_Stats)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Apply Level Scaling Now"))
        {
            stats.ApplyLevelScaling();
            Debug.Log("Applied scaling in editor.");
        }

        if (GUILayout.Button("Reset Name"))
        {
            stats.gameObject.name = stats.name;
        }

        if (GUI.changed)
            EditorUtility.SetDirty(stats);
    }
}
#endif

