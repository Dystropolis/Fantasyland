using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class ActorAI : MonoBehaviour
{
    public enum Faction { Friendly, Enemy }
    public enum EnemyType { Sword, Bow, Ice }
    public enum State { Idle, Roam, Patrol, Attack }

    [Header("Setup")]
    public Faction faction = Faction.Enemy;
    public EnemyType enemyType = EnemyType.Sword;
    [Tooltip("Assign the Player root Transform here.")]
    public Transform targetPlayer;

    [Header("Animation")]
    public Animator animator;

    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float chaseRange = 20f;

    [Header("Melee Distances")]
    public float attackHitDistance = 2f;
    public float attackWindupSlack = 0.5f;

    [Header("Ranged Distances")]
    public float desiredRange = 8f;
    public float desiredRangedDistBonus = 4f;

    [Header("Roam / Patrol")]
    public State startState = State.Idle;
    public float roamRadius = 5f;
    public float roamWaitTime = 2f;
    public Transform[] patrolPoints;

    [Header("Attack")]
    public float attackCooldown = 1.5f;
    public float attackAnimDuration = 0.8f;

    [Header("Audio / Footsteps")]
    public AudioSource audioSource;
    public AudioClip[] walkClips;
    public AudioClip[] runClips;
    public float walkStepInterval = 0.6f;
    public float runStepInterval = 0.35f;
    public AudioClip[] attackHitClips;
    public AudioClip[] attackMissClips;

    [Header("Perception (Enemies only)")]
    public float visionRange = 12f;          // head-based FOV range
    [Range(0, 180)] public float visionFOV = 60f; // half-angle in degrees
    public float closeSightRange = 2.5f;     // body-based short cone
    [Range(0, 180)] public float closeSightFOV = 160f; // half-angle
    public Transform headTransform;          // eye/head for LOS ray
    public float eyeRayDistance = 12f;

    [Header("Dialogue / Adlib")]
    public bool inDialogue = false;          // true while dialogue UI is open
    public bool inAdlib = false;             // true while ad-lib voice line should hold the actor
    [Tooltip("Voice source assigned by DialogueManager / Interactable so we can drive talk anims.")]
    public AudioSource voiceSource;

    [Header("Talk Overlay (Animator Layer)")]
    [Tooltip("Animator layer index for talk overlay (Override).")]
    public int talkLayerIndex = 1;
    public float talkLayerFadeInSpeed = 16f;
    public float talkLayerFadeOutSpeed = 16f;
    [Tooltip("Animator state names (on talk layer) to cycle while voice is playing.")]
    public string[] talkIdleStates;
    public float talkIdleMinInterval = 2f;
    public float talkIdleMaxInterval = 4f;

    [Header("Head Aim (optional)")]
    public Transform headBone;
    public bool useHeadAim = true;
    [Range(0, 85)] public float headYawLimit = 55f;
    [Range(0, 60)] public float headPitchLimit = 35f;
    public float headAimSpeed = 10f;

    // ----- runtime -----
    private State currentState;
    private NavMeshAgent agent;
    private EnemyAttack attack;

    private bool isChasing = false;
    private float lastAttackTime = -999f;
    private bool isAttacking = false;
    private float attackEndTime = 0f;

    private Vector3 roamCenter;
    private Vector3 roamDest;
    private float nextRoamTime;
    private int patrolIndex = 0;

    // perception (enemies only)
    private bool isAlerted = false;
    private bool canCurrentlySeePlayer = false;

    // footsteps
    private float nextStepTime = 0f;

    // talk overlay
    private float talkLayerWeight = 0f;
    private float nextTalkIdleTime = 0f;
    private string lastTalkState = null;

    // head aim neutral
    private Quaternion headNeutralLocal = Quaternion.identity;
    private bool headNeutralCached = false;

    // gizmos cache
    private Vector3 gizHeadPos, gizHeadDir;
    private bool gizHeadConeHasPlayer, gizHeadHasLOS;
    private Vector3 gizBodyPos, gizBodyDir;
    private bool gizBodyConeHasPlayer, gizBodyHasLOS;

    // Dialogue global override
    private bool dialogueOverride = false;
    private Faction factionBeforeDialogue;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        attack = GetComponent<EnemyAttack>();

        // Nav config
        agent.updateRotation = false;
        agent.updateUpAxis = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(20, 80);
        agent.autoBraking = false;
        agent.acceleration = Mathf.Max(agent.acceleration, 16f);

        audioSource = GetComponent<AudioSource>();
        if (audioSource) { audioSource.playOnAwake = false; audioSource.loop = false; }

        if (animator)
        {
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        roamCenter = transform.position;
        ChangeState(startState);
    }

    void Start()
    {
        CacheHeadNeutral();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        // Global dialogue freeze — belt & suspenders
        if (DialogueInteractable.IsDialogueOpen)
        {
            if (faction == Faction.Enemy)
            {
                if (agent) { agent.isStopped = true; agent.ResetPath(); }
                isAttacking = false;
                if (animator)
                {
                    animator.ResetTrigger("Attack");
                    animator.SetFloat("Speed", 0f);
                    animator.CrossFade("Idle", 0.05f, 0);
                }
                currentState = State.Idle;
            }

            // Keep facing the player and run talk overlay if applicable
            if (targetPlayer) FaceTowards(targetPlayer.position, 10f);
            TickTalkOverlay();
            return;
        }

        if (isAttacking && Time.time >= attackEndTime)
            isAttacking = false;

        // Ad-lib auto-exit when the voice clip finishes
        if (inAdlib && (!voiceSource || !voiceSource.isPlaying))
            ExitAllTalkModes();

        // Dialogue/ad-lib modes: stop nav, face player, voice-gated talk overlay
        if (inDialogue || inAdlib)
        {
            if (agent) { agent.isStopped = true; agent.ResetPath(); }
            if (animator) animator.SetFloat("Speed", 0f);
            if (targetPlayer) FaceTowards(targetPlayer.position, 10f);

            TickTalkOverlay(); // talk layer weight follows voiceSource.isPlaying
            return;
        }

        UpdatePerception();          // enemies only auto-alert here
        HandleStateTransitions();    // friendly stays on startState
        ApplySpeedByIntent();
        RunCurrentState();
        UpdateFacing();
        UpdateAnimatorAndFootsteps();
    }

    void LateUpdate()
    {
        if ((inDialogue || inAdlib) && useHeadAim && headBone && targetPlayer)
            AimHeadAt(targetPlayer.position, headYawLimit, headPitchLimit, headAimSpeed);
    }

    // ----- External flips -----
    public void BecomeHostileToPlayer(Transform playerRoot)
    {
        faction = Faction.Enemy;
        if (playerRoot) targetPlayer = playerRoot;
        if (agent) agent.isStopped = false;
        isAlerted = true;
    }

    public void SetDialogueMode(bool on)
    {
        inDialogue = on;
        if (agent) { agent.isStopped = on; if (on) agent.ResetPath(); }
        if (animator) animator.SetFloat("Speed", 0f);
        // talk overlay is gated purely by voiceSource.isPlaying in TickTalkOverlay
        if (!on) ClearTalkOverlayImmediate();
    }

    public void SetAdlibMode(bool on, Transform facePlayer = null)
    {
        inAdlib = on;
        if (agent) { agent.isStopped = on; if (on) agent.ResetPath(); }
        if (animator) animator.SetFloat("Speed", 0f);
        if (on && facePlayer) targetPlayer = facePlayer;
        if (!on) ClearTalkOverlayImmediate();
    }

    public void ExitAllTalkModes()
    {
        inDialogue = false;
        inAdlib = false;
        ClearTalkOverlayImmediate();
    }

    // ----- Perception (Enemies only) -----
    void UpdatePerception()
    {
        canCurrentlySeePlayer = false;

        // reset gizmos
        gizHeadPos = gizBodyPos = Vector3.zero;
        gizHeadDir = gizBodyDir = Vector3.forward;
        gizHeadConeHasPlayer = gizHeadHasLOS = false;
        gizBodyConeHasPlayer = gizBodyHasLOS = false;

        if (faction != Faction.Enemy) return;
        if (!targetPlayer) return;

        UpdateVision();

        if (canCurrentlySeePlayer)
            isAlerted = true;
    }

    void UpdateVision()
    {
        // Head cone LOS
        if (headTransform && targetPlayer)
        {
            Vector3 eyePos = headTransform.position;
            Vector3 toP = targetPlayer.position - eyePos;
            float dist = toP.magnitude;
            Vector3 dir = (dist > 0f) ? toP.normalized : Vector3.forward;

            gizHeadPos = eyePos;
            gizHeadDir = headTransform.forward;

            if (dist <= visionRange)
            {
                float ang = Vector3.Angle(headTransform.forward, dir);
                if (ang <= visionFOV)
                {
                    gizHeadConeHasPlayer = true;
                    if (Physics.Raycast(eyePos, dir, out RaycastHit hit, eyeRayDistance, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider.GetComponentInParent<PlayerController>() != null)
                        {
                            gizHeadHasLOS = true;
                            canCurrentlySeePlayer = true;
                        }
                    }
                }
            }
        }

        // Body close cone LOS (less strict, near-field)
        {
            Vector3 bodyPos = transform.position + Vector3.up * 1.6f;
            Vector3 toP = targetPlayer.position - bodyPos;
            float dist = toP.magnitude;
            Vector3 dir = (dist > 0f) ? toP.normalized : Vector3.forward;

            gizBodyPos = bodyPos;
            gizBodyDir = transform.forward;

            if (dist <= closeSightRange)
            {
                float ang = Vector3.Angle(transform.forward, dir);
                if (ang <= closeSightFOV)
                {
                    gizBodyConeHasPlayer = true;
                    if (Physics.Raycast(bodyPos, dir, out RaycastHit hit, closeSightRange, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider.GetComponentInParent<PlayerController>() != null)
                        {
                            gizBodyHasLOS = true;
                            canCurrentlySeePlayer = true;
                        }
                    }
                }
            }
        }
    }

    public void FaceTowards(Vector3 worldPos, float slerpSpeed = 12f)
    {
        Vector3 to = worldPos - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(to.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.unscaledDeltaTime * slerpSpeed);
    }

    // ----- State machine -----
    void HandleStateTransitions()
    {
        if (faction == Faction.Enemy && isAlerted && targetPlayer)
        {
            float d = Vector3.Distance(transform.position, targetPlayer.position);
            currentState = (d <= chaseRange) ? State.Attack : startState;
        }
        else
        {
            currentState = startState; // Friendlies never aggro via perception
        }
    }

    void ApplySpeedByIntent()
    {
        isChasing = (faction == Faction.Enemy && currentState == State.Attack);
        agent.speed = isChasing ? runSpeed : walkSpeed;
    }

    void RunCurrentState()
    {
        switch (currentState)
        {
            case State.Idle:   DoIdle();   break;
            case State.Roam:   DoRoam();   break;
            case State.Patrol: DoPatrol(); break;
            case State.Attack: DoAttack(); break;
        }
    }

    void ChangeState(State s)
    {
        currentState = s;
        if (s == State.Roam) PickNewRoamDest();
    }

    void DoIdle()
    {
        if (agent.hasPath) agent.ResetPath();
    }

    void PickNewRoamDest()
    {
        Vector2 r = Random.insideUnitCircle * roamRadius;
        roamDest = roamCenter + new Vector3(r.x, 0f, r.y);
        nextRoamTime = Time.time + roamWaitTime;
    }

    void DoRoam()
    {
        if (!agent.hasPath || agent.remainingDistance < 0.1f)
        {
            if (Time.time >= nextRoamTime)
            {
                PickNewRoamDest();
                agent.stoppingDistance = 0f;
                agent.SetDestination(roamDest);
            }
        }
    }

    void DoPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            currentState = State.Idle;
            return;
        }

        Transform wp = patrolPoints[patrolIndex];

        if (!agent.hasPath)
        {
            agent.stoppingDistance = 0f;
            agent.SetDestination(wp.position);
        }
        else if (agent.remainingDistance <= 0.1f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            agent.stoppingDistance = 0f;
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }
    }

    void DoAttack()
    {
        if (faction != Faction.Enemy) return;
        if (!targetPlayer)
        {
            currentState = State.Idle;
            return;
        }

        float dist = Vector3.Distance(transform.position, targetPlayer.position);

        switch (enemyType)
        {
            case EnemyType.Sword:
                if (dist > attackHitDistance)
                {
                    agent.stoppingDistance = attackHitDistance;
                    agent.SetDestination(targetPlayer.position);
                }
                else
                {
                    if (agent.hasPath) agent.ResetPath();
                }
                break;

            case EnemyType.Bow:
            {
                float ideal = desiredRange;
                float tooClose = ideal * 0.7f;
                float tooFar = ideal + 2f;

                if (dist > tooFar)
                {
                    agent.stoppingDistance = ideal;
                    agent.SetDestination(targetPlayer.position);
                }
                else if (dist < tooClose)
                {
                    Vector3 away = (transform.position - targetPlayer.position).normalized;
                    Vector3 retreat = transform.position + away * 3f;
                    agent.stoppingDistance = 0f;
                    agent.SetDestination(retreat);
                }
                else
                {
                    if (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                        agent.ResetPath();
                }
                break;
            }

            case EnemyType.Ice:
            {
                float ideal = desiredRange + desiredRangedDistBonus;
                float tooClose = ideal * 0.7f;
                float tooFar = ideal + 2f;

                if (dist > tooFar)
                {
                    agent.stoppingDistance = ideal;
                    agent.SetDestination(targetPlayer.position);
                }
                else if (dist < tooClose)
                {
                    Vector3 away = (transform.position - targetPlayer.position).normalized;
                    Vector3 retreat = transform.position + away * 3f;
                    agent.stoppingDistance = 0f;
                    agent.SetDestination(retreat);
                }
                else
                {
                    if (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                        agent.ResetPath();
                }
                break;
            }
        }

        TryStartAttack(dist);
    }

    void TryStartAttack(float distToPlayer)
    {
        if (faction != Faction.Enemy) return;
        if (isAttacking) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        if (enemyType == EnemyType.Sword)
        {
            float startSwingDistance = attackHitDistance + attackWindupSlack;
            if (distToPlayer > startSwingDistance) return;
        }

        lastAttackTime = Time.time;
        isAttacking = true;
        attackEndTime = Time.time + attackAnimDuration;

        int index = Random.Range(1, 3);
        if (animator)
        {
            animator.SetInteger("AttackIndex", index);
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Attack");
        }

        if ((enemyType == EnemyType.Bow || enemyType == EnemyType.Ice) && attack != null && targetPlayer != null)
            attack.TryAttack(targetPlayer);
    }

    // Animation event receiver for melee
    public void DealDamageToPlayerEvent()
    {
        if (faction != Faction.Enemy) return;

        bool didHit = false;
        if (enemyType == EnemyType.Sword && targetPlayer)
        {
            float dist = Vector3.Distance(transform.position, targetPlayer.position);
            if (dist <= attackHitDistance)
            {
                var ph = targetPlayer.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(10);
                    didHit = true;
                }
            }
        }

        if (didHit) PlayRandomClip(attackHitClips);
        else PlayRandomClip(attackMissClips);
    }

    public void FinishAttack()
    {
        isAttacking = false;
        if (animator) animator.ResetTrigger("Attack");
    }

    public void PlayTakeDamage()
    {
        if (animator) animator.SetTrigger("TakeDamage");
    }

    // ----- Facing / Anim / SFX -----
    void UpdateFacing()
    {
        Vector3 lookDir = Vector3.zero;

        if (currentState == State.Attack && targetPlayer && faction == Faction.Enemy)
        {
            lookDir = targetPlayer.position - transform.position;
            lookDir.y = 0f;
        }
        else
        {
            Vector3 vel = agent.velocity; vel.y = 0f;
            if (vel.sqrMagnitude > 0.01f) lookDir = vel;
        }

        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion t = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, t, Time.deltaTime * 10f);
        }
    }

    void UpdateAnimatorAndFootsteps()
    {
        if (!animator) return;

        Vector3 vA = agent.velocity; vA.y = 0f;
        Vector3 vB = agent.desiredVelocity; vB.y = 0f;
        float speed = Mathf.Max(vA.magnitude, vB.magnitude);

        // Animator "Speed": 0 idle, 1 walk, 2 run
        int speedState = 0;
        float walkGate = Mathf.Max(0.02f, walkSpeed * 0.05f);
        float runGate = Mathf.Max(0.05f, runSpeed * 0.30f);
        if (speed >= runGate && currentState == State.Attack && faction == Faction.Enemy) speedState = 2;
        else if (speed >= walkGate) speedState = 1;

        animator.SetFloat("Speed", speedState, 0.08f, Time.deltaTime);

        bool movingOnNav = speed > walkGate && agent.remainingDistance > agent.stoppingDistance + 0.01f;
        if (!movingOnNav) return;

        float interval = (speedState == 2) ? runStepInterval : walkStepInterval;
        AudioClip[] bank = (speedState == 2) ? runClips : walkClips;

        if (Time.time >= nextStepTime)
        {
            nextStepTime = Time.time + interval;
            PlayRandomClip(bank);
        }
    }

    void PlayRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;
        int i = Random.Range(0, clips.Length);
        audioSource.PlayOneShot(clips[i]);
    }

    // ----- Talk overlay (voice-gated) -----
    void TickTalkOverlay()
    {
        if (!animator) return;

        bool shouldTalk = (voiceSource && voiceSource.isPlaying && talkIdleStates != null && talkIdleStates.Length > 0);
        float target = shouldTalk ? 1f : 0f;

        float spd = (target > talkLayerWeight) ? talkLayerFadeInSpeed : talkLayerFadeOutSpeed;
        talkLayerWeight = Mathf.MoveTowards(talkLayerWeight, target, Time.unscaledDeltaTime * spd);
        animator.SetLayerWeight(talkLayerIndex, talkLayerWeight);

        if (shouldTalk && talkLayerWeight > 0.9f)
        {
            if (Time.time >= nextTalkIdleTime)
            {
                string pick = talkIdleStates[Random.Range(0, talkIdleStates.Length)];
                if (!string.IsNullOrEmpty(lastTalkState) && talkIdleStates.Length > 1)
                {
                    int guard = 6;
                    while (pick == lastTalkState && guard-- > 0)
                        pick = talkIdleStates[Random.Range(0, talkIdleStates.Length)];
                }
                lastTalkState = pick;
                animator.CrossFadeInFixedTime(pick, 0.12f, talkLayerIndex);
                nextTalkIdleTime = Time.time + Random.Range(talkIdleMinInterval, talkIdleMaxInterval);
            }
        }
    }

    void ClearTalkOverlayImmediate()
    {
        lastTalkState = null;
        nextTalkIdleTime = 0f;
        talkLayerWeight = 0f;
        if (animator) animator.SetLayerWeight(talkLayerIndex, 0f);
    }

    // ----- Head aim -----
    void CacheHeadNeutral()
    {
        if (headBone && !headNeutralCached)
        {
            headNeutralLocal = headBone.localRotation;
            headNeutralCached = true;
        }
    }

    void AimHeadAt(Vector3 targetPos, float yawLimit, float pitchLimit, float speed)
    {
        if (!headBone) return;
        var parent = headBone.parent;
        if (!parent) return;

        Vector3 to = (targetPos - headBone.position);
        if (to.sqrMagnitude < 0.0001f) return;
        to.Normalize();

        Quaternion worldLook = Quaternion.LookRotation(to, Vector3.up);
        Quaternion targetLocal = Quaternion.Inverse(parent.rotation) * worldLook;

        Vector3 e = targetLocal.eulerAngles;
        e.x = NormalizeAngle(e.x);
        e.y = NormalizeAngle(e.y);
        e.z = 0f;

        e.x = Mathf.Clamp(e.x, -pitchLimit, pitchLimit);
        e.y = Mathf.Clamp(e.y, -yawLimit, yawLimit);

        Quaternion clamped = headNeutralLocal * Quaternion.Euler(e.x, e.y, 0f);
        headBone.localRotation = Quaternion.Slerp(headBone.localRotation, clamped, Time.unscaledDeltaTime * speed);
    }

    static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        return a;
    }

    // ----- Gizmos -----
    void OnDrawGizmosSelected()
    {
        // head cone
        if (headTransform)
        {
            Gizmos.color = gizHeadConeHasPlayer ? Color.green : Color.yellow;
            DrawVisionCone(headTransform.position, headTransform.forward, visionRange, visionFOV);

            if (gizHeadConeHasPlayer && targetPlayer)
            {
                Gizmos.color = gizHeadHasLOS ? Color.green : Color.red;
                Gizmos.DrawLine(headTransform.position, targetPlayer.position);
            }
        }

        // body close cone
        {
            Gizmos.color = gizBodyConeHasPlayer ? new Color(0f, 1f, 0f, 0.6f) : new Color(1f, 1f, 0f, 0.6f);
            Vector3 pos = (gizBodyPos == Vector3.zero) ? (transform.position + Vector3.up * 1.6f) : gizBodyPos;
            Vector3 dir = (gizBodyDir == Vector3.zero) ? transform.forward : gizBodyDir;
            DrawVisionCone(pos, dir, closeSightRange, closeSightFOV);

            if (gizBodyConeHasPlayer && targetPlayer)
            {
                Gizmos.color = gizBodyHasLOS ? Color.green : Color.red;
                Gizmos.DrawLine(pos, targetPlayer.position);
            }
        }

        // chase range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }

    void DrawVisionCone(Vector3 origin, Vector3 forwardDir, float range, float halfAngleDeg)
    {
        if (forwardDir.sqrMagnitude < 0.001f) forwardDir = transform.forward;
        forwardDir.Normalize();

        Gizmos.DrawLine(origin, origin + forwardDir * range);

        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, forwardDir).normalized;
        if (right.sqrMagnitude < 0.001f) right = Vector3.right;
        up = Vector3.Cross(forwardDir, right).normalized;

        Quaternion rotUp = Quaternion.AngleAxis(halfAngleDeg, right);
        Quaternion rotDown = Quaternion.AngleAxis(-halfAngleDeg, right);
        Quaternion rotRight = Quaternion.AngleAxis(halfAngleDeg, up);
        Quaternion rotLeft = Quaternion.AngleAxis(-halfAngleDeg, up);

        Vector3 dirUp = rotUp * forwardDir;
        Vector3 dirDown = rotDown * forwardDir;
        Vector3 dirRight = rotRight * forwardDir;
        Vector3 dirLeft = rotLeft * forwardDir;

        Gizmos.DrawLine(origin, origin + dirUp * range);
        Gizmos.DrawLine(origin, origin + dirDown * range);
        Gizmos.DrawLine(origin, origin + dirRight * range);
        Gizmos.DrawLine(origin, origin + dirLeft * range);

        Gizmos.DrawLine(origin + dirUp * range, origin + dirRight * range);
        Gizmos.DrawLine(origin + dirRight * range, origin + dirDown * range);
        Gizmos.DrawLine(origin + dirDown * range, origin + dirLeft * range);
        Gizmos.DrawLine(origin + dirLeft * range, origin + dirUp * range);
    }

    void OnEnable() { DialogueInteractable.OnDialogueModeChanged += HandleGlobalDialogueMode; }
    void OnDisable() { DialogueInteractable.OnDialogueModeChanged -= HandleGlobalDialogueMode; }

    void HandleGlobalDialogueMode(bool active)
    {
        if (active)
        {
            if (faction == Faction.Enemy)
            {
                dialogueOverride = true;
                factionBeforeDialogue = faction;

                // Flip friendly and engage dialogue mode (your Update already idles in dialogue)
                faction = Faction.Friendly;
                SetDialogueMode(true);

                // Kill any ongoing attack immediately so animation events can’t fire
                isAttacking = false;
                if (animator)
                {
                    animator.ResetTrigger("Attack");
                    animator.SetFloat("Speed", 0f);
                    // If you have an Idle state name, crossfade now to guarantee we exit attack
                    animator.CrossFade("Idle", 0.05f, 0);
                }

                if (agent) { agent.isStopped = true; agent.ResetPath(); }
                currentState = State.Idle;
            }
        }
        else
        {
            if (dialogueOverride)
            {
                dialogueOverride = false;
                faction = factionBeforeDialogue;
                SetDialogueMode(false);
                if (agent) agent.isStopped = false;
                currentState = State.Idle;
            }
        }
    }
}
