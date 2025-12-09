using Proselyte.Sigils;
using System;
using UnityEngine;
using static SpyBrain;

public class SpyBrain : MonoBehaviour
{
    public enum State
    { 
        Idle,
        Walk,
        Run,
        Melee,
        Jump,
        Fall,
        Hang,
        Climb
    }
    [Serializable] public struct ComponentData
    {
        public Rigidbody2D rigidBody;
        public BoxCollider2D boxCollider;
        public Animator animator;
        public SpriteRenderer spriteRenderer;
        internal SlideDoors slideDoors;
        internal GangwayDoor gangwayDoor;
        internal Collider2D curClimbCollider;
    }
    [SerializeField] ComponentData componentData;

    [Serializable]
    public struct StateData
    {
        internal State curStateType;
    }
    [SerializeField] StateData stateData;

    [Serializable] public struct AnimClipData
    {
        public AnimationClip startRunClip;
        public AnimationClip climbClip;

        internal int idleBreathingHash;
        internal int idleLookAroundHash;
        internal int walkHash;
        internal int startRunHash;
        internal int runHash;
        internal int meleeHash;
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
        public GameEventDataSO gameEventData;
    }
    [SerializeField] SOData soData;

    [Serializable] public struct CollisionPoints
    {
        internal Vector2 groundLeft;
        internal Vector2 groundRight;

        internal Vector2 stepLeft;
        internal Vector2 stepRight;
    }
    [SerializeField] CollisionPoints collisionPoints;

