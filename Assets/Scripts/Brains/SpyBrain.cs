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
        Jump,
        Fall,
        Hang,
        Climb,
        Phone,
    }

    [Header("Components")]
    [SerializeField] Rigidbody2D rigidBody;
    [SerializeField] BoxCollider2D boxCollider;
    [SerializeField] Animator animator;
    [SerializeField] SpriteRenderer spriteRenderer;

    [Header("Scriptable Objects")]
    [SerializeField] SpySettingsSO settings;
    [SerializeField] SpyStatsSO stats;
    [SerializeField] PlayerInputsSO inputs;
    [SerializeField] TrainStatsSO trainStats;
    [SerializeField] TrainSettingsSO trainSettings;
    [SerializeField] LayerSettingsSO layerSettings;
    [SerializeField] GameEventDataSO gameEventData;

    [SerializeField] AnimationClip startRunClip;
    [SerializeField] AnimationClip climbClip;

    SlideDoors slideDoors;
    GangwayDoor gangwayDoor;
    Collider2D curClimbCollider;
    Carriage curCarriage;
    State curState;
    [Serializable] public struct AnimClipData
    {
        internal int idleBreathingHash;
        internal int idleLookAroundHash;
        internal int walkHash;
        internal int startRunHash;
        internal int runHash;
        internal int jumpHash;
        internal int fallHash;
        internal int heavyLandingHash;
        internal int grabLedgeHash;
        internal int hangHash;
        internal int climbHash;
        internal int callHash;
    }
    [SerializeField] AnimClipData animClipData;
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
        animClipData.jumpHash = Animator.StringToHash("Jump");
        animClipData.fallHash = Animator.StringToHash("Fall");
        animClipData.heavyLandingHash = Animator.StringToHash("HeavyLanding");
        animClipData.grabLedgeHash = Animator.StringToHash("GrabLedge");
        animClipData.hangHash = Animator.StringToHash("Hang");
        animClipData.climbHash  = Animator.StringToHash("Climb");
        animClipData.callHash = Animator.StringToHash("Call");

        Animations.SetAnimationEvent(startRunClip, nameof(PlayRunningClip));
        Animations.SetAnimationEvent(climbClip, nameof(ExitClimbClip));
    }
    private void OnEnable()
    {
        gameEventData.OnStationArrival.RegisterListener(() => stats.canBoardTrain = true);
        gameEventData.OnInteract.RegisterListener(OpenSlideDoor);
        gameEventData.OnInteract.RegisterListener(EnterTrain);
        gameEventData.OnInteract.RegisterListener(OpenGangwayDoor);
    }
    private void OnDisable()
    {
        gameEventData.OnStationArrival.UnregisterListener(() => stats.canBoardTrain = true);
        gameEventData.OnInteract.UnregisterListener(OpenSlideDoor);
        gameEventData.OnInteract.UnregisterListener(EnterTrain);
        gameEventData.OnInteract.UnregisterListener(OpenGangwayDoor);
    }
    private void Start()
    {
        curState = State.Idle;
        rigidBody.gravityScale = settings.gravityScale;
        stats.gravityScale = rigidBody.gravityScale;
        stats.startPos = rigidBody.position;
        stats.curGroundLayer = layerSettings.stationLayersStruct.ground;
        rigidBody.includeLayers = layerSettings.stationMask;
        stats.curRunSpeed = 1.0f;
        stats.curJumpHorizontalForce = 0.0f;
        stats.firstFixedFrameClimb = true;
    }
    private void Update()
    {
        ChooseState();
        UpdateStates();

        stats.spriteFlip = spriteRenderer.flipX;
        stats.curWorldPos = transform.position;
        stats.willJump = Time.time - stats.lastJumpTime <= settings.jumpBufferTime && curState != State.Jump;   
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        CalculateCollisionPoints();
        stats.isGrounded = Physics2D.Linecast(collisionPoints.groundLeft, collisionPoints.groundRight, stats.curGroundLayer);
        stats.isStepping = Physics2D.Linecast(new Vector2(collisionPoints.groundLeft.x, transform.position.y), collisionPoints.stepLeft, stats.curGroundLayer) || 
                                  Physics2D.Linecast(new Vector2(collisionPoints.groundRight.x, transform.position.y), collisionPoints.stepRight, stats.curGroundLayer);
        if (stats.isStepping)
        {
            rigidBody.position = new Vector2(rigidBody.position.x, rigidBody.position.y + (collisionPoints.stepRight.y - transform.position.y));
        }

        if (curState != State.Hang && curState != State.Climb)
        {
            stats.targetXVelocity = (settings.moveSpeed * stats.curRunSpeed * inputs.move) + (stats.curJumpHorizontalForce * inputs.move);
            rigidBody.linearVelocityX = Mathf.Lerp(stats.moveVelocity.x, stats.targetXVelocity, settings.groundAccelation * Time.fixedDeltaTime);
        }

        stats.moveVelocity = rigidBody.linearVelocity;

        if (stats.isGrounded)
        {
            stats.canHang = true;
        }
        else
        {
            curClimbCollider = Physics2D.OverlapBox(boxCollider.bounds.center, boxCollider.bounds.size, angle: 0, layerSettings.trainLayerStruct.climbingBounds);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!stats.onTrain) return;

        if ((layerSettings.trainLayerStruct.insideCarriageBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            SetLocationData(collision.bounds, layerSettings.trainLayerStruct.insideCarriageBounds);
        }
        else if ((layerSettings.trainLayerStruct.gangwayBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            SetLocationData(collision.bounds, layerSettings.trainLayerStruct.gangwayBounds);
        }
        else if ((layerSettings.trainLayerStruct.roofBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            SetLocationData(collision.bounds, layerSettings.trainLayerStruct.roofBounds);
        }

        if ((layerSettings.trainLayerStruct.carriage.value & (1 << collision.gameObject.layer)) != 0)
        {
            curCarriage = collision.GetComponent<Carriage>();
            curCarriage.StartFade(fadeIn: false);
            stats.curCarriageMinXPos = curCarriage.carriageCollider.bounds.min.x;
            stats.curCarriageMaxXPos = curCarriage.carriageCollider.bounds.max.x;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!stats.onTrain) return;

        if ((layerSettings.trainLayerStruct.insideCarriageBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            stats.curLocationLayer = 0;
        }
        else if ((layerSettings.trainLayerStruct.gangwayBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            stats.curLocationLayer = 0;
        }

        if ((layerSettings.trainLayerStruct.carriage.value & (1 << collision.gameObject.layer)) != 0)
        {
            curCarriage.StartFade(fadeIn: true);
            curCarriage = null;
            stats.curCarriageMinXPos = 0;
            stats.curCarriageMaxXPos = 0;
        }
    }
    private void ChooseState()
    {
        //TODO do heavy land logic
        if ((stats.isGrounded && stats.willJump) || rigidBody.linearVelocityY > 0.01f || stats.coyoteJump)
        {
            SetState(State.Jump);
        }
        else if (stats.isClimbing)
        {
            SetState(State.Climb);
        }
        else if (stats.canHang && curClimbCollider != null && boxCollider.bounds.max.y < curClimbCollider.bounds.max.y && stats.onTrain)
        {
            SetState(State.Hang);
        }
        else if (rigidBody.linearVelocityY < 0 && !stats.isGrounded)
        {
            SetState(State.Fall);
        }
        else if (stats.isGrounded && inputs.move != 0 && !inputs.run)
        {
            SetState(State.Walk);
        }
        else if (stats.isGrounded && inputs.move != 0 && inputs.run) //TODO: not in carriage
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
        switch (curState)
        {
            case State.Idle:
            {
                if (inputs.jump) { stats.lastJumpTime = Time.time; }
            }
            break;
            case State.Walk:
            {
                if (inputs.jump) { stats.lastJumpTime = Time.time; }
                spriteRenderer.flipX = inputs.move < 0;
            }
            break;
            case State.Run:
            {
                if (inputs.jump) { stats.lastJumpTime = Time.time; }
                spriteRenderer.flipX = inputs.move < 0;
            }
            break;
            case State.Jump:
            {
                spriteRenderer.flipX = inputs.move < 0;

                stats.curJumpHorizontalForce = Mathf.Max(stats.curJumpHorizontalForce - Time.deltaTime, 0);
            }
            break;
            case State.Fall:
            {
                stats.coyoteTimeElapsed += Time.deltaTime;
                stats.coyoteJump = stats.coyoteTimeElapsed < settings.coyoteTime && inputs.jump;

                if (inputs.jump)
                { 
                    stats.lastJumpTime = Time.time; 
                    inputs.jump = false;
                }
                spriteRenderer.flipX = inputs.move < 0;
            }
            break;
            case State.Hang:
            {
                if (inputs.cancel)
                {
                    rigidBody.gravityScale = settings.gravityScale;
                    stats.canHang = false;
                }
                else if (inputs.interact)
                {
                    stats.isClimbing = true;
                }
            }
            break;
        }
    }
    private void FixedUpdateStates()
    {
        switch (curState)
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

                //if (!inputs.jump) // enable early fall //NOTE: If i want early fall, this will need to be fixed because the velocity is being set to both jump force and 0 at the same time
                //{
                //    rigidBody.linearVelocityY = 0;
                //}

                if (rigidBody.linearVelocityY < settings.antiGravApexThreshold)
                {
                    rigidBody.gravityScale = stats.gravityScale * settings.antiGravMultiplier;
                }
            }
            break;
            case State.Fall:
            {
                rigidBody.linearVelocityY = Mathf.Max(rigidBody.linearVelocityY, -settings.maxFallSpeed);
                if (rigidBody.linearVelocityY < -settings.antiGravApexThreshold)
                {
                    rigidBody.gravityScale = stats.gravityScale;
                }
            }
            break;
            case State.Climb:
            {
                if (stats.firstFixedFrameClimb)
                {
                    float climbXPos = stats.isLeftClimbBound ? curClimbCollider.bounds.min.x : curClimbCollider.bounds.max.x;
                    rigidBody.position = new Vector2(climbXPos, curClimbCollider.bounds.max.y);
                    animator.Play(animClipData.climbHash, 0, 0f);
                    stats.firstFixedFrameClimb = false;
                }
            }
            break;
        }
    }
    private void SetState(State newState)
    {
        if (curState == newState) return;
        ExitState();
        curState = newState;
        EnterState();
    }
    private void EnterState()
    {
        switch (curState)
        {
            case State.Idle:
            {
                animator.Play(animClipData.idleBreathingHash);
                rigidBody.gravityScale = stats.gravityScale;
            }
            break;
            case State.Walk:
            {
                animator.Play(animClipData.walkHash);
            }
            break;
            case State.Run:
            {
                animator.Play(animClipData.startRunHash);
                stats.curRunSpeed = settings.runSpeedMultiplier;
            }
            break;
            case State.Jump:
            {
                animator.Play(animClipData.jumpHash);
                rigidBody.linearVelocityY = settings.jumpVerticalForce;
                stats.coyoteTimeElapsed = Mathf.Infinity;

                if (inputs.run)
                {
                    stats.curJumpHorizontalForce = settings.jumpHorizontalForce;
                }
            }
            break;
            case State.Fall:
            {
                animator.Play(animClipData.fallHash);
            }
            break;
            case State.Hang:
            {
                stats.isLeftClimbBound = rigidBody.position.x > curClimbCollider.bounds.center.x;

                animator.Play(animClipData.grabLedgeHash);
                rigidBody.gravityScale = 0;
                rigidBody.linearVelocity = Vector2.zero;

                float hangXPos = stats.isLeftClimbBound ? curClimbCollider.bounds.max.x : curClimbCollider.bounds.min.x;
                float hangYPos = curClimbCollider.bounds.center.y - boxCollider.bounds.size.y;
                rigidBody.position = new Vector2(hangXPos, hangYPos);
                spriteRenderer.flipX = stats.isLeftClimbBound; 
            }
            break;
            case State.Climb:
            {
                rigidBody.gravityScale = 0;
                rigidBody.linearVelocity = Vector2.zero;
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (curState)
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
                stats.curRunSpeed = 1.0f;
            }
            break;
            case State.Jump:
            {
                stats.curJumpHorizontalForce = 0.0f;
            }
            break;

            case State.Fall:
            {
                stats.coyoteTimeElapsed = 0.0f;
                stats.coyoteJump = false;
            }
            break;

            case State.Climb:
            {
                rigidBody.gravityScale = settings.gravityScale;
                stats.firstFixedFrameClimb = true;
            }
            break;
        }
    }
    private void PlayRunningClip()
    {
        animator.Play(animClipData.runHash);
    }
    private void ExitClimbClip()
    {
        stats.isClimbing = false;
    }
    private void CalculateCollisionPoints()
    {
        if (settings == null) return;
        collisionPoints.groundLeft = new Vector2(transform.position.x - settings.groundBufferHorizontal, transform.position.y - settings.groundBufferVertical);
        collisionPoints.groundRight = new Vector2(transform.position.x + settings.groundBufferHorizontal, transform.position.y - settings.groundBufferVertical);

        collisionPoints.stepLeft = new Vector2(transform.position.x - settings.groundBufferHorizontal, transform.position.y + settings.stepBuffer);
        collisionPoints.stepRight = new Vector2(transform.position.x + settings.groundBufferHorizontal, transform.position.y + settings.stepBuffer);
    }
    private void OpenSlideDoor()
    {
        if (rigidBody.includeLayers == layerSettings.stationLayersStruct.ground && stats.canBoardTrain)
        {
            RaycastHit2D[] slideDoorHit = Physics2D.BoxCastAll(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, layerSettings.trainLayerStruct.slideDoors);

            for (int i = 0; i < slideDoorHit.Length; i++)
            {
                slideDoors = slideDoorHit[i].collider.GetComponent<SlideDoors>();
                if (slideDoors.stats.curState == SlideDoors.State.Unlocked) 
                {
                    slideDoors.OpenDoors();
                    break;
                }
            }
        }
    }
    private void EnterTrain()
    {
        if (!stats.onTrain && stats.canBoardTrain)
        {
            RaycastHit2D slideDoorHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, layerSettings.trainLayerStruct.slideDoors);

            if (slideDoorHit.collider != null)
            {
                slideDoors = slideDoorHit.collider.GetComponent<SlideDoors>();
                if (slideDoors.stats.curState == SlideDoors.State.Opened)
                {
                    RaycastHit2D insideCarriageHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, layerSettings.trainLayerStruct.insideCarriageBounds);

                    if (insideCarriageHit.collider != null)
                    {
                        SetLocationData(insideCarriageHit.collider.bounds, layerSettings.trainLayerStruct.insideCarriageBounds);
                    }
                    stats.curGroundLayer = layerSettings.trainLayerStruct.ground;
                    rigidBody.includeLayers = layerSettings.trainMask;

                    stats.onTrain = true;
                    float zPos = Mathf.Lerp(trainSettings.maxMinWorldZPos.min, trainSettings.maxMinWorldZPos.max, settings.depthPositionInTrain);
                    transform.position = new Vector3(transform.position.x, transform.position.y, zPos);
                    trainStats.curPassengerCount ++;
                    gameEventData.OnBoardingSpy.Raise();
                }
            }
        }
    }
    private void OpenGangwayDoor()
    {
        if (!stats.onTrain || !stats.isGrounded) return;
        RaycastHit2D gangwayDoorHit = Physics2D.Linecast(boxCollider.bounds.center, new Vector2(boxCollider.bounds.center.x + (spriteRenderer.flipX ? -1 : 1), boxCollider.bounds.center.y), layerSettings.trainLayerStruct.gangwayDoor);

        if (gangwayDoorHit.collider != null)
        {
            gangwayDoor = gangwayDoorHit.collider.GetComponent<GangwayDoor>();
            gangwayDoor.OpenDoor();
        }
    }
    private void SetLocationData(Bounds bounds, LayerMask layerMask)
    {
        stats.curLocationBounds = bounds;
        stats.curLocationLayer = layerMask;
    }
    private void OnApplicationQuit()
    {
        stats.ResetStats();
    }
    private void OnDrawGizmos()
    {
        CalculateCollisionPoints();
        Gizmos.color = stats.isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(collisionPoints.groundLeft, collisionPoints.groundRight);
        Gizmos.color = stats.isStepping ? Color.green : Color.red;
        Gizmos.DrawLine(collisionPoints.stepLeft, collisionPoints.stepRight);
        Gizmos.DrawLine(collisionPoints.groundRight, collisionPoints.stepRight);
        Gizmos.DrawLine(collisionPoints.stepLeft, collisionPoints.groundLeft);

        Gizmos.color = gangwayDoor != null ? Color.green : Color.red;
        Gizmos.DrawLine(boxCollider.bounds.center, new Vector2(boxCollider.bounds.center.x + (spriteRenderer.flipX ? -1 : 1), boxCollider.bounds.center.y));
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        string groundedText = stats.isGrounded ? "Is Grounded" : "Is Not Grounded";

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.UpperRight;
        style.normal.textColor = stats.isGrounded ? Color.green : Color.red;

        Rect rect = new Rect(0, 20f, Screen.width - 20f, 25f); // full-width area, 20px margin
        GUI.Label(rect, groundedText, style);
    }
#endif

}
