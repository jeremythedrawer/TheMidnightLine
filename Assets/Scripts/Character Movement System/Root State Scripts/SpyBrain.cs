using System;
using Unity.Multiplayer.Center.Common.Analytics;
using Unity.VisualScripting;
using UnityEngine;

public class SpyBrain : MonoBehaviour
{
    public enum State
    { 
        Idle,
        Move,
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
    }
    public ComponentData componentData;

    [Serializable]
    public struct StateData
    {
        public StateSO[] stateSOs;
        internal State curStateType;
    }
    public StateData stateData;

    [Serializable]
    public struct GameEventData
    {
    }
    public GameEventData gameEventData;

    [Serializable]
    public struct AnimClipData
    {
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
    public AnimClipData animClipData;

    [Serializable] public struct SOData
    {
        public SpySettingsSO settings;
        public SpyStatsSO stats;
        public SpyInputsSO inputs;
    }
    public SOData soData;

    [Serializable]
    public struct CollisionPoints
    {
        internal Vector2 groundLeft;
        internal Vector2 groundRight;
    }
    public CollisionPoints collisionPoints;


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
    }
    void Start()
    {
        stateData.curStateType = State.Idle;
        componentData.rigidBody.gravityScale = soData.settings.gravityScale;
        soData.stats.gravityScale = componentData.rigidBody.gravityScale;
        soData.stats.startPos = componentData.rigidBody.position;
        soData.stats.curGroundMask = LayerMask.GetMask("Station Ground");
        soData.stats.curRunSpeed = 1.0f;
    }
    void Update()
    {
        SelectingStates();
        UpdateStates();

        soData.stats.curWorldPos = transform.position;
        soData.stats.willJump = Time.time - soData.stats.lastJumpTime <= soData.settings.jumpBufferTime && stateData.curStateType != State.Jump;
        componentData.spriteRenderer.flipX = soData.inputs.move < 0;
        soData.stats.targetXVelocity = soData.settings.moveSpeed * soData.stats.curRunSpeed * soData.inputs.move;
    }
    private void FixedUpdate()
    {
        HandleVelocity();
        FixedUpdateStates();
        CalculateCollisionPoints();
        soData.stats.isGrounded = Physics2D.Linecast(collisionPoints.groundLeft, collisionPoints.groundRight, soData.stats.curGroundMask);
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
        else if (soData.stats.isGrounded && soData.inputs.move != 0)
        {
            SetState(State.Move);
        }
        else if (soData.stats.isGrounded && soData.inputs.move == 0)
        {
            SetState(State.Idle);
        }
        else
        {
            Debug.LogWarning($" {gameObject.name} is currently not in a state");
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
            case State.Move:
            {
                if (soData.inputs.jump) { soData.stats.lastJumpTime = Time.time; }

                soData.stats.curRunSpeed = soData.inputs.run ? soData.settings.runSpeedMultiplier : 1;
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
            }
            break;
        }
    }
    private void FixedUpdateStates()
    {
        switch (stateData.curStateType)
        {
            case State.Move:
            {
            }
            break;
            case State.Jump:
            {
                soData.stats.targetXVelocity = soData.settings.moveSpeed;

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
            case State.Move:
            {
                componentData.animator.Play(animClipData.walkHash);
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
                componentData.rigidBody.linearVelocityY = soData.settings.jumpForce;

                soData.stats.coyoteTimeElapsed = Mathf.Infinity;
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
            case State.Move:
            {
                soData.stats.curRunSpeed = 1;
            }
            break;
            case State.Melee:
            {
            }
            break;
            case State.Jump:
            {
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
    private void HandleVelocity()
    {
        componentData.rigidBody.linearVelocityX = Mathf.Lerp(soData.stats.moveVelocity.x, soData.stats.targetXVelocity, soData.settings.groundAccelation * Time.fixedDeltaTime);
        soData.stats.moveVelocity = componentData.rigidBody.linearVelocity;
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
    private void CalculateCollisionPoints()
    {
        collisionPoints.groundLeft = new Vector2(transform.position.x - soData.settings.groundBufferHorizontal, transform.position.y - soData.settings.groundBufferVertical);
        collisionPoints.groundRight = new Vector2(transform.position.x + soData.settings.groundBufferHorizontal, transform.position.y - soData.settings.groundBufferVertical);
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
        soData.stats.willJump = false;
        soData.stats.lastJumpTime = 0.0f;
        soData.stats.isGrounded = false;
        soData.stats.coyoteJump = false;
        soData.stats.coyoteTimeElapsed = 0.0f;
        soData.stats.onTrain = false;
        soData.stats.curGroundMask = LayerMask.NameToLayer("Station Ground");

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
