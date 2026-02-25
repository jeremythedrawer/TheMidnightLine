using Proselyte.Sigils;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Atlas;

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
    [SerializeField] AtlasRenderer atlasRenderer;
    [Header("Scriptable Objects")]
    [SerializeField] SpySettingsSO settings;
    [SerializeField] MaterialIDSO materialIDs;
    [SerializeField] SpyStatsSO stats;
    [SerializeField] PlayerInputsSO inputs;
    [SerializeField] TrainStatsSO trainStats;
    [SerializeField] TrainSettingsSO trainSettings;
    [SerializeField] LayerSettingsSO layerSettings;
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] PhoneSO phone;

    [Header("Generated")]
    public AtlasSO atlas;
    public SlideDoors slideDoors;
    public GangwayDoor gangwayDoor;
    public Collider2D curClimbCollider;
    public Carriage curCarriage;
    public State curState;
    public AtlasClip curClip;
    public float clipTime;
    public int curFrameIndex;
    public int prevFrameIndex;
    [Serializable] public struct CollisionPoints
    {
        public Vector2 groundLeft;
        public Vector2 groundRight;

        public Vector2 wallTopLeft;
        public Vector2 wallTopRight;
        public Vector2 wallBottomLeft;
        public Vector2 wallBottomRight;
    }
    [SerializeField] CollisionPoints collisionPoints;

    private void OnValidate()
    {
        CalculateCollisionPoints();
    }
    private void OnEnable()
    {
        gameEventData.OnStationArrival.RegisterListener(() => stats.canBoardTrain = true);
        gameEventData.OnInteract.RegisterListener(OpenSlideDoor);
        gameEventData.OnInteract.RegisterListener(EnterTrain);
    }
    private void OnDisable()
    {
        gameEventData.OnStationArrival.UnregisterListener(() => stats.canBoardTrain = true);
        gameEventData.OnInteract.UnregisterListener(OpenSlideDoor);
        gameEventData.OnInteract.UnregisterListener(EnterTrain);

    }
    private void Start()
    {
        atlas = atlasRenderer.atlas;
        atlas.UpdateClipDictionary();
        curState = State.Idle;
        rigidBody.gravityScale = settings.gravityScale;
        stats.gravityScale = rigidBody.gravityScale;
        stats.startPos = rigidBody.position;
        stats.curGroundLayer = layerSettings.stationLayers.ground;
        stats.curWallLayer = layerSettings.stationWallLayers;
        rigidBody.includeLayers = layerSettings.stationMask;
        stats.curRunSpeed = 1.0f;
        stats.curJumpHorizontalForce = 0.0f;
        stats.firstFixedFrameClimb = true;
        stats.onPhone = false;
        stats.spyID = UnityEngine.Random.Range(0, 10000).ToString("D4");
    }
    private void Update()
    {
        ChooseState();
        UpdateStates();
        PlayClip();
        stats.curWorldPos = transform.position;
        stats.willJump = Time.time - stats.lastJumpTime <= settings.jumpBufferTime && curState != State.Jump;   
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        CalculateCollisionPoints();
        stats.isGrounded = Physics2D.Linecast(collisionPoints.groundLeft, collisionPoints.groundRight, stats.curGroundLayer);

        bool leftWallTouch = Physics2D.Linecast(collisionPoints.wallBottomLeft, collisionPoints.wallTopLeft, stats.curWallLayer);
        bool rightWallTouch = Physics2D.Linecast(collisionPoints.wallBottomRight, collisionPoints.wallTopRight, stats.curWallLayer);

        stats.walkingIntoWall = (leftWallTouch && inputs.move == -1) || (rightWallTouch && inputs.move == 1);

        if (curState != State.Hang && curState != State.Climb && curState != State.Phone && !stats.walkingIntoWall)
        {
            stats.targetXVelocity = (settings.moveSpeed * stats.curRunSpeed * inputs.move) + (stats.curJumpHorizontalForce * inputs.move);
            stats.moveVelocity.x = Mathf.Lerp(stats.moveVelocity.x, stats.targetXVelocity, settings.groundAccelation * Time.fixedDeltaTime);
        }
        else
        {
            stats.moveVelocity.x = 0;
        }

        rigidBody.linearVelocity = stats.moveVelocity;

        if (stats.isGrounded)
        {
            stats.canHang = true;
        }
        else
        {
            curClimbCollider = Physics2D.OverlapBox(boxCollider.bounds.center, boxCollider.bounds.size, angle: 0, layerSettings.trainLayers.climbingBounds);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!stats.onTrain) return;

        if ((layerSettings.trainLayers.insideCarriageBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            SetLocationData(collision.bounds, layerSettings.trainLayers.insideCarriageBounds);
            curCarriage = trainStats.carriageDict[collision];
            curCarriage.StartFade(fadeIn: false);
            //stats.curCarriageMinXPos = curCarriage.carriageCollider.bounds.min.x;
            //stats.curCarriageMaxXPos = curCarriage.carriageCollider.bounds.max.x;
        }
        else if ((layerSettings.trainLayers.gangwayBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            SetLocationData(collision.bounds, layerSettings.trainLayers.gangwayBounds);
        }
        else if ((layerSettings.trainLayers.roofBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            SetLocationData(collision.bounds, layerSettings.trainLayers.roofBounds);
        }

        if ((layerSettings.trainLayers.insideCarriageBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!stats.onTrain) return;

        if ((layerSettings.trainLayers.insideCarriageBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            stats.curLocationLayer = 0;
        }
        else if ((layerSettings.trainLayers.gangwayBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            stats.curLocationLayer = 0;
        }

        if ((layerSettings.trainLayers.carriage.value & (1 << collision.gameObject.layer)) != 0)
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
        else if (stats.onPhone)
        {
            SetState(State.Phone);
        }
        else if (stats.isGrounded && inputs.move != 0 && !inputs.run && !stats.walkingIntoWall)
        {
            SetState(State.Walk);
        }
        else if (stats.isGrounded && inputs.move != 0 && inputs.run && !stats.walkingIntoWall) //TODO: not in carriage
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
                Flip(inputs.move < 0);
                
            }
            break;
            case State.Run:
            {
                if (inputs.jump) { stats.lastJumpTime = Time.time; }
                Flip(inputs.move < 0);
            }
            break;
            case State.Jump:
            {
                Flip(inputs.move < 0);
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
                Flip(inputs.move < 0);
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
                    //animator.Play(animClipData.climbHash, 0, 0f);
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
                curClip = atlas.clipDict[(int)SpyMotion.StandingBreathing];
                rigidBody.gravityScale = stats.gravityScale;
            }
            break;
            case State.Walk:
            {
                curClip = atlas.clipDict[(int)SpyMotion.Walking];
            }
            break;
            case State.Run:
            {
                curClip = atlas.clipDict[(int)SpyMotion.Running];
                stats.curRunSpeed = settings.runSpeedMultiplier;
            }
            break;
            case State.Jump:
            {
                curClip = atlas.clipDict[(int)SpyMotion.Jump];
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
                curClip = atlas.clipDict[(int)SpyMotion.Fall];
            }
            break;
            case State.Hang:
            {
                stats.isLeftClimbBound = rigidBody.position.x > curClimbCollider.bounds.center.x;

                curClip = atlas.clipDict[(int)SpyMotion.GrabLedge];
                rigidBody.gravityScale = 0;
                rigidBody.linearVelocity = Vector2.zero;

                float hangXPos = stats.isLeftClimbBound ? curClimbCollider.bounds.max.x : curClimbCollider.bounds.min.x;
                float hangYPos = curClimbCollider.bounds.center.y - boxCollider.bounds.size.y;
                rigidBody.position = new Vector2(hangXPos, hangYPos);
                Flip(stats.isLeftClimbBound); 
            }
            break;
            case State.Climb:
            {
                curClip = atlas.clipDict[(int)SpyMotion.Climbing];
                rigidBody.gravityScale = 0;
                rigidBody.linearVelocity = Vector2.zero;
            }
            break;
            case State.Phone:
            {
                curClip = atlas.clipDict[(int)SpyMotion.StandingCalling];
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
    private void CalculateCollisionPoints()
    {
        if (settings == null) return;

        float groundLeft = transform.position.x - settings.groundBufferHorizontal;
        float groundRight = transform.position.x + settings.groundBufferHorizontal;
        float groundBottom = transform.position.y - settings.groundBufferVertical;
        float step = transform.position.y + settings.stepBuffer;
        float waste = transform.position.y + settings.wasteBuffer;
        float wallLeft = transform.position.x - settings.wallBuffer;
        float wallRight = transform.position.x + settings.wallBuffer;
        float head = transform.position.y + settings.headBuffer;
        collisionPoints.groundLeft = new Vector2(groundLeft, groundBottom);
        collisionPoints.groundRight = new Vector2(groundRight, groundBottom);

        collisionPoints.wallTopLeft = new Vector2(wallLeft, head);
        collisionPoints.wallTopRight = new Vector2(wallRight, head);

        collisionPoints.wallBottomLeft = new Vector2(wallLeft, waste);
        collisionPoints.wallBottomRight = new Vector2(wallRight, waste);

    }
    private void OpenSlideDoor()
    {
        if (rigidBody.includeLayers == layerSettings.stationLayers.ground && stats.canBoardTrain)
        {
            RaycastHit2D[] slideDoorHit = Physics2D.BoxCastAll(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, layerSettings.trainLayers.slideDoors);

            for (int i = 0; i < slideDoorHit.Length; i++)
            {
                slideDoors = slideDoorHit[i].collider.GetComponent<SlideDoors>();
                if (slideDoors.curState == SlideDoors.State.Unlocked) 
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
            RaycastHit2D slideDoorHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, layerSettings.trainLayers.slideDoors);

            if (slideDoorHit.collider != null)
            {
                slideDoors = slideDoorHit.collider.GetComponent<SlideDoors>();
                if (slideDoors.curState == SlideDoors.State.Opened)
                {
                    RaycastHit2D insideCarriageHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, layerSettings.trainLayers.insideCarriageBounds);

                    if (insideCarriageHit.collider != null)
                    {
                        SetLocationData(insideCarriageHit.collider.bounds, layerSettings.trainLayers.insideCarriageBounds);
                    }
                    stats.curGroundLayer = layerSettings.trainLayers.ground;
                    stats.curWallLayer = layerSettings.trainWallLayers;
                    rigidBody.includeLayers = layerSettings.trainMask;

                    stats.onTrain = true;
                    atlasRenderer.depthOrder = CHARACTER_ON_TRAIN_DEPTH;
                    trainStats.curPassengerCount ++;
                    gameEventData.OnBoardingSpy.Raise();
                }
            }
        }
    }
    private void SetLocationData(Bounds bounds, LayerMask layerMask)
    {
        stats.curLocationBounds = bounds;
        stats.curLocationLayer = layerMask;
    }
    private void Flip(bool flip)
    {
        stats.spriteFlip = flip;
        atlasRenderer.Flip(flip);
    }
    private void PlayClip()
    {
        clipTime += Time.deltaTime;
        atlasRenderer.sprite = curClip.GetNextSprite(ref clipTime, ref curFrameIndex, ref prevFrameIndex);
    }
    private void OnApplicationQuit()
    {
        stats.ResetStats();
    }
    private void OnDrawGizmos()
    {
        CalculateCollisionPoints();
        Gizmos.color = stats.isGrounded ? Color.limeGreen : Color.orange;
        Gizmos.DrawLine(collisionPoints.groundLeft, collisionPoints.groundRight);

        Gizmos.color = stats.walkingIntoWall ? Color.forestGreen : Color.red;
        Gizmos.DrawLine(collisionPoints.wallTopLeft, collisionPoints.wallBottomLeft);
        Gizmos.DrawLine(collisionPoints.wallTopRight, collisionPoints.wallBottomRight);

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
