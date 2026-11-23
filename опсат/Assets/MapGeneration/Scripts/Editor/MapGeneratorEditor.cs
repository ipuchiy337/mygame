using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var gen = (MapGenerator)target;

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Сгенерировать", GUILayout.Height(30))) { gen.Generate(); MarkDirty(gen); }
            if (GUILayout.Button("Очистить", GUILayout.Height(30))) { gen.Clear(); MarkDirty(gen); }
        }
    }

    static void MarkDirty(MapGenerator gen)
    {
        if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(gen.gameObject.scene);
    }
}
