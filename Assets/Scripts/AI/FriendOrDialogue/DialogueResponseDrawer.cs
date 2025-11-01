#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialogueResponse))]
public class DialogueResponseDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded, label, true);

        var lineH = EditorGUIUtility.singleLineHeight;
        var y = position.y + lineH + 2f;
        var fullW = position.width;

        var textProp = property.FindPropertyRelative("text");
        var nextNodeProp = property.FindPropertyRelative("nextNode");
        var isExitProp = property.FindPropertyRelative("isExitButton");
        var actionProp = property.FindPropertyRelative("action");
        var intParamProp = property.FindPropertyRelative("intParam");
        var floatParamProp = property.FindPropertyRelative("floatParam");

        if (property.isExpanded)
        {
            EditorGUI.PropertyField(new Rect(position.x, y, fullW, lineH), textProp);
            y += lineH + 2f;

            using (new EditorGUI.DisabledScope(isExitProp.boolValue))
            {
                int currentIndex = nextNodeProp.intValue;
                var (options, nodeIndexMap, displayIndex) = BuildNodeOptions(property, currentIndex);

                int picked = EditorGUI.Popup(new Rect(position.x, y, fullW, lineH),
                                             new GUIContent("Next Node"),
                                             displayIndex, options);
                y += lineH + 2f;

                if (picked >= 0 && picked < nodeIndexMap.Length)
                    nextNodeProp.intValue = nodeIndexMap[picked];
            }

            EditorGUI.PropertyField(new Rect(position.x, y, fullW, lineH), isExitProp);
            y += lineH + 2f;

            EditorGUI.PropertyField(new Rect(position.x, y, fullW, lineH), actionProp);
            y += lineH + 2f;

            EditorGUI.PropertyField(new Rect(position.x, y, fullW, lineH), intParamProp);
            y += lineH + 2f;

            EditorGUI.PropertyField(new Rect(position.x, y, fullW, lineH), floatParamProp);
            y += lineH + 2f;
        }
    }

    private (GUIContent[] options, int[] nodeIndexMap, int displayIndex) BuildNodeOptions(SerializedProperty responseProp, int currentValue)
    {
        var target = responseProp.serializedObject.targetObject as NPCDialogueData;
        if (target == null || target.nodes == null)
        {
            var opts = new[] { new GUIContent("End / Exit (-1)") };
            var ids = new[] { -1 };
            return (opts, ids, 0);
        }

        var guiList = new System.Collections.Generic.List<GUIContent>();
        var idList = new System.Collections.Generic.List<int>();

        guiList.Add(new GUIContent("End / Exit (-1)"));
        idList.Add(-1);

        for (int i = 0; i < target.nodes.Length; i++)
        {
            string preview = FirstWords(target.nodes[i]?.npcLine, 3);
            guiList.Add(new GUIContent($"[{i}] {preview}"));
            idList.Add(i);
        }

        int displayIndex = idList.IndexOf(currentValue);
        if (displayIndex < 0) displayIndex = 0;

        return (guiList.ToArray(), idList.ToArray(), displayIndex);
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
