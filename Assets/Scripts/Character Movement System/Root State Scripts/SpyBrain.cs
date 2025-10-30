using Proselyte.Sigils;
using System;
using Unity.Multiplayer.Center.Common.Analytics;
using Unity.VisualScripting;
using UnityEngine;

public class SpyBrain : MonoBehaviour
{
    public enum State
    { 
        Idle,
        Walk,
        Run,
        Melee,
        Shoot,
        Jump,
        Fall,
        AirMelee,
        AirShoot,
        Hang,
        Climb
    }
    [Serializable] public struct ComponentData
    {
        public Rigidbody2D rigidBody;
        public BoxCollider2D boxCollider;
        public Animator animator;
        public SpriteRenderer spriteRenderer;
        internal RuntimeAnimatorController animController;
        internal SlideDoors slideDoors;
    }
    [SerializeField] ComponentData componentData;

    [Serializable]
    public struct StateData
    {
        public StateSO[] stateSOs;
        internal State curStateType;
    }
    [SerializeField] StateData stateData;

    [Serializable] public struct GameEventData
    {
        public GameEvent OnUnlockSlideDoors;
        public GameEvent OnInteract;
    }
    [SerializeField] GameEventData gameEventData;

    [Serializable] public struct AnimClipData
    {
        public AnimationClip startRunClip;

        internal int idleBreathingHash;
        internal int idleLookAroundHash;
        internal int walkHash;
        internal int startRunHash;
        internal int runHash;
        internal int meleeHash;
        internal int shootHash;
        internal int airMeleeHash;
        internal int airShootHash;
        internal int jumpHash;
        internal int fallHash;
        internal int heavyLandingHash;
        internal int grabLedgeHash;
        internal int hangHash;
        internal int climbHash;
    }
    [SerializeField] AnimClipData animClipData;

    [Serializable] public struct SOData
    {
        public SpySettingsSO settings;
        public SpyStatsSO stats;
        public SpyInputsSO inputs;
        public TrainStatsSO trainStats;
        public TrainSettingsSO trainSettings;
        public LayerSettingsSO layerSettings;
    }
    [SerializeField] SOData soData;

    [Serializable] public struct CollisionPoints
    {
        internal Vector2 groundLeft;
        internal Vector2 groundRight;
    }
    [SerializeField] CollisionPoints collisionPoints;

