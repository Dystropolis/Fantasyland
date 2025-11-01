using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class DialogueUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject panelRoot;

    [Header("Text")]
    public TMP_Text npcNameText;
    public TMP_Text npcLineText;

    [Header("Responses")]
    public Transform responsesParent;       // Vertical Layout Group + ContentSizeFitter
    public Button responseButtonPrefab;

    [Header("Typewriter")]
    [Tooltip("Fallback chars/sec if no audio clip is present.")]
    public float defaultCharsPerSecond = 40f;
    [Tooltip("Clamp autospeed from audio length.")]
    public Vector2 charSpeedClamp = new Vector2(20f, 80f);

    [Header("Click-to-skip")]
    [Tooltip("Transparent Button covering the NPC line area; hook its OnClick to OnLineClicked.")]
    public Button lineClickCatcher;

    private readonly List<Button> _spawned = new();
    private Action<int> _onChoose;
    private Coroutine _typeCR;
    private bool _typing = false;
    private string _fullLineCached = "";

    void Awake()
    {
        if (panelRoot) panelRoot.SetActive(false);
        if (lineClickCatcher)
        {
            lineClickCatcher.onClick.RemoveAllListeners();
            lineClickCatcher.onClick.AddListener(OnLineClicked);
        }
    }

    // Add inside DialogueUI (e.g., below existing methods)
    void Update()
    {
        if (_typing && Input.GetMouseButtonDown(0))
            OnLineClicked();
    }

    public void Show(string npcName, string fullLine, DialogueResponse[] responses, Action<int> onChoose, float? overrideCPS = null)
    {
        if (!panelRoot) return;
        panelRoot.SetActive(true);

        if (npcNameText) npcNameText.text = npcName;

        // typewriter
        _fullLineCached = fullLine ?? "";
        if (_typeCR != null) StopCoroutine(_typeCR);
        npcLineText.text = "";
        float cps = overrideCPS ?? defaultCharsPerSecond;
        _typing = true;
        _typeCR = StartCoroutine(Typewriter(_fullLineCached, cps));

        // responses (initially disabled)
        ClearButtons();
        _onChoose = onChoose;
        for (int i = 0; i < responses.Length; i++)
        {
            var r = responses[i];
            var btn = Instantiate(responseButtonPrefab, responsesParent);
            _spawned.Add(btn);

            btn.interactable = false;              // still true, but hidden anyway
            btn.gameObject.SetActive(false);       // HIDE until typing is done or skipped

            var txt = btn.GetComponentInChildren<TMP_Text>(true);
            if (txt) txt.text = r.isExitButton && string.IsNullOrEmpty(r.text) ? "Exit" : r.text;

            int idx = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => { if (!_typing) _onChoose?.Invoke(idx); });
            btn.interactable = false; // gated until typing finished
        }
        StartCoroutine(RebuildNextFrame());
    }

    public void Hide()
    {
        if (_typeCR != null) StopCoroutine(_typeCR);
        _typeCR = null;
        _typing = false;
        if (panelRoot) panelRoot.SetActive(false);
        ClearButtons();
        _onChoose = null;
    }

    IEnumerator Typewriter(string text, float cps)
    {
        if (!npcLineText) yield break;
        npcLineText.text = "";
        if (string.IsNullOrEmpty(text))
        {
            _typing = false;
            SetResponsesInteractable(true);
            yield break;
        }

        float delay = 1f / Mathf.Max(1f, cps);
        for (int i = 0; i < text.Length; i++)
        {
            npcLineText.text += text[i];
            yield return new WaitForSecondsRealtime(delay);
        }

        _typing = false;
        RevealResponses();
    }

    public void OnLineClicked()
    {
        if (!_typing) return;
        if (_typeCR != null) StopCoroutine(_typeCR);
        _typeCR = null;
        _typing = false;
        if (npcLineText) npcLineText.text = _fullLineCached; // reveal instantly
        SetResponsesInteractable(true);
        _typing = false;
        if (npcLineText) npcLineText.text = _fullLineCached;
        RevealResponses();
    }

    private void SetResponsesInteractable(bool on)
    {
        foreach (var b in _spawned) if (b) b.interactable = on;
    }

    private void ClearButtons()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i])
            {
                _spawned[i].onClick.RemoveAllListeners();
                Destroy(_spawned[i].gameObject);
            }
        }
        _spawned.Clear();
    }

    private IEnumerator RebuildNextFrame()
    {
        yield return null;
        var rt = responsesParent as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void RevealResponses()
    {
        foreach (var b in _spawned)
        {
            if (!b) continue;
            b.gameObject.SetActive(true);
            b.interactable = true;
        }
        StartCoroutine(RebuildNextFrame());
    }
}
