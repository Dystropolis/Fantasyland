using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/NPC Dialogue", fileName = "NewNPCDialogue")]
public class NPCDialogueData : ScriptableObject
{
    [Header("Post-Dialogue Ad-libs")]
    [Tooltip("Friendly chatter or ad-libs that play after the main dialogue has already been had.")]
    public AudioClip[] adlibClips;

    [Tooltip("Whether this dialogue has already been played by the player.")]
    [HideInInspector] public bool hasBeenPlayed = false;

    [Header("Designer Versioning")]
    [Tooltip("Bump this when you change this SO to re-enable the NPC's conversation.")]
    public int dialogueVersion = 1;

    [Header("Basic")]
    public string npcName = "NPC";
    public int startNodeIndex = 0;

    [Header("Conversation")]
    public DialogueNode[] nodes;

    [Header("Repeatability")]
    [Tooltip("If true, this conversation can be started every time (e.g., shops). If false, it will only play once unless the version is bumped.")]
    public bool isRepeatable = false;

    // ========= NEW: Post-conversation behavior =========
    public enum PostConversationMode
    {
        None_AdlibOnly,          // do nothing; future talks ad-lib (if not repeatable)
        RepeatSameConversation,  // keep this SO
        SwitchToNewConversation, // swap to nextDialogue
        SetActiveOnGameObject,   // toggle a scene object via DialogueInteractable.sceneTargets[index]
        SetBoolFlag              // set a named global bool
    }

    [Header("After Conversation")]
    public PostConversationMode postAction = PostConversationMode.None_AdlibOnly;

    [Tooltip("Used if postAction == SwitchToNewConversation")]
    public NPCDialogueData nextDialogue;

    [Tooltip("Used if postAction == SetActiveOnGameObject. Index into DialogueInteractable.sceneTargets on the NPC in scene.")]
    public int postSetActiveTargetIndex = 0;

    public bool postSetActiveValue = true;

    [Tooltip("Used if postAction == SetBoolFlag")]
    public string postFlagName;
    public bool postFlagValue = true;
}

[Serializable]
public class DialogueNode
{
    [TextArea(2, 5)] public string npcLine;

    [Header("Voice")]
    public AudioClip voiceClip; // plays while this line types

    [Header("Responses")]
    public DialogueResponse[] responses;
}

public enum DialogueAction { None, BuyHealthPotion }

[Serializable]
public class DialogueResponse
{
    public string text = "Response";
    [Tooltip("Next node index; -1 = end/exit dialogue")]
    public int nextNode = -1;

    [Tooltip("If true, this button exits immediately.")]
    public bool isExitButton = false;

    public DialogueAction action = DialogueAction.None;
    public int intParam = 0;
    public float floatParam = 0f;

    // ========= NEW: per-button flag set =========
    [Header("Optional: Set Bool Flag On Click")]
    public bool setFlagOnClick = false;
    public string flagName;
    public bool flagValue = true;
}
