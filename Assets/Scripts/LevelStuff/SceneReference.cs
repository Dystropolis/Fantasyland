using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// A robust reference to a scene:
/// - In the Editor: drag a SceneAsset OR pick from Build Settings via dropdown.
/// - At runtime: uses the serialized path/name (SceneAsset is editor-only).
/// </summary>
[Serializable]
public class SceneReference
{
    [SerializeField] private string scenePath;   // "Assets/Scenes/MyScene.unity"

#if UNITY_EDITOR
    [SerializeField] private SceneAsset sceneAsset; // Editor-only nice field
#endif

    public string ScenePath => scenePath;
    public string SceneName
    {
        get
        {
            if (string.IsNullOrEmpty(scenePath)) return string.Empty;
            int slash = scenePath.LastIndexOf('/');
            int dot = scenePath.LastIndexOf('.');
            if (slash < 0) slash = -1;
            if (dot < 0 || dot <= slash) dot = scenePath.Length;
            return scenePath.Substring(slash + 1, dot - slash - 1);
        }
    }

    public bool HasValue => !string.IsNullOrEmpty(scenePath);

#if UNITY_EDITOR
    public void SetScene(SceneAsset asset)
    {
        sceneAsset = asset;
        scenePath = asset ? AssetDatabase.GetAssetPath(asset) : string.Empty;
    }

    public SceneAsset GetSceneAsset() => sceneAsset;

    public bool IsInBuildSettings()
    {
        if (string.IsNullOrEmpty(scenePath)) return false;
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path == scenePath) return true;
        return false;
    }

    public void AddToBuildSettings()
    {
        if (string.IsNullOrEmpty(scenePath)) return;
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == scenePath) return;
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
#endif
}

#if UNITY_EDITOR
// Nice Inspector: lets you drag a SceneAsset OR pick from Build Settings
[CustomPropertyDrawer(typeof(SceneReference))]
public class SceneReferenceDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // 1 line for object field + (optionally) 1 line for warning + 1 line for Add button
        int lines = 1;

        var pathProp = property.FindPropertyRelative("scenePath");
        string path = pathProp.stringValue;

        bool showBuildWarning = !string.IsNullOrEmpty(path) && !IsInBuildSettings(path);
        if (showBuildWarning) lines += 2;

        return EditorGUIUtility.singleLineHeight * lines + 6f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var pathProp = property.FindPropertyRelative("scenePath");
        var assetProp = property.FindPropertyRelative("sceneAsset");

        EditorGUI.BeginProperty(position, label, property);
        var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        // Row 1: SceneAsset object field
        EditorGUI.BeginChangeCheck();
        var obj = EditorGUI.ObjectField(line, label, assetProp.objectReferenceValue, typeof(SceneAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            assetProp.objectReferenceValue = obj;
            pathProp.stringValue = obj ? AssetDatabase.GetAssetPath(obj) : string.Empty;
        }

        // Build settings warning & button
        string path = pathProp.stringValue;
        if (!string.IsNullOrEmpty(path) && !IsInBuildSettings(path))
        {
            line.y += EditorGUIUtility.singleLineHeight + 2f;
            EditorGUI.HelpBox(line, "Scene is not in Build Settings.", MessageType.Warning);

            line.y += EditorGUIUtility.singleLineHeight + 2f;
            if (GUI.Button(line, "Add to Build Settings"))
            {
                AddToBuildSettings(path);
            }
        }

        EditorGUI.EndProperty();
    }

    private static bool IsInBuildSettings(string scenePath)
    {
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path == scenePath) return true;
        return false;
    }

    private static void AddToBuildSettings(string scenePath)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == scenePath) return;
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
