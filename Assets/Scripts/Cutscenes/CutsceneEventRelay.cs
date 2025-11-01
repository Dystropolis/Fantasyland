using UnityEngine;

public class CutsceneEventRelay : MonoBehaviour
{
    [Header("Objects to Toggle (drag scene objects here)")]
    public GameObject[] objectsToToggle;

    // ---- Animation Events: index-based ----
    public void EnableObject(int index)
    {
        var go = Get(index);
        if (!go) { Debug.LogWarning($"[CutsceneEventRelay] EnableObject: no target at index {index}"); return; }
        go.SetActive(true);
        Debug.Log($"[CutsceneEventRelay] ENABLE index {index} -> {go.name}");
    }

    public void DisableObject(int index)
    {
        var go = Get(index);
        if (!go) { Debug.LogWarning($"[CutsceneEventRelay] DisableObject: no target at index {index}"); return; }
        go.SetActive(false);
        Debug.Log($"[CutsceneEventRelay] DISABLE index {index} -> {go.name}");
    }

    // ---- Animation Events: object param (works with GameObject OR Component) ----
    public void EnableSingle(UnityEngine.Object obj)
    {
        var go = ToGameObject(obj);
        if (!go) { Debug.LogWarning("[CutsceneEventRelay] EnableSingle: null/unsupported param"); return; }
        go.SetActive(true);
        Debug.Log($"[CutsceneEventRelay] ENABLE obj -> {go.name}");
    }

    public void DisableSingle(UnityEngine.Object obj)
    {
        var go = ToGameObject(obj);
        if (!go) { Debug.LogWarning("[CutsceneEventRelay] DisableSingle: null/unsupported param"); return; }
        go.SetActive(false);
        Debug.Log($"[CutsceneEventRelay] DISABLE obj -> {go.name}");
    }

    // ---- Cutscene flow helpers (optional) ----
    public void EndCutscene() => CutsceneManager.Instance?.EndCutscene();
    public void MoveCutsceneToPlayer() => CutsceneManager.Instance?.Event_MoveCutsceneToSavedPlayerPose();

    // ---- helpers ----
    GameObject Get(int i)
    {
        if (objectsToToggle == null || i < 0 || i >= objectsToToggle.Length) return null;
        return objectsToToggle[i];
    }

    static GameObject ToGameObject(UnityEngine.Object obj)
    {
        if (!obj) return null;
        if (obj is GameObject go) return go;
        if (obj is Component c) return c.gameObject;   // <-- key fix: supports dragging Components
        return null;
    }
}