    private void OnValidate()
    {
        CalculateCollisionPoints();
    }
    private void Awake()
    {
        componentData.animController = componentData.animator.runtimeAnimatorController;

        animClipData.idleBreathingHash = Animator.StringToHash("IdleBreathing");
        animClipData.idleLookAroundHash = Animator.StringToHash("IdleLookAround");
        animClipData.walkHash = Animator.StringToHash("Walk");
        animClipData.startRunHash = Animator.StringToHash("StartRun");
        animClipData.runHash = Animator.StringToHash("Run");
        animClipData.meleeHash = Animator.StringToHash("Melee");
        animClipData.shootHash = Animator.StringToHash("Shoot");
        animClipData.airMeleeHash = Animator.StringToHash("AirBorneMelee");
        animClipData.airShootHash = Animator.StringToHash("AirShoot");
        animClipData.jumpHash = Animator.StringToHash("Jump");
        animClipData.fallHash = Animator.StringToHash("Fall");
        animClipData.heavyLandingHash = Animator.StringToHash("HeavyLanding");
        animClipData.grabLedgeHash = Animator.StringToHash("GrabLedge");
        animClipData.hangHash = Animator.StringToHash("Hang");
        animClipData.climbHash  = Animator.StringToHash("Climb");

        SetAnimationEvent(animClipData.startRunClip, nameof(PlayRunningClip));
    }
    private void OnEnable()
    {
        gameEventData.OnUnlockSlideDoors.RegisterListener(() => soData.stats.canBoardTrain = true);
        gameEventData.OnInteract.RegisterListener(OpenSlideDoor);
        gameEventData.OnInteract.RegisterListener(EnterTrain);
    }
    private void OnDisable()
    {
        gameEventData.OnUnlockSlideDoors.UnregisterListener(() => soData.stats.canBoardTrain = true);
        gameEventData.OnInteract.UnregisterListener(OpenSlideDoor);
        gameEventData.OnInteract.UnregisterListener(EnterTrain);
    }
    private void Start()
    {
        stateData.curStateType = State.Idle;
        componentData.rigidBody.gravityScale = soData.settings.gravityScale;
        soData.stats.gravityScale = componentData.rigidBody.gravityScale;
        soData.stats.startPos = componentData.rigidBody.position;
        componentData.rigidBody.includeLayers = soData.layerSettings.stationGround;
        soData.stats.curRunSpeed = 1.0f;
        soData.stats.curJumpHorizontalForce = 0.0f;
    }
    private void Update()
    {
        SelectingStates();
        UpdateStates();

        soData.stats.curWorldPos = transform.position;
        soData.stats.willJump = Time.time - soData.stats.lastJumpTime <= soData.settings.jumpBufferTime && stateData.curStateType != State.Jump;
        
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        CalculateCollisionPoints();
        soData.stats.isGrounded = Physics2D.Linecast(collisionPoints.groundLeft, collisionPoints.groundRight, componentData.rigidBody.includeLayers);
        
        soData.stats.targetXVelocity = (soData.settings.moveSpeed * soData.stats.curRunSpeed * soData.inputs.move) + (soData.stats.curJumpHorizontalForce * soData.inputs.move);
        componentData.rigidBody.linearVelocityX = Mathf.Lerp(soData.stats.moveVelocity.x, soData.stats.targetXVelocity, soData.settings.groundAccelation * Time.fixedDeltaTime);
        
        soData.stats.moveVelocity = componentData.rigidBody.linearVelocity;
    }
    private void SelectingStates()
    {
        //TODO do heavy land logic
        if ((soData.stats.isGrounded && soData.stats.willJump) || componentData.rigidBody.linearVelocityY > 0.01f || soData.stats.coyoteJump)
        {
            SetState(State.Jump);
        }
        else if (componentData.rigidBody.linearVelocityY < 0 && !soData.stats.isGrounded)
        {
            SetState(State.Fall);
        }
        else if (soData.stats.isGrounded && soData.inputs.move != 0 && !soData.inputs.run)
        {
            SetState(State.Walk);
        }
        else if (soData.stats.isGrounded && soData.inputs.move != 0 && soData.inputs.run)
        {
            SetState(State.Run);
        }
        else
        {
            SetState(State.Idle);
        }
    }
    private void UpdateStates()
    {
        switch (stateData.curStateType)
        {
            case State.Idle:
            {
                if (soData.inputs.jump) { soData.stats.lastJumpTime = Time.time; }
            }
            break;
            case State.Walk:
            {
                if (soData.inputs.jump) { soData.stats.lastJumpTime = Time.time; }
                componentData.spriteRenderer.flipX = soData.inputs.move < 0;
            }
            break;
            case State.Run:
            {
                if (soData.inputs.jump) { soData.stats.lastJumpTime = Time.time; }
                componentData.spriteRenderer.flipX = soData.inputs.move < 0;
            }
            break;
            case State.Jump:
            {
                componentData.spriteRenderer.flipX = soData.inputs.move < 0;

                soData.stats.curJumpHorizontalForce = Mathf.Max(soData.stats.curJumpHorizontalForce - Time.deltaTime, 0);
            }
            break;
            case State.Fall:
            {
                soData.stats.coyoteTimeElapsed += Time.deltaTime;
                soData.stats.coyoteJump = soData.stats.coyoteTimeElapsed < soData.settings.coyoteTime && soData.inputs.jump;

                if (soData.inputs.jump)
                { 
                    soData.stats.lastJumpTime = Time.time; 
                    soData.inputs.jump = false;
                }
                componentData.spriteRenderer.flipX = soData.inputs.move < 0;
            }
            break;
        }
    }
    private void FixedUpdateStates()
    {
        switch (stateData.curStateType)
        {
            case State.Idle:
            {

            }
            break;
            case State.Walk:
            {
            }
            break;
            case State.Jump:
            {

                //if (!soData.inputs.jump) // enable early fall //NOTE: If i want early fall, this will need to be fixed because the velocity is being set to both jump force and 0 at the same time
                //{
                //    componentData.rigidBody.linearVelocityY = 0;
                //}

                if (componentData.rigidBody.linearVelocityY < soData.settings.antiGravApexThreshold)
                {
                    componentData.rigidBody.gravityScale = soData.stats.gravityScale * soData.settings.antiGravMultiplier;
                }
            }
            break;
            case State.Fall:
            {
                componentData.rigidBody.linearVelocityY = Mathf.Max(componentData.rigidBody.linearVelocityY, -soData.settings.maxFallSpeed);
                if (componentData.rigidBody.linearVelocityY < -soData.settings.antiGravApexThreshold)
                {
                    componentData.rigidBody.gravityScale = soData.stats.gravityScale;
                }
            }
            break;
        }
    }
    private void SetState(State newState)
    {
        if (stateData.curStateType == newState) return;
        ExitState();
        stateData.curStateType = newState;
        EnterState();
    }
    private void EnterState()
    {
        switch (stateData.curStateType)
        {
            case State.Idle:
            {
                componentData.animator.Play(animClipData.idleBreathingHash);
                componentData.rigidBody.gravityScale = soData.stats.gravityScale;
            }
            break;
            case State.Walk:
            {
                componentData.animator.Play(animClipData.walkHash);
            }
            break;
            case State.Run:
            {
                componentData.animator.Play(animClipData.startRunHash);
                soData.stats.curRunSpeed = soData.settings.runSpeedMultiplier;
            }
            break;
            case State.Melee:
            {
                componentData.animator.Play(animClipData.meleeHash);
            }
            break;
            case State.Shoot:
            {
                componentData.animator.Play(animClipData.shootHash);
            }
            break;
            case State.Jump:
            {
                componentData.animator.Play(animClipData.jumpHash);
                componentData.rigidBody.linearVelocityY = soData.settings.jumpVerticalForce;
                soData.stats.coyoteTimeElapsed = Mathf.Infinity;

                if (soData.inputs.run)
                {
                    soData.stats.curJumpHorizontalForce = soData.settings.jumpHorizontalForce;
                }
            }
            break;
            case State.Fall:
            {
                componentData.animator.Play(animClipData.fallHash);
            }
            break;
            case State.AirMelee:
            {
                componentData.animator.Play(animClipData.airMeleeHash);
            }
            break;
            case State.AirShoot:
            {
                componentData.animator.Play(animClipData.airShootHash);
            }
            break;
            case State.Hang:
            {
                componentData.animator.Play(animClipData.grabLedgeHash);
            }
            break;
            case State.Climb:
            {
                componentData.animator.Play(animClipData.climbHash);
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (stateData.curStateType)
        {
            case State.Idle:
            {
                
            }
            break;
            case State.Walk:
            {
            }
            break;
            case State.Run:
            {
                soData.stats.curRunSpeed = 1.0f;
            }
            break;
            case State.Melee:
            {
            }
            break;
            case State.Jump:
            {
                soData.stats.curJumpHorizontalForce = 0.0f;
            }
            break;

            case State.Fall:
            {
                soData.stats.coyoteTimeElapsed = 0.0f;
                soData.stats.coyoteJump = false;
            }
            break;
        }
    }
    private void SetAnimationEvent(AnimationClip clip, string inputFunction, float? inputTime = null)
    {
        AnimationEvent animationEvent = new AnimationEvent
        {
            time = inputTime ?? clip.length,
            functionName = inputFunction
        };
        clip.AddEvent(animationEvent);
    }
    private void PlayRunningClip()
    {
        componentData.animator.Play(animClipData.runHash);
    }
    private void CalculateCollisionPoints()
    {
        collisionPoints.groundLeft = new Vector2(transform.position.x - soData.settings.groundBufferHorizontal, transform.position.y - soData.settings.groundBufferVertical);
        collisionPoints.groundRight = new Vector2(transform.position.x + soData.settings.groundBufferHorizontal, transform.position.y - soData.settings.groundBufferVertical);
    }
    private void OpenSlideDoor()
    {
        if (componentData.rigidBody.includeLayers == soData.layerSettings.stationGround && soData.stats.canBoardTrain)
        {
            RaycastHit2D slideDoorHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, componentData.boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, soData.layerSettings.slideDoors);

            if (slideDoorHit.collider != null)
            {
                componentData.slideDoors = slideDoorHit.collider.GetComponent<SlideDoors>();
                if (componentData.slideDoors != null && componentData.slideDoors.stateData.curState == SlideDoors.State.Unlocked) 
                {
                    componentData.slideDoors.OpenDoors();
                }
            }
        }
    }

    private void EnterTrain()
    {
        if (componentData.rigidBody.includeLayers == soData.layerSettings.stationGround && soData.stats.canBoardTrain)
        {
            RaycastHit2D slideDoorHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, componentData.boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, soData.layerSettings.slideDoors);

            if (slideDoorHit.collider != null)
            {
                componentData.slideDoors = slideDoorHit.collider.GetComponent<SlideDoors>();
                if (componentData.slideDoors != null && componentData.slideDoors.stateData.curState == SlideDoors.State.Opened)
                {
                    componentData.rigidBody.includeLayers = soData.layerSettings.trainGround;
                    transform.position = new Vector3(transform.position.x, transform.position.y, soData.trainSettings.entityDepthRange.x - soData.settings.depthPositionInTrain);
                }
            }
        }
    }
    private void ResetStats()
    {
        soData.stats.curHealth = 100;
        soData.stats.curWorldPos = Vector2.zero;
        soData.stats.spriteFlip = false;
        soData.stats.startPos = Vector2.zero;
        soData.stats.checkpointPos = Vector2.zero;
        soData.stats.moveVelocity = Vector2.zero;
        soData.stats.targetXVelocity = 0.0f;
        soData.stats.curRunSpeed = 1.0f;
        soData.stats.gravityScale = soData.settings.gravityScale;
        soData.stats.curJumpHorizontalForce = 0.0f;
        soData.stats.willJump = false;
        soData.stats.lastJumpTime = 0.0f;
        soData.stats.isGrounded = false;
        soData.stats.coyoteJump = false;
        soData.stats.coyoteTimeElapsed = 0.0f;
        soData.stats.canBoardTrain = false;
    }

    private void OnApplicationQuit()
    {
        ResetStats();
    }
    private void OnDrawGizmos()
    {
        CalculateCollisionPoints();
        Gizmos.color = soData.stats.isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(collisionPoints.groundLeft, collisionPoints.groundRight);
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        string groundedText = soData.stats.isGrounded ? "Is Grounded" : "Is Not Grounded";

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.UpperRight;
        style.normal.textColor = soData.stats.isGrounded ? Color.green : Color.red;

        Rect rect = new Rect(0, 20f, Screen.width - 20f, 25f); // full-width area, 20px margin
        GUI.Label(rect, groundedText, style);
    }
#endif

}