    private void OnValidate()
    {
        CalculateCollisionPoints();
    }
    private void Awake()
    {
        animClipData.idleBreathingHash = Animator.StringToHash("IdleBreathing");
        animClipData.idleLookAroundHash = Animator.StringToHash("IdleLookAround");
        animClipData.walkHash = Animator.StringToHash("Walk");
        animClipData.startRunHash = Animator.StringToHash("StartRun");
        animClipData.runHash = Animator.StringToHash("Run");
        animClipData.meleeHash = Animator.StringToHash("Melee");
        animClipData.jumpHash = Animator.StringToHash("Jump");
        animClipData.fallHash = Animator.StringToHash("Fall");
        animClipData.heavyLandingHash = Animator.StringToHash("HeavyLanding");
        animClipData.grabLedgeHash = Animator.StringToHash("GrabLedge");
        animClipData.hangHash = Animator.StringToHash("Hang");
        animClipData.climbHash  = Animator.StringToHash("Climb");

        AnimationUtilities.SetAnimationEvent(animClipData.startRunClip, nameof(PlayRunningClip));
        AnimationUtilities.SetAnimationEvent(animClipData.climbClip, nameof(ExitClimbClip));
    }
    private void OnEnable()
    {
        soData.gameEventData.OnUnlockSlideDoors.RegisterListener(() => soData.stats.canBoardTrain = true);
        soData.gameEventData.OnInteract.RegisterListener(OpenSlideDoor);
        soData.gameEventData.OnInteract.RegisterListener(EnterTrain);
        soData.gameEventData.OnInteract.RegisterListener(OpenGangwayDoor);
    }
    private void OnDisable()
    {
        soData.gameEventData.OnUnlockSlideDoors.UnregisterListener(() => soData.stats.canBoardTrain = true);
        soData.gameEventData.OnInteract.UnregisterListener(OpenSlideDoor);
        soData.gameEventData.OnInteract.UnregisterListener(EnterTrain);
        soData.gameEventData.OnInteract.UnregisterListener(OpenGangwayDoor);
    }
    private void Start()
    {
        stateData.curStateType = State.Idle;
        componentData.rigidBody.gravityScale = soData.settings.gravityScale;
        soData.stats.gravityScale = componentData.rigidBody.gravityScale;
        soData.stats.startPos = componentData.rigidBody.position;
        soData.stats.curGroundLayer = soData.layerSettings.stationLayers.ground;
        componentData.rigidBody.includeLayers = soData.layerSettings.stationMask;
        soData.stats.curRunSpeed = 1.0f;
        soData.stats.curJumpHorizontalForce = 0.0f;
        soData.stats.firstFixedFrameClimb = true;
    }
    private void Update()
    {
        SelectingStates();
        UpdateStates();

        soData.stats.spriteFlip = componentData.spriteRenderer.flipX;
        soData.stats.curWorldPos = transform.position;
        soData.stats.willJump = Time.time - soData.stats.lastJumpTime <= soData.settings.jumpBufferTime && stateData.curStateType != State.Jump;     
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        CalculateCollisionPoints();
        soData.stats.isGrounded = Physics2D.Linecast(collisionPoints.groundLeft, collisionPoints.groundRight, soData.stats.curGroundLayer);
        soData.stats.isStepping =   Physics2D.Linecast(new Vector2(collisionPoints.groundLeft.x, transform.position.y), collisionPoints.stepLeft, soData.stats.curGroundLayer) || 
                                    Physics2D.Linecast(new Vector2(collisionPoints.groundRight.x, transform.position.y), collisionPoints.stepRight, soData.stats.curGroundLayer);
        if (soData.stats.isStepping)
        {
            componentData.rigidBody.position = new Vector2(componentData.rigidBody.position.x, componentData.rigidBody.position.y + (collisionPoints.stepRight.y - transform.position.y));
        }

        if (stateData.curStateType != State.Hang && stateData.curStateType != State.Climb)
        {
            soData.stats.targetXVelocity = (soData.settings.moveSpeed * soData.stats.curRunSpeed * soData.inputs.move) + (soData.stats.curJumpHorizontalForce * soData.inputs.move);
            componentData.rigidBody.linearVelocityX = Mathf.Lerp(soData.stats.moveVelocity.x, soData.stats.targetXVelocity, soData.settings.groundAccelation * Time.fixedDeltaTime);
        }

        soData.stats.moveVelocity = componentData.rigidBody.linearVelocity;

        if (soData.stats.isGrounded)
        {
            soData.stats.canHang = true;
        }
        else
        {
            componentData.curClimbCollider = Physics2D.OverlapBox(componentData.boxCollider.bounds.center, componentData.boxCollider.bounds.size, angle: 0, soData.layerSettings.trainLayers.climbingBounds);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!soData.stats.onTrain) return;

        if ((soData.layerSettings.trainLayers.insideCarriageBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            SetLocationData(collision.bounds, soData.layerSettings.trainLayers.insideCarriageBounds);
        }
        else if ((soData.layerSettings.trainLayers.gangwayBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            SetLocationData(collision.bounds, soData.layerSettings.trainLayers.gangwayBounds);
        }
        else if ((soData.layerSettings.trainLayers.roofBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            SetLocationData(collision.bounds, soData.layerSettings.trainLayers.roofBounds);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!soData.stats.onTrain) return;

        if ((soData.layerSettings.trainLayers.insideCarriageBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            soData.stats.curLocationLayer = 0;
        }
        else if ((soData.layerSettings.trainLayers.gangwayBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            soData.stats.curLocationLayer = 0;
        }
    }
    private void SelectingStates()
    {
        //TODO do heavy land logic
        if ((soData.stats.isGrounded && soData.stats.willJump) || componentData.rigidBody.linearVelocityY > 0.01f || soData.stats.coyoteJump)
        {
            SetState(State.Jump);
        }
        else if (soData.stats.isClimbing)
        {
            SetState(State.Climb);
        }
        else if (soData.stats.canHang && componentData.curClimbCollider != null && componentData.boxCollider.bounds.max.y < componentData.curClimbCollider.bounds.max.y && soData.stats.onTrain)
        {
            SetState(State.Hang);
        }
        else if (componentData.rigidBody.linearVelocityY < 0 && !soData.stats.isGrounded)
        {
            SetState(State.Fall);
        }
        else if (soData.stats.isGrounded && soData.inputs.move != 0 && !soData.inputs.run)
        {
            SetState(State.Walk);
        }
        else if (soData.stats.isGrounded && soData.inputs.move != 0 && soData.inputs.run) //TODO: not in carriage
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
            case State.Hang:
            {
                if (soData.inputs.cancel)
                {
                    componentData.rigidBody.gravityScale = soData.settings.gravityScale;
                    soData.stats.canHang = false;
                }
                else if (soData.inputs.interact)
                {
                    soData.stats.isClimbing = true;
                }
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
            case State.Climb:
            {
                if (soData.stats.firstFixedFrameClimb)
                {
                    float climbXPos = soData.stats.isLeftClimbBound ? componentData.curClimbCollider.bounds.min.x : componentData.curClimbCollider.bounds.max.x;
                    componentData.rigidBody.position = new Vector2(climbXPos, componentData.curClimbCollider.bounds.max.y);
                    componentData.animator.Play(animClipData.climbHash, 0, 0f);
                    soData.stats.firstFixedFrameClimb = false;
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
            case State.Hang:
            {
                soData.stats.isLeftClimbBound = componentData.rigidBody.position.x > componentData.curClimbCollider.bounds.center.x;

                componentData.animator.Play(animClipData.grabLedgeHash);
                componentData.rigidBody.gravityScale = 0;
                componentData.rigidBody.linearVelocity = Vector2.zero;

                float hangXPos = soData.stats.isLeftClimbBound ? componentData.curClimbCollider.bounds.max.x : componentData.curClimbCollider.bounds.min.x;
                float hangYPos = componentData.curClimbCollider.bounds.center.y - componentData.boxCollider.bounds.size.y;
                componentData.rigidBody.position = new Vector2(hangXPos, hangYPos);
                componentData.spriteRenderer.flipX = soData.stats.isLeftClimbBound; 
            }
            break;
            case State.Climb:
            {
                componentData.rigidBody.gravityScale = 0;
                componentData.rigidBody.linearVelocity = Vector2.zero;
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

            case State.Climb:
            {
                componentData.rigidBody.gravityScale = soData.settings.gravityScale;
                soData.stats.firstFixedFrameClimb = true;
            }
            break;
        }
    }
    private void PlayRunningClip()
    {
        componentData.animator.Play(animClipData.runHash);
    }
    private void ExitClimbClip()
    {
        soData.stats.isClimbing = false;
    }
    private void CalculateCollisionPoints()
    {
        collisionPoints.groundLeft = new Vector2(transform.position.x - soData.settings.groundBufferHorizontal, transform.position.y - soData.settings.groundBufferVertical);
        collisionPoints.groundRight = new Vector2(transform.position.x + soData.settings.groundBufferHorizontal, transform.position.y - soData.settings.groundBufferVertical);

        collisionPoints.stepLeft = new Vector2(transform.position.x - soData.settings.groundBufferHorizontal, transform.position.y + soData.settings.stepBuffer);
        collisionPoints.stepRight = new Vector2(transform.position.x + soData.settings.groundBufferHorizontal, transform.position.y + soData.settings.stepBuffer);
    }
    private void OpenSlideDoor()
    {
        if (componentData.rigidBody.includeLayers == soData.layerSettings.stationLayers.ground && soData.stats.canBoardTrain)
        {
            RaycastHit2D[] slideDoorHit = Physics2D.BoxCastAll(componentData.boxCollider.bounds.center, componentData.boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, soData.layerSettings.trainLayers.slideDoors);

            for (int i = 0; i < slideDoorHit.Length; i++)
            {
                componentData.slideDoors = slideDoorHit[i].collider.GetComponent<SlideDoors>();
                if (componentData.slideDoors.stats.curState == SlideDoors.State.Unlocked) 
                {
                    componentData.slideDoors.OpenDoors();
                    break;
                }
            }
        }
    }
    private void EnterTrain()
    {
        if (!soData.stats.onTrain && soData.stats.canBoardTrain)
        {
            RaycastHit2D slideDoorHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, componentData.boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, soData.layerSettings.trainLayers.slideDoors);

            if (slideDoorHit.collider != null)
            {
                componentData.slideDoors = slideDoorHit.collider.GetComponent<SlideDoors>();
                if (componentData.slideDoors.stats.curState == SlideDoors.State.Opened)
                {
                    RaycastHit2D insideCarriageHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, componentData.boxCollider.bounds.extents, 0.0f, Vector2.zero, soData.layerSettings.trainLayers.insideCarriageBounds);

                    if (insideCarriageHit.collider != null)
                    {
                        SetLocationData(insideCarriageHit.collider.bounds, soData.layerSettings.trainLayers.insideCarriageBounds);
                    }
                    soData.stats.curGroundLayer = soData.layerSettings.trainLayers.ground;
                    componentData.rigidBody.includeLayers = soData.layerSettings.trainMask;

                    soData.stats.onTrain = true;
                    transform.position = new Vector3(transform.position.x, transform.position.y, soData.trainSettings.entityDepthRange.x + soData.settings.depthPositionInTrain);
                    soData.trainStats.curPassengerCount ++;
                    soData.gameEventData.OnBoardingSpy.Raise();
                }
            }
        }
    }
    private void OpenGangwayDoor()
    {
        if (!soData.stats.onTrain || !soData.stats.isGrounded) return;
        RaycastHit2D gangwayDoorHit = Physics2D.Linecast(componentData.boxCollider.bounds.center, new Vector2(componentData.boxCollider.bounds.center.x + (componentData.spriteRenderer.flipX ? -1 : 1), componentData.boxCollider.bounds.center.y), soData.layerSettings.trainLayers.gangwayDoor);

        if (gangwayDoorHit.collider != null)
        {
            componentData.gangwayDoor = gangwayDoorHit.collider.GetComponent<GangwayDoor>();
            componentData.gangwayDoor.OpenDoor();
        }
    }
    private void SetLocationData(Bounds bounds, LayerMask layerMask)
    {
        soData.stats.curLocationBounds = bounds;
        soData.stats.curLocationLayer = layerMask;
    }
    private void OnApplicationQuit()
    {
        soData.stats.ResetStats();
    }
    private void OnDrawGizmos()
    {
        CalculateCollisionPoints();
        Gizmos.color = soData.stats.isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(collisionPoints.groundLeft, collisionPoints.groundRight);
        Gizmos.color = soData.stats.isStepping ? Color.green : Color.red;
        Gizmos.DrawLine(collisionPoints.stepLeft, collisionPoints.stepRight);
        Gizmos.DrawLine(collisionPoints.groundRight, collisionPoints.stepRight);
        Gizmos.DrawLine(collisionPoints.stepLeft, collisionPoints.groundLeft);

        Gizmos.color = componentData.gangwayDoor != null ? Color.green : Color.red;
        Gizmos.DrawLine(componentData.boxCollider.bounds.center, new Vector2(componentData.boxCollider.bounds.center.x + (componentData.spriteRenderer.flipX ? -1 : 1), componentData.boxCollider.bounds.center.y));
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
