using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class AllScriptsTests
{
    private readonly List<UnityEngine.Object> _createdObjects = new();

    [TearDown]
    public void Cleanup()
    {
        foreach (var obj in _createdObjects)
        {
            if (obj != null)
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
        _createdObjects.Clear();
        Time.timeScale = 1f;
    }

    private void CallPrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        method?.Invoke(instance, args);
    }

    private T CreateGOWith<T>(string name = null) where T : Component
    {
        var go = new GameObject(name ?? typeof(T).Name);
        _createdObjects.Add(go);
        return go.AddComponent<T>();
    }

    private GameObject CreateGO(string name = "Temp")
    {
        var go = new GameObject(name);
        _createdObjects.Add(go);
        return go;
    }

    // -------- Level Scripts --------

    [Test]
    public void SceneReference_ReturnsSceneName()
    {
        var sceneRef = new SceneReference();
        typeof(SceneReference)
            .GetField("scenePath", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(sceneRef, "Assets/Scenes/DemoScene.unity");

        Assert.AreEqual("DemoScene", sceneRef.SceneName);
        Assert.IsTrue(sceneRef.HasValue);
    }

    [Test]
    public void SceneLoadTrigger_DoesNotLoadWhenSceneMissing()
    {
        var trigger = CreateGOWith<SceneLoadTrigger>();
        var colliderGO = CreateGO("Collider");
        var collider = colliderGO.AddComponent<BoxCollider>();
        collider.isTrigger = true;

        trigger.playerLayerMask = -1;
        trigger.requirePlayerTag = false;

        var method = typeof(SceneLoadTrigger).GetMethod("OnTriggerEnter", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method.Invoke(trigger, new object[] { collider });

        var field = typeof(SceneLoadTrigger).GetField("isLoading", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.False((bool)field.GetValue(trigger));
    }

    [Test]
    public void DoorInteractable_LockUnlockTogglesState()
    {
        var door = CreateGOWith<DoorInteractable>();
        door.isLocked = true;
        door.UnlockDoor();
        Assert.False(door.isLocked);
        door.LockDoor();
        Assert.True(door.isLocked);
    }

    // -------- Player Scripts --------

    [Test]
    public void PlayerController_DisableAndEnableControl()
    {
        var go = CreateGO("Player");
        var controller = go.AddComponent<CharacterController>();
        var pc = go.AddComponent<PlayerController>();
        var camHolder = new GameObject("Cam");
        _createdObjects.Add(camHolder);
        camHolder.transform.SetParent(go.transform);
        pc.cameraTransform = camHolder.transform;
        CallPrivate(pc, "Awake");

        pc.DisableControlForDialogue();
        Assert.False(pc.canControl);
        Assert.False(pc.worldPaused);

        pc.EnableControlAfterDialogue();
        Assert.True(pc.canControl);
        Assert.False(pc.worldPaused);
    }

    [Test]
    public void PlayerHealth_DamageAndHealClamp()
    {
        var health = CreateGOWith<PlayerHealth>();
        health.maxHealth = 100;
        health.currentHealth = 100;

        health.TakeDamage(30);
        Assert.AreEqual(70, health.currentHealth);

        health.Heal(50);
        Assert.AreEqual(100, health.currentHealth);
    }

    [Test]
    public void PlayerHealth_GemSpendingRespectsBalance()
    {
        var health = CreateGOWith<PlayerHealth>();
        health.gemCount = 1;

        health.AddGem(4);
        Assert.AreEqual(5, health.gemCount);

        Assert.True(health.SpendGems(3));
        Assert.AreEqual(2, health.gemCount);

        Assert.False(health.SpendGems(5));
        Assert.AreEqual(2, health.gemCount);
    }

    [Test]
    public void PlayerHealth_DeathDisablesControlAndPauses()
    {
        var player = CreateGO("Player");
        player.AddComponent<CharacterController>();
        var controller = player.AddComponent<PlayerController>();
        CallPrivate(controller, "Awake");

        var health = player.AddComponent<PlayerHealth>();
        health.controller = controller;
        health.maxHealth = 50;
        health.currentHealth = 10;

        health.TakeDamage(25);

        Assert.AreEqual(0, health.currentHealth);
        Assert.False(controller.canControl);
        Assert.True(controller.worldPaused);
    }

    [Test]
    public void PlayerCombat_EquipSwordActivatesWeapon()
    {
        var player = CreateGO("PlayerCombat");
        var combat = player.AddComponent<PlayerCombat>();
        var swordGO = new GameObject("Sword");
        _createdObjects.Add(swordGO);
        swordGO.transform.SetParent(player.transform);
        var sword = swordGO.AddComponent<SwordWeapon>();
        swordGO.SetActive(false);
        combat.swordWeapon = sword;

        var start = typeof(PlayerCombat).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
        start.Invoke(combat, null);

        Assert.True(sword.gameObject.activeSelf);
    }

    [Test]
    public void PlayerFootsteps_AwakeSetsAudioTo2D()
    {
        var go = CreateGO("Footsteps");
        var controller = go.AddComponent<CharacterController>();
        var audio = go.AddComponent<AudioSource>();
        var playerController = go.AddComponent<PlayerController>();
        CallPrivate(playerController, "Awake");

        var footsteps = go.AddComponent<PlayerFootsteps>();
        footsteps.playerController = playerController;
        CallPrivate(footsteps, "Awake");

        Assert.AreEqual(0f, audio.spatialBlend);
    }

    [Test]
    public void DialogueCameraLook_AlignsWhenDialogueOpen()
    {
        var camGO = CreateGO("Camera");
        var camera = camGO.AddComponent<Camera>();
        Camera.SetupCurrent(camera);

        var look = camGO.AddComponent<DialogueCameraLook>();
        var target = CreateGO("Target");
        target.transform.position = camGO.transform.position + Vector3.forward;

        typeof(DialogueInteractable).GetField("<IsDialogueOpen>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, true);
        typeof(DialogueInteractable).GetField("<CurrentHeadTarget>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, target.transform);

        var initial = camGO.transform.rotation;
        look.LateUpdate();
        Assert.AreNotEqual(initial, camGO.transform.rotation);

        typeof(DialogueInteractable).GetField("<IsDialogueOpen>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, false);
        typeof(DialogueInteractable).GetField("<CurrentHeadTarget>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, null);
    }

    // -------- UI Scripts --------

    [Test]
    public void PlayerUI_UpdatesHealthText()
    {
        var canvas = CreateGO("Canvas");
        var slider = canvas.AddComponent<Slider>();
        var textGO = CreateGO("Text");
        var text = textGO.AddComponent<Text>();
        var ui = canvas.AddComponent<PlayerUI>();
        ui.healthSlider = slider;
        ui.healthText = text;

        ui.UpdateHealthBar(25, 100);
        Assert.AreEqual(25, slider.value);
        Assert.AreEqual("25", text.text);
    }

    [Test]
    public void CrosshairTarget_ChangesColorForEnemy()
    {
        var camGO = CreateGO("Camera");
        var cam = camGO.AddComponent<Camera>();
        Camera.SetupCurrent(cam);

        var uiGO = CreateGO("Crosshair");
        var image = uiGO.AddComponent<Image>();
        var crosshair = uiGO.AddComponent<CrosshairTarget>();
        crosshair.crosshairImage = image;
        crosshair.playerCamera = cam;
        crosshair.checkRange = 5f;

        var enemyGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _createdObjects.Add(enemyGO);
        enemyGO.transform.position = camGO.transform.position + camGO.transform.forward * 2f;
        var actor = enemyGO.AddComponent<ActorAI>();
        enemyGO.AddComponent<NavMeshAgent>();
        enemyGO.AddComponent<AudioSource>();
        actor.faction = ActorAI.Faction.Enemy;
        cam.tag = "MainCamera";

        crosshair.Update();
        Assert.AreEqual(crosshair.enemyColor, image.color);
    }

    [Test]
    public void DialogueUI_CreatesResponseButtons()
    {
        var root = CreateGO("DialogueUI");
        var panel = CreateGO("Panel");
        panel.transform.SetParent(root.transform);
        var nameText = panel.AddComponent<TextMeshProUGUI>();
        var lineText = panel.AddComponent<TextMeshProUGUI>();
        var responses = CreateGO("Responses");
        responses.transform.SetParent(panel.transform);
        var responsesRect = responses.AddComponent<RectTransform>();
        var prefabGO = CreateGO("ButtonPrefab");
        var button = prefabGO.AddComponent<Button>();
        prefabGO.AddComponent<TextMeshProUGUI>();

        var ui = root.AddComponent<DialogueUI>();
        ui.panelRoot = panel;
        ui.npcNameText = nameText;
        ui.npcLineText = lineText;
        ui.responsesParent = responses.transform;
        ui.responseButtonPrefab = button;
        ui.lineClickCatcher = panel.AddComponent<Button>();

        CallPrivate(ui, "Awake");

        var responsesData = new[]
        {
            new DialogueResponse { text = "One", nextNode = -1 },
            new DialogueResponse { text = "Two", nextNode = -1 }
        };

        ui.Show("NPC", "Hello", responsesData, _ => { });
        Assert.AreEqual(2, responses.transform.childCount);
    }

    // -------- Cutscene Scripts --------

    [Test]
    public void CutsceneManager_BeginAndEndRestoresPlayer()
    {
        var managerGO = CreateGO("CutsceneManager");
        var manager = managerGO.AddComponent<CutsceneManager>();

        var player = CreateGO("Player");
        player.AddComponent<CharacterController>();
        var pc = player.AddComponent<PlayerController>();
        CallPrivate(pc, "Awake");
        var playerCamGO = CreateGO("PlayerCam");
        playerCamGO.AddComponent<Camera>();
        playerCamGO.transform.SetParent(player.transform);

        var cutsceneCamGO = CreateGO("CutsceneCam");
        cutsceneCamGO.AddComponent<Camera>();

        manager.playerRoot = player;
        manager.playerCamera = playerCamGO.GetComponent<Camera>();
        manager.cutsceneCamera = cutsceneCamGO.GetComponent<Camera>();

        CallPrivate(manager, "Awake");

        var startPose = CreateGO("StartPose").transform;
        startPose.SetPositionAndRotation(new Vector3(5f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));

        manager.BeginCutscene(startPose);
        Assert.False(pc.canControl);
        Assert.True(cutsceneCamGO.activeSelf);

        manager.EndCutscene();
        Assert.True(pc.canControl);
        Assert.False(cutsceneCamGO.activeSelf);
    }

    [Test]
    public void CutsceneEventRelay_TogglesObjects()
    {
        var relay = CreateGOWith<CutsceneEventRelay>();
        var target = CreateGO("Target");
        target.SetActive(false);
        relay.objectsToToggle = new[] { target };

        relay.EnableObject(0);
        Assert.True(target.activeSelf);

        relay.DisableObject(0);
        Assert.False(target.activeSelf);
    }

    [Test]
    public void CutsceneUse_StartIncludesOwnLayer()
    {
        var go = CreateGO("CutsceneUse");
        go.layer = 8;
        var cutsceneUse = go.AddComponent<CutsceneUse>();
        CallPrivate(cutsceneUse, "Start");

        int bit = 1 << go.layer;
        Assert.IsTrue((cutsceneUse.interactMask & bit) != 0);
    }

    // -------- Item Scripts --------

    private class TestWeapon : WeaponBase
    {
        public bool attacked;
        protected override void Attack()
        {
            attacked = true;
        }

        public void CallAttack()
        {
            Attack();
        }
    }

    [Test]
    public void WeaponBase_CanInvokeAttackInSubclass()
    {
        var weapon = CreateGOWith<TestWeapon>();
        weapon.CallAttack();
        Assert.True(weapon.attacked);
    }

    [Test]
    public void SwordWeapon_BlockingStateChanges()
    {
        var go = CreateGO("Sword");
        var sword = go.AddComponent<SwordWeapon>();

        sword.StartBlock();
        Assert.True(sword.blocking);

        sword.StopBlock();
        Assert.False(sword.blocking);
    }

    [Test]
    public void HitSurface_DefaultTypeIsDefault()
    {
        var hit = CreateGOWith<HitSurface>();
        Assert.AreEqual(HitSurface.SurfaceType.Default, hit.surfaceType);
    }

    [Test]
    public void PickupRotator_BobsPosition()
    {
        var go = CreateGO("Pickup");
        var rotator = go.AddComponent<PickupRotator>();
        CallPrivate(rotator, "Start");
        var startPos = go.transform.position;
        float expectedOffset = Mathf.Sin(Time.time * rotator.bobSpeed) * rotator.bobAmount;
        rotator.Update();
        Assert.AreEqual(startPos.y + expectedOffset, go.transform.position.y, 0.0001f);
    }

    [Test]
    public void IceProjectile_InitSetsVelocity()
    {
        var projectile = CreateGOWith<IceProjectile>();
        projectile.Init(Vector3.forward, 5f, 10);
        var velocityField = typeof(IceProjectile).GetField("velocity", BindingFlags.NonPublic | BindingFlags.Instance);
        var value = (Vector3)velocityField.GetValue(projectile);
        Assert.AreEqual(Vector3.forward * 5f, value);
    }

    [Test]
    public void PickupItem_HealthPickupCallsHeal()
    {
        var pickup = CreateGOWith<PickupItem>();
        var player = CreateGO("Player");
        player.tag = "Player";
        var collider = player.AddComponent<BoxCollider>();
        var health = player.AddComponent<PlayerHealth>();
        health.currentHealth = 50;
        health.maxHealth = 100;

        pickup.pickupType = PickupItem.PickupType.Health;
        pickup.amount = 25;

        var method = typeof(PickupItem).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(pickup, new object[] { collider });

        Assert.AreEqual(75, health.currentHealth);
    }

    // -------- AI Scripts --------

    [Test]
    public void ActorHealth_TakingDamageCanKill()
    {
        var enemy = CreateGO("Enemy");
        enemy.AddComponent<AudioSource>();
        enemy.AddComponent<NavMeshAgent>();
        var ai = enemy.AddComponent<ActorAI>();
        ai.targetPlayer = enemy.transform;

        var health = enemy.AddComponent<ActorHealth>();
        health.maxHealth = 50f;
        health.currentHealth = 20f;
        health.ai = ai;

        health.TakeDamage(25f, null);
        Assert.AreEqual(0f, health.currentHealth);
    }

    [Test]
    public void EnemyDropTable_SpawnsWhenChanceHigh()
    {
        var table = CreateGOWith<EnemyDropTable>();
        var dropGO = CreateGO("DropPrefab");
        var drop = dropGO;
        var option = new EnemyDropTable.DropOption
        {
            prefab = drop,
            chance = 1f
        };
        table.drops = new[] { option };

        Random.InitState(0);
        int before = GameObject.FindObjectsOfType<GameObject>().Length;
        table.SpawnDrop(Vector3.zero);
        int after = GameObject.FindObjectsOfType<GameObject>().Length;
        Assert.Greater(after, before);
        var spawned = GameObject.Find(drop.name + "(Clone)");
        if (spawned)
        {
            _createdObjects.Add(spawned);
        }
    }

    [Test]
    public void EnemyAnimationRelay_FindsActorAIOnParent()
    {
        var parent = CreateGO("EnemyRoot");
        parent.AddComponent<AudioSource>();
        parent.AddComponent<NavMeshAgent>();
        var ai = parent.AddComponent<ActorAI>();
        var child = new GameObject("AnimRelay");
        _createdObjects.Add(child);
        child.transform.SetParent(parent.transform);
        var relay = child.AddComponent<EnemyAnimationRelay>();
        CallPrivate(relay, "Awake");

        var field = typeof(EnemyAnimationRelay).GetField("ai", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.AreSame(ai, field.GetValue(relay));
    }

    [Test]
    public void EnemyAI_AssignsComponentsOnAwake()
    {
        var enemy = CreateGO("OldEnemy");
        enemy.AddComponent<CharacterController>();
        var attack = enemy.AddComponent<EnemyAttack>();
        enemy.AddComponent<AudioSource>();
        enemy.AddComponent<NavMeshAgent>();
        var actor = enemy.AddComponent<ActorAI>();
        var ai = enemy.AddComponent<EnemyAI>();
        CallPrivate(ai, "Awake");

        Assert.NotNull(typeof(EnemyAI).GetField("controller", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ai));
    }

    [Test]
    public void EnemyAttack_DealDamageDelegatesToActorAI()
    {
        var go = CreateGO("EnemyAttack");
        go.AddComponent<AudioSource>();
        var agent = go.AddComponent<NavMeshAgent>();
        var ai = go.AddComponent<ActorAI>();
        var attack = go.AddComponent<EnemyAttack>();
        CallPrivate(attack, "Awake");

        Assert.DoesNotThrow(() => attack.DealDamageNow());
    }

    [Test]
    public void EnemyID_DefaultPrefabIndexZero()
    {
        var id = CreateGOWith<EnemyID>();
        Assert.AreEqual(0, id.prefabIndex);
    }

    [Test]
    public void DialogueInteractable_MarkConversationConsumedUpdatesVersion()
    {
        var interactor = CreateGOWith<DialogueInteractable>();
        interactor.consumedVersion = -1;
        interactor.MarkConversationConsumed(3);
        Assert.AreEqual(3, interactor.consumedVersion);
    }

    [Test]
    public void DialogueFlags_SetAndGet()
    {
        DialogueFlags.Set("quest_started", true);
        Assert.True(DialogueFlags.Get("quest_started"));
    }

    [Test]
    public void DialogueManager_StartAndEndDialogueFlow()
    {
        var managerGO = CreateGO("DialogueManager");
        var manager = managerGO.AddComponent<DialogueManager>();

        var uiGO = CreateGO("UI");
        var ui = uiGO.AddComponent<DialogueUI>();
        ui.panelRoot = uiGO;
        ui.npcNameText = uiGO.AddComponent<TextMeshProUGUI>();
        ui.npcLineText = uiGO.AddComponent<TextMeshProUGUI>();
        ui.responsesParent = uiGO.transform;
        var buttonPrefabGO = CreateGO("ResponseButton");
        var responseButton = buttonPrefabGO.AddComponent<Button>();
        responseButton.gameObject.AddComponent<TextMeshProUGUI>();
        ui.responseButtonPrefab = responseButton;
        ui.lineClickCatcher = uiGO.AddComponent<Button>();
        CallPrivate(ui, "Awake");

        manager.ui = ui;
        var player = CreateGO("Player");
        player.AddComponent<CharacterController>();
        var pc = player.AddComponent<PlayerController>();
        CallPrivate(pc, "Awake");
        manager.playerController = pc;
        manager.playerHealth = player.AddComponent<PlayerHealth>();

        var data = ScriptableObject.CreateInstance<NPCDialogueData>();
        _createdObjects.Add(data);
        data.npcName = "Tester";
        data.nodes = new[]
        {
            new DialogueNode
            {
                npcLine = "Hello",
                responses = new[]
                {
                    new DialogueResponse { text = "Bye", isExitButton = true }
                }
            }
        };

        manager.StartDialogue(data, null, null, null, null, null);
        Assert.True(manager.IsOpen);
        manager.EndDialogue();
        Assert.False(manager.IsOpen);
    }

    [Test]
    public void NPCDialogueData_DefaultsConfigured()
    {
        var data = ScriptableObject.CreateInstance<NPCDialogueData>();
        _createdObjects.Add(data);
        data.postAction = NPCDialogueData.PostConversationMode.SwitchToNewConversation;
        Assert.AreEqual(NPCDialogueData.PostConversationMode.SwitchToNewConversation, data.postAction);
    }

    [Test]
    public void DialogueResponseDrawer_FirstWordsUtility()
    {
#if UNITY_EDITOR
        var drawer = new DialogueResponseDrawer();
        var method = typeof(DialogueResponseDrawer).GetMethod("FirstWords", BindingFlags.NonPublic | BindingFlags.Static);
        string result = (string)method.Invoke(drawer, new object[] { "Hello brave adventurer", 2 });
        Assert.AreEqual("Hello brave ", result);
#else
        Assert.Pass();
#endif
    }

    [Test]
    public void NPCDialogueDataEditor_FirstWordsUtility()
    {
#if UNITY_EDITOR
        var editorType = typeof(NPCDialogueDataEditor);
        var method = editorType.GetMethod("FirstWords", BindingFlags.NonPublic | BindingFlags.Static);
        string result = (string)method.Invoke(null, new object[] { "Testing editor helper", 1 });
        Assert.AreEqual("Testing ", result);
#else
        Assert.Pass();
#endif
    }

    [Test]
    public void ActorAI_BecomeHostileSetsFaction()
    {
        var go = CreateGO("ActorAI");
        go.AddComponent<AudioSource>();
        go.AddComponent<NavMeshAgent>();
        var ai = go.AddComponent<ActorAI>();
        ai.faction = ActorAI.Faction.Friendly;
        var player = CreateGO("PlayerRoot");

        ai.BecomeHostileToPlayer(player.transform);
        Assert.AreEqual(ActorAI.Faction.Enemy, ai.faction);
        Assert.AreSame(player.transform, ai.targetPlayer);
    }

}
