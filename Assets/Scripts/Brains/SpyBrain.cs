using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using static Atlas;

public class SpyBrain : MonoBehaviour
{
    const float DEPTH_CHANGE_TIME = 0.5f;
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
    [SerializeField] Material matrixMaterial;
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
    [Serializable] public struct CollisionData
    {
        public Vector2 groundLeft;
        public Vector2 groundRight;

        public Vector2 stepTopLeft;
        public Vector2 stepTopRight;

        public Vector2 stepBottomLeft;
        public Vector2 stepBottomRight;

        public Vector2 wallTopLeft;
        public Vector2 wallTopRight;
        public Vector2 wallBottomLeft;
        public Vector2 wallBottomRight;

        public RaycastHit2D[] leftStepResults;
        public RaycastHit2D[] rightStepResults;
        public ContactFilter2D stepFilter;
    }
    [SerializeField] CollisionData collisionData;

    public float lastGroundYPos;
    private void OnValidate()
    {
        CalculateCollisionPoints();
    }
    private void OnEnable()
    {
        gameEventData.OnStationArrival.RegisterListener(() => stats.canBoardTrain = true);
        gameEventData.OnInteract.RegisterListener(OpenTrainDoors);
        gameEventData.OnInteract.RegisterListener(EnterTrain);
    }
    private void OnDisable()
    {
        gameEventData.OnStationArrival.UnregisterListener(() => stats.canBoardTrain = true);
        gameEventData.OnInteract.UnregisterListener(OpenTrainDoors);
        gameEventData.OnInteract.UnregisterListener(EnterTrain);

    }
    private void Start()
    {
        atlas = atlasRenderer.atlas;
        atlas.UpdateClipDictionary();
        curState = State.Idle;
        rigidBody.gravityScale = settings.gravityScale;
        stats.curGravityScale = rigidBody.gravityScale;
        stats.startPos = rigidBody.position;
        stats.curGroundLayer = layerSettings.stationLayers.ground;
        stats.curWallLayer = layerSettings.stationWallLayers;
        rigidBody.includeLayers = layerSettings.stationMask;
        stats.curRunSpeed = 1.0f;
        stats.firstFixedFrameClimb = true;
        stats.onPhone = false;
        stats.spyID = UnityEngine.Random.Range(0, 10000).ToString("D4");

        collisionData.leftStepResults = new RaycastHit2D[1];
        collisionData.rightStepResults = new RaycastHit2D[1];

        collisionData.stepFilter = new ContactFilter2D() { useLayerMask = true, layerMask = layerSettings.stationLayers.ground };

        stats.maxJumpHeight = (settings.jumpVerticalForce * settings.jumpVerticalForce) / (2 * settings.gravityScale);

        matrixMaterial.SetFloat("_PlayerDepth", atlasRenderer.depthOrder);
    }
    private void Update()
    {
        ChooseState();
        UpdateStates();

        stats.curWorldPos = transform.position;
        stats.willJump = Time.time - stats.lastJumpTime <= settings.jumpBufferTime && curState != State.Jump;   
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        CalculateCollisionPoints();
        stats.isGrounded = Physics2D.Linecast(collisionData.groundLeft, collisionData.groundRight, stats.curGroundLayer);

        Physics2D.Linecast(collisionData.stepBottomLeft, collisionData.stepTopLeft, collisionData.stepFilter, collisionData.leftStepResults);
        Physics2D.Linecast(collisionData.stepBottomRight, collisionData.stepTopRight, collisionData.stepFilter, collisionData.rightStepResults);

        if (collisionData.leftStepResults[0].collider != default)
        {
            Bounds hitColliderHeight = collisionData.leftStepResults[0].collider.bounds;
            if (hitColliderHeight.max.y < collisionData.stepTopLeft.y)
            {
                rigidBody.gravityScale = 0;
                rigidBody.position = new Vector2(rigidBody.position.x - 0.1f, hitColliderHeight.max.y);
                rigidBody.gravityScale = stats.curGravityScale;
            }
            collisionData.leftStepResults[0] = default;
        }
        else if (collisionData.rightStepResults[0] != default)
        {
            Bounds hitColliderHeight = collisionData.rightStepResults[0].collider.bounds;
            if (hitColliderHeight.max.y < collisionData.stepTopRight.y)
            {
                rigidBody.gravityScale = 0;
                rigidBody.position = new Vector2(rigidBody.position.x + 0.1f, hitColliderHeight.max.y);
                rigidBody.gravityScale = stats.curGravityScale;
            }
            collisionData.rightStepResults[0] = default;
        }

        bool leftWallTouch = Physics2D.Linecast(collisionData.wallBottomLeft, collisionData.wallTopLeft, stats.curWallLayer);
        bool rightWallTouch = Physics2D.Linecast(collisionData.wallBottomRight, collisionData.wallTopRight, stats.curWallLayer);

        stats.walkingIntoWall = (leftWallTouch && inputs.move == -1) || (rightWallTouch && inputs.move == 1);

        if (curState != State.Hang && curState != State.Climb && curState != State.Phone && !stats.walkingIntoWall)
        {
            stats.targetXVelocity = (settings.moveSpeed * stats.curRunSpeed * inputs.move);
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


            stats.moveVelocity.y -= stats.curGravityScale * Time.fixedDeltaTime;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!stats.onTrain) return;

        if ((layerSettings.trainLayers.insideCarriageBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            SetLocationData(collision.bounds, layerSettings.trainLayers.insideCarriageBounds);
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
            curCarriage = null;
            stats.curCarriageMinXPos = 0;
            stats.curCarriageMaxXPos = 0;
        }
    }
    private void ChooseState()
    {
        //TODO do heavy land logic
        if ((stats.isGrounded && stats.willJump) || stats.moveVelocity.y > 0 || stats.coyoteJump)
        {
            SetState(State.Jump);
        }
        else if (stats.isClimbing)
        {
            SetState(State.Climb);
        }
        else if (stats.canHang && curClimbCollider != null && boxCollider.bounds.max.y < curClimbCollider.bounds.max.y && (stats.curLocationLayer & layerSettings.trainLayers.gangwayBounds) != 0)
        {
            SetState(State.Hang);
        }
        else if (stats.moveVelocity.y <= 0 && !stats.isGrounded)
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
                PlayClip();
            }
            break;
            case State.Walk:
            {
                if (inputs.jump) { stats.lastJumpTime = Time.time; }
                Flip(inputs.move < 0);
                PlayClip();
            }
            break;
            case State.Run:
            {
                if (inputs.jump) { stats.lastJumpTime = Time.time; }
                Flip(inputs.move < 0);
                PlayClip();
            }
            break;
            case State.Jump:
            {
                Flip(inputs.move < 0);

                float normHeight = (transform.position.y - stats.lastGroundHeight) / (stats.maxJumpHeight - stats.lastGroundHeight);
                PlayManualClip(normHeight);
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

                float normHeight = (transform.position.y - stats.lastGroundHeight) / (stats.maxJumpHeight - stats.lastGroundHeight);
                PlayManualClip(1 - normHeight);
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

                PlayClip();
            }
            break;
            case State.Climb:
            {
                PlayClip();
                if (curFrameIndex == curClip.keyFrames.Length - 1)
                {
                    stats.isClimbing = false;
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

                if (!inputs.jump)
                {
                    stats.moveVelocity.y = 0;
                }

                if (rigidBody.linearVelocityY < settings.antiGravApexThreshold)
                {
                    rigidBody.gravityScale = stats.curGravityScale * settings.antiGravMultiplier;
                }
            }
            break;
            case State.Fall:
            {
                if (stats.moveVelocity.y < -settings.antiGravApexThreshold)
                {
                    rigidBody.gravityScale = stats.curGravityScale;
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
                rigidBody.gravityScale = stats.curGravityScale;
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
                stats.moveVelocity.y = settings.jumpVerticalForce;
                stats.coyoteTimeElapsed = Mathf.Infinity;

                stats.lastGroundHeight = transform.position.y;
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
            }
            break;

            case State.Fall:
            {
                stats.coyoteTimeElapsed = 0.0f;
                stats.coyoteJump = false;
                stats.moveVelocity.y = 0;
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
        float stepTop = transform.position.y + settings.topStepBuffer;
        float stepBottom = transform.position.y + settings.bottomStepBuffer;
        float waste = transform.position.y + settings.wasteBuffer;
        float wallLeft = transform.position.x - settings.wallBuffer;
        float wallRight = transform.position.x + settings.wallBuffer;
        float head = transform.position.y + settings.headBuffer;
        collisionData.groundLeft = new Vector2(groundLeft, groundBottom);
        collisionData.groundRight = new Vector2(groundRight, groundBottom);

        collisionData.stepTopLeft = new Vector2(groundLeft, stepTop);
        collisionData.stepTopRight = new Vector2(groundRight, stepTop);

        collisionData.stepBottomLeft = new Vector2(groundLeft, stepBottom);
        collisionData.stepBottomRight = new Vector2(groundRight, stepBottom);

        collisionData.wallTopLeft = new Vector2(wallLeft, head);
        collisionData.wallTopRight = new Vector2(wallRight, head);

        collisionData.wallBottomLeft = new Vector2(wallLeft, waste);
        collisionData.wallBottomRight = new Vector2(wallRight, waste);

    }
    private void OpenTrainDoors()
    {
        if (!stats.onTrain && stats.canBoardTrain)
        {
            RaycastHit2D slideDoorHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, layerSettings.trainLayers.slideDoors);

            if (slideDoorHit.collider != null)
            {
                slideDoors = slideDoorHit.collider.GetComponent<SlideDoors>(); //TODO: Put into dictionary
                if (slideDoors.curState == SlideDoors.State.Unlocked) 
                {
                    slideDoors.OpenDoors();
                }
            }
        }
        else if (stats.onTrain)
        {
            RaycastHit2D left = Physics2D.Linecast(collisionData.wallBottomLeft, collisionData.wallTopLeft, layerSettings.trainLayers.gangwayDoor);
            RaycastHit2D right = Physics2D.Linecast(collisionData.wallBottomRight, collisionData.wallTopRight, layerSettings.trainLayers.gangwayDoor);

            if (left.collider != null)
            {
                gangwayDoor = left.collider.GetComponent<GangwayDoor>();
                gangwayDoor.OpenDoor();
            }
            else if (right.collider != null)
            {
                gangwayDoor = right.collider.GetComponent<GangwayDoor>();
                gangwayDoor.OpenDoor();
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
                    RaycastHit2D insideCarriageHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, layerSettings.trainLayers.insideCarriageBounds);

                    if (insideCarriageHit.collider != null)
                    {
                        SetLocationData(insideCarriageHit.collider.bounds, layerSettings.trainLayers.insideCarriageBounds);
                        curCarriage = trainStats.carriageDict[insideCarriageHit.collider];
                        curCarriage.FadeOut();
                    }
                    stats.curGroundLayer = layerSettings.trainLayers.ground;
                    stats.curWallLayer = layerSettings.trainWallLayers;
                    rigidBody.includeLayers = layerSettings.trainMask;
                    collisionData.stepFilter.layerMask = layerSettings.trainLayers.ground;
                    stats.onTrain = true;
                    UpdateDepth(atlasRenderer.depthOrder).Forget();
                    atlasRenderer.UpdateDepth(trainStats.depthSection_front_min);
                    trainStats.curPassengerCount++;
                    
                    gameEventData.OnBoardingSpy.Raise();
                    
                }
            }
        }
    }

