#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NPCDialogueData))]
public class NPCDialogueDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var data = (NPCDialogueData)target;

        DrawProperty("dialogueVersion");
        DrawProperty("npcName");
        DrawProperty("adlibClips");
        DrawProperty("nodes");

        // Start node popup (unchanged)
        var startProp = serializedObject.FindProperty("startNodeIndex");
        int current = startProp.intValue;
        var options = BuildNodeLabels(data);
        int[] map = BuildNodeMap(data);
        int displayIndex = 0;
        for (int i = 0; i < map.Length; i++)
            if (map[i] == current) { displayIndex = i; break; }

        // After Conversation
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("After Conversation", EditorStyles.boldLabel);
        DrawProperty("postAction");

        var paProp = serializedObject.FindProperty("postAction");
        var pa = (NPCDialogueData.PostConversationMode)paProp.enumValueIndex;

        switch (pa)
        {
            case NPCDialogueData.PostConversationMode.SwitchToNewConversation:
                DrawProperty("nextDialogue");
                break;

            case NPCDialogueData.PostConversationMode.SetActiveOnGameObject:
                // <<< SHOW THE INDEX FIELD >>>
                DrawProperty("postSetActiveTargetIndex");
                DrawProperty("postSetActiveValue");
                EditorGUILayout.HelpBox(
                    "Index refers to the NPC's DialogueInteractable.sceneTargets array in the scene.",
                    MessageType.Info);
                break;

            case NPCDialogueData.PostConversationMode.SetBoolFlag:
                DrawProperty("postFlagName");
                DrawProperty("postFlagValue");
                break;
            }
        }


    private void DrawProperty(string name)
    {
        var p = serializedObject.FindProperty(name);
        if (p != null) EditorGUILayout.PropertyField(p, true);
    }

    private GUIContent[] BuildNodeLabels(NPCDialogueData data)
    {
        if (data.nodes == null || data.nodes.Length == 0)
            return new[] { new GUIContent("(no nodes)") };

        var arr = new GUIContent[data.nodes.Length];
        for (int i = 0; i < data.nodes.Length; i++)
            arr[i] = new GUIContent($"[{i}] {FirstWords(data.nodes[i]?.npcLine, 3)}");
        return arr;
    }

    private int[] BuildNodeMap(NPCDialogueData data)
    {
        int n = data.nodes == null ? 0 : data.nodes.Length;
        var map = new int[n];
        for (int i = 0; i < n; i++) map[i] = i;
        return map;
    }

    private static string FirstWords(string s, int count)
    {
        if (string.IsNullOrEmpty(s)) return "(empty)";
        var parts = s.Split(new[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        int take = Mathf.Min(count, parts.Length);
        string joined = string.Join(" ", parts, 0, take);
        if (parts.Length > take) joined += " …";
        return joined;
    }
}
#endif
