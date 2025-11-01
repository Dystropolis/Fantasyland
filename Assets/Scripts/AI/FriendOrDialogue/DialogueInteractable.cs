using System;
using UnityEngine;

public class DialogueInteractable : MonoBehaviour
{
    [Header("Dialogue")]
    public NPCDialogueData dialogue;
    public float interactDistance = 3f;
    public LayerMask interactMask = ~0;

    [Header("NPC Refs")]
    public Transform npcRoot;
    public Transform npcHead;
    public ActorAI npcAI;
    public AudioSource voiceSource;

    [Header("Conversation Gating")]
    public int consumedVersion = -1;

    [Header("Ad-lib")]
    public float adlibCooldown = 2f;
    private float nextAdlibTime;
    private Coroutine adlibCR;

    [Header("Scene References for Post-Conversation Actions")]
    [Tooltip("Scene objects referenced by NPCDialogueData or overrides.")]
    public GameObject[] postActionSceneTargets;

    [Header("Optional Overrides (Scene-level)")]
    [Tooltip("If true, overrides ScriptableObject's post-action behavior.")]
    public bool overridePostAction = false;
    public NPCDialogueData.PostConversationMode postActionOverride = NPCDialogueData.PostConversationMode.None_AdlibOnly;

    [Tooltip("If SwitchToNewConversation override is used.")]
    public NPCDialogueData nextDialogueOverride;

    [Tooltip("If SetActiveOnGameObject override is used.")]
    public int postSetActiveTargetIndex = 0;
    public bool postSetActiveValue = true;

    [Tooltip("If SetBoolFlag override is used.")]
    public string postFlagNameOverride;
    public bool postFlagValueOverride = true;

    private Camera cam;
    private bool lastDialogueOpen = false;

    public static bool IsDialogueOpen { get; private set; } = false;
    public static Transform CurrentHeadTarget { get; private set; } = null;
    public static event Action<bool> OnDialogueModeChanged; // true = open, false = closed

    void Start()
    {
        cam = Camera.main;
        if (!npcRoot) npcRoot = transform;
        if (!npcAI) npcAI = GetComponentInParent<ActorAI>();
        if (!voiceSource) voiceSource = GetComponentInParent<AudioSource>();
    }

    void Update()
    {
        var managerOpen = DialogueManager.Instance && DialogueManager.Instance.IsOpen;

        // detect dialogue open/close
        if (managerOpen != lastDialogueOpen)
        {
            lastDialogueOpen = managerOpen;
            IsDialogueOpen = managerOpen;
            if (!managerOpen) CurrentHeadTarget = null;
            OnDialogueModeChanged?.Invoke(managerOpen);
        }

        if (managerOpen) return;

        if (Input.GetKeyDown(KeyCode.E) && CanTalkByLook())
        {
            if (!dialogue) return;

            if (!dialogue.isRepeatable && consumedVersion == dialogue.dialogueVersion &&
                npcAI && npcAI.faction == ActorAI.Faction.Friendly)
            {
                TryPlayAdlib();
                return;
            }

            DialogueManager.Instance.StartDialogue(dialogue, npcRoot, npcHead, npcAI, this, voiceSource);
            IsDialogueOpen = true;
            CurrentHeadTarget = npcHead;
            OnDialogueModeChanged?.Invoke(true);
        }
    }

    void TryPlayAdlib()
    {
        if (Time.time < nextAdlibTime || dialogue.adlibClips == null || dialogue.adlibClips.Length == 0) return;
        var src = voiceSource ? voiceSource : GetComponentInParent<AudioSource>();
        if (!src) return;

        var clip = dialogue.adlibClips[UnityEngine.Random.Range(0, dialogue.adlibClips.Length)];
        if (!clip) return;

        nextAdlibTime = Time.time + adlibCooldown;
        if (adlibCR != null) StopCoroutine(adlibCR);
        adlibCR = StartCoroutine(AdlibRoutine(src, clip));
    }

    System.Collections.IEnumerator AdlibRoutine(AudioSource src, AudioClip clip)
    {
        if (npcAI) { npcAI.voiceSource = src; npcAI.SetAdlibMode(true, cam ? cam.transform : null); }

        src.ignoreListenerPause = true;
        src.loop = false;
        src.Stop();
        src.clip = clip;
        src.Play();

        while (src.isPlaying)
            yield return null;

        if (npcAI) { npcAI.ExitAllTalkModes(); npcAI.voiceSource = null; }
        adlibCR = null;
    }

    public void MarkConversationConsumed(int version) => consumedVersion = version;

    bool CanTalkByLook()
    {
        if (!cam) return false;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == transform) return true;
            if (npcRoot && hit.transform.IsChildOf(npcRoot)) return true;
        }
        return false;
    }
}
