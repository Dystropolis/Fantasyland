using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class SceneLoadTrigger : MonoBehaviour
{
    [Header("Target Scene")]
    public SceneReference targetScene;

    [Header("Who can trigger")]
    public LayerMask playerLayerMask;  // pick the Player layer here
    public bool requirePlayerTag = false;

    [Header("Loading Screen (optional)")]
    public GameObject loadingScreenPrefab;      // prefab or scene object
    public bool makeLoadingScreenPersistent = true;

    [Header("Behavior")]
    public bool oneShot = true;

    private bool isLoading = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isLoading && oneShot) return;

        // Layer gate
        if ((playerLayerMask.value & (1 << other.gameObject.layer)) == 0) return;
        if (requirePlayerTag && !other.CompareTag("Player")) return;

        if (targetScene == null || !targetScene.HasValue)
        {
            Debug.LogWarning("[SceneLoadTrigger] No scene selected.");
            return;
        }

        StartCoroutine(LoadSceneRoutine(targetScene.SceneName));
    }

    IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        GameObject screenInstance = null;
        if (loadingScreenPrefab)
        {
            if (makeLoadingScreenPersistent)
            {
                screenInstance = Instantiate(loadingScreenPrefab);
                DontDestroyOnLoad(screenInstance);
                screenInstance.SetActive(true);
            }
            else
            {
                // if a scene object, just show it
                loadingScreenPrefab.SetActive(true);
                screenInstance = loadingScreenPrefab;
            }
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneLoadTrigger] Empty scene name.");
            yield break;
        }

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (op == null)
        {
            Debug.LogError($"[SceneLoadTrigger] Failed to start loading '{sceneName}'. Is it in Build Settings?");
            yield break;
        }

        while (!op.isDone) yield return null;

        if (screenInstance && makeLoadingScreenPersistent)
        {
            // give the new scene one frame to initialize then clean up
            yield return null;
            Destroy(screenInstance);
        }
    }
}
