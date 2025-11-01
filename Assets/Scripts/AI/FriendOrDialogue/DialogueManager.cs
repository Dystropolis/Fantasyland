using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI / Player")]
    public DialogueUI ui;
    public PlayerController playerController;
    public PlayerHealth playerHealth;

    private NPCDialogueData current;
    private DialogueInteractable interactor;
    private ActorAI npcAI;
    private AudioSource npcVoiceSource;
    private int nodeIndex = -1;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool IsOpen => current != null;

    public void StartDialogue(
        NPCDialogueData data,
        Transform npcRoot = null,
        Transform npcHead = null,
        ActorAI npcAI = null,
        DialogueInteractable owner = null,
        AudioSource voiceSource = null)
    {
        if (!ui || data == null || data.nodes == null || data.nodes.Length == 0) return;

        current = data;
        interactor = owner;
        this.npcAI = npcAI;
        npcVoiceSource = voiceSource;

        if (npcAI)
        {
            npcAI.voiceSource = npcVoiceSource;
            npcAI.SetDialogueMode(true);
        }

        LockControls(true);
        ShowCursor(true);
        nodeIndex = Mathf.Clamp(data.startNodeIndex, 0, data.nodes.Length - 1);
        ShowNode(nodeIndex);
    }

    void ShowNode(int idx)
    {
        if (current == null || idx < 0 || idx >= current.nodes.Length) { EndDialogue(); return; }

        var node = current.nodes[idx];
        nodeIndex = idx;

        float? cps = null;
        if (npcVoiceSource && node.voiceClip)
        {
            npcVoiceSource.Stop();
            npcVoiceSource.clip = node.voiceClip;
            npcVoiceSource.Play();
            float charsPerSec = node.npcLine.Length / Mathf.Max(0.05f, node.voiceClip.length);
            cps = Mathf.Clamp(charsPerSec, ui.charSpeedClamp.x, ui.charSpeedClamp.y);
        }

        ui.Show(current.npcName, node.npcLine,
            node.responses?.Length > 0 ? node.responses : new[] { new DialogueResponse { text = "Goodbye", isExitButton = true } },
            OnChooseResponse, cps);
    }

    void OnChooseResponse(int idx)
    {
        var node = current.nodes[nodeIndex];
        if (idx < 0 || node.responses == null || idx >= node.responses.Length) { EndDialogue(); return; }

        var r = node.responses[idx];
        if (r.setFlagOnClick && !string.IsNullOrEmpty(r.flagName))
            DialogueFlags.Set(r.flagName, r.flagValue);

        ExecuteAction(r);
        if (r.isExitButton || r.nextNode < 0) EndDialogue();
        else ShowNode(r.nextNode);
    }

    void ExecuteAction(DialogueResponse r)
    {
        if (r.action == DialogueAction.BuyHealthPotion && playerHealth)
        {
            int cost = r.intParam <= 0 ? 5 : r.intParam;
            int heal = r.floatParam <= 0 ? 25 : Mathf.RoundToInt(r.floatParam);
            if (playerHealth.gemCount >= cost)
            {
                playerHealth.gemCount -= cost;
                playerHealth.Heal(heal);
            }
        }
    }

    public void EndDialogue()
    {
        ui?.Hide();
        npcVoiceSource?.Stop();
        npcAI?.ExitAllTalkModes();

        if (interactor?.dialogue && !interactor.dialogue.isRepeatable)
            interactor.MarkConversationConsumed(interactor.dialogue.dialogueVersion);

        ApplyPostConversation(interactor, current);

        current = null; interactor = null; npcAI = null; npcVoiceSource = null;

        // snap now and next frame to kill any residual roll
        if (playerController)
        {
            try { playerController.SnapCameraUpright(); } catch { }
            StartCoroutine(SnapNextFrame());
        }

        ShowCursor(false);
        LockControls(false);
    }

    private System.Collections.IEnumerator SnapNextFrame()
    {
        yield return null; // one frame
        try { playerController?.SnapCameraUpright(); } catch { }
    }

    void ApplyPostConversation(DialogueInteractable npc, NPCDialogueData data)
    {
        if (!npc || !data) return;

        var mode = npc.overridePostAction ? npc.postActionOverride : data.postAction;

        switch (mode)
        {
            case NPCDialogueData.PostConversationMode.None_AdlibOnly:
                break;

            case NPCDialogueData.PostConversationMode.RepeatSameConversation:
                npc.dialogue = data;
                break;

            case NPCDialogueData.PostConversationMode.SwitchToNewConversation:
                var newDialogue = npc.overridePostAction ? npc.nextDialogueOverride : data.nextDialogue;
                if (newDialogue) npc.dialogue = newDialogue;
                break;

            case NPCDialogueData.PostConversationMode.SetActiveOnGameObject:
                var list = npc.postActionSceneTargets;
                if (list != null && list.Length > 0)
                {
                    int index = npc.overridePostAction ? npc.postSetActiveTargetIndex : data.postSetActiveTargetIndex;
                    bool value = npc.overridePostAction ? npc.postSetActiveValue : data.postSetActiveValue;
                    var go = list[Mathf.Clamp(index, 0, list.Length - 1)];
                    if (go) go.SetActive(value);
                }
                break;

            case NPCDialogueData.PostConversationMode.SetBoolFlag:
                string flagName = npc.overridePostAction ? npc.postFlagNameOverride : data.postFlagName;
                bool flagValue = npc.overridePostAction ? npc.postFlagValueOverride : data.postFlagValue;
                if (!string.IsNullOrEmpty(flagName)) DialogueFlags.Set(flagName, flagValue);
                break;
        }
    }

    void LockControls(bool locked)
    {
        if (playerController)
        {
            playerController.canControl = !locked;
            playerController.worldPaused = locked;
        }
    }

    void ShowCursor(bool show)
    {
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
    }
}