    private async UniTask UpdateDepth(float oldDepth)
    {
        float elaspedTime = 0;

        while(elaspedTime < DEPTH_CHANGE_TIME)
        {
            elaspedTime += Time.deltaTime;
            float t = elaspedTime / DEPTH_CHANGE_TIME;
            t *= t;
            float depth = Mathf.Lerp(oldDepth, atlasRenderer.depthOrder, t);
            matrixMaterial.SetFloat("_PlayerDepth", depth);
            await UniTask.Yield();
        }
        matrixMaterial.SetFloat("_PlayerDepth", atlasRenderer.depthOrder);
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
    private void PlayManualClip(float t)
    {
        atlasRenderer.sprite = curClip.GetNextSpriteManual(t);
    }
    private void OnApplicationQuit()
    {
        stats.ResetStats();
    }
    private void OnDrawGizmos()
    {
        CalculateCollisionPoints();
        Gizmos.color = stats.isGrounded ? Color.limeGreen : Color.orange;
        Gizmos.DrawLine(collisionData.groundLeft, collisionData.groundRight);

        Gizmos.color = stats.isStepping ? Color.springGreen : Color.indianRed;
        Gizmos.DrawLine(collisionData.stepBottomLeft, collisionData.stepTopLeft);
        Gizmos.DrawLine(collisionData.stepBottomRight, collisionData.stepTopRight);

        Gizmos.color = stats.walkingIntoWall ? Color.forestGreen : Color.red;
        Gizmos.DrawLine(collisionData.wallTopLeft, collisionData.wallBottomLeft);
        Gizmos.DrawLine(collisionData.wallTopRight, collisionData.wallBottomRight);

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
