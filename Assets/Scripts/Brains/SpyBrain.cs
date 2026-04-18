using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using static Atlas;

public class SpyBrain : MonoBehaviour
{
    const float DEPTH_CHANGE_TIME = 0.5f;
    public enum State
    {
        None,
        Idle,
        Walk,
        Run,
        Ticket,
        Notepad,
        Map,
    }


    [Header("Components")]
    [SerializeField] Rigidbody2D rigidBody;
    [SerializeField] BoxCollider2D boxCollider;
    [SerializeField] AtlasRenderer atlasRenderer;
    [Header("Scriptable Objects")]
    [SerializeField] SpySettingsSO settings;
    [SerializeField] MaterialIDSO materialIDs;
    [SerializeField] SpyStatsSO stats;
    [SerializeField] PlayerInputsSO playerInputs;
    [SerializeField] TrainStatsSO trainStats;
    [SerializeField] TrainSettingsSO trainSettings;
    [SerializeField] LayerSettingsSO layerSettings;
    [SerializeField] GameEventDataSO gameEventData;
    public TripSO trip;

    [SerializeField] Material matrixMaterial;

    [Header("Generated")]
    public AtlasSO atlas;
    public SlideDoors slideDoors;
    public GangwayDoor gangwayDoor;
    public Carriage curCarriage;
    public NPCBrain npcTicketCheck;
    public AtlasClip curClip;
    public float clipTime;
    public int curFrameIndex;
    public int prevFrameIndex;

    public bool canExitCheckTicket;
    public bool canExitNotepad;
    public bool checkingTicket;
    public bool checkingNotepad;
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
        gameEventData.OnTrainArrivedAtStartPosition.RegisterListener(SpyCanBoardTrain);
        gameEventData.OnInteract.RegisterListener(OpenTrainDoors);
        gameEventData.OnInteract.RegisterListener(EnterTrain);
    }
    private void OnDisable()
    {
        gameEventData.OnTrainArrivedAtStartPosition.UnregisterListener(SpyCanBoardTrain);
        gameEventData.OnInteract.UnregisterListener(OpenTrainDoors);
        gameEventData.OnInteract.UnregisterListener(EnterTrain);

    }
    private void Start()
    {
        atlas = atlasRenderer.atlas;
        atlas.UpdateClipDictionary();
        stats.curState = State.None;
        rigidBody.gravityScale = settings.gravityScale;
        stats.curGravityScale = rigidBody.gravityScale;
        stats.curGroundLayer = layerSettings.stationLayers.ground;
        stats.curWallLayer = layerSettings.stationWallLayers;
        rigidBody.includeLayers = layerSettings.stationMask;

        collisionData.leftStepResults = new RaycastHit2D[1];
        collisionData.rightStepResults = new RaycastHit2D[1];

        collisionData.stepFilter = new ContactFilter2D() { useLayerMask = true, layerMask = layerSettings.stationLayers.ground };

        stats.maxJumpHeight = (settings.jumpVerticalForce * settings.jumpVerticalForce) / (2 * settings.gravityScale);

        matrixMaterial.SetFloat("_PlayerDepth", atlasRenderer.batchKey.depthOrder);
    }
    private void Update()
    {
        ChooseState();
        UpdateStates();

        stats.curWorldPos = transform.position; 
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

        stats.walkingIntoWall = (leftWallTouch && playerInputs.move == -1) || (rightWallTouch && playerInputs.move == 1);

        rigidBody.linearVelocity = stats.moveVelocity;
        stats.moveVelocity.y -= stats.curGravityScale * Time.fixedDeltaTime;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!stats.onTrain) return;

        if ((layerSettings.trainLayers.insideCarriageBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            SetLocationData(collision.bounds, layerSettings.trainLayers.insideCarriageBounds);
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
        }
    }
    private void ChooseState()
    {
        if ((playerInputs.ticketPressed && stats.onTrain) || checkingTicket)
        {
            SetState(State.Ticket);
        }
        else if (playerInputs.notepadToggle || checkingNotepad)
        {
            SetState(State.Notepad);
        }
        else if (stats.isGrounded && playerInputs.move != 0 && !playerInputs.run && !stats.walkingIntoWall)
        {
            SetState(State.Walk);
        }
        else if (stats.isGrounded && playerInputs.move != 0 && playerInputs.run && !stats.walkingIntoWall) //TODO: not in carriage
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
        switch (stats.curState)
        {
            case State.Idle:
            {
                atlasRenderer.PlayClip(ref curClip);
            }
            break;
            case State.Walk:
            {
                Flip(playerInputs.move < 0);
                atlasRenderer.PlayClip(ref curClip);
            }
            break;
            case State.Run:
            {
                Flip(playerInputs.move < 0);
                atlasRenderer.PlayClip(ref curClip);
            }
            break;
            case State.Ticket:
            {
                if (!playerInputs.ticketPressed) canExitCheckTicket = true;
                if((playerInputs.ticketPressed && canExitCheckTicket) || playerInputs.notepadChooseStationAndFlip.x == 1)
                {
                    checkingTicket = false;
                }
                if (playerInputs.notepadChooseStationAndFlip.x == -1)
                {
                    checkingTicket = false;
                    checkingNotepad = true;
                }
            }
            break;
            case State.Notepad:
            {
                if(!playerInputs.notepadToggle) canExitNotepad = true;

                if (playerInputs.notepadToggle && canExitNotepad)
                {
                    checkingNotepad = false;
                }
            }
            break;
        }
    }
    private void FixedUpdateStates()
    {
        switch (stats.curState)
        {
            case State.Idle:
            {

            }
            break;
            case State.Walk:
            {
                stats.targetXVelocity = (settings.moveSpeed * playerInputs.move);
                stats.moveVelocity.x = Mathf.Lerp(stats.moveVelocity.x, stats.targetXVelocity, settings.groundAccelation * Time.fixedDeltaTime);
            }
            break;
            case State.Run:
            {
                stats.targetXVelocity = settings.moveSpeed * playerInputs.move * settings.runSpeedMultiplier;
                stats.moveVelocity.x = Mathf.Lerp(stats.moveVelocity.x, stats.targetXVelocity, settings.groundAccelation * Time.fixedDeltaTime);
            }
            break;
        }
    }
    private void SetState(State newState)
    {
        if (stats.curState == newState) return;
        ExitState();
        stats.curState = newState;
        EnterState();
    }
    private void EnterState()
    {
        switch (stats.curState)
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
            }
            break;
            case State.Ticket:
            {
                curClip = atlas.clipDict[(int)SpyMotion.StandingBreathing];
                rigidBody.gravityScale = stats.curGravityScale;

                RaycastHit2D npcHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, transform.right, boxCollider.bounds.size.x, layerSettings.npc);

                if (npcHit.collider != null)
                {
                    npcTicketCheck = npcHit.transform.gameObject.GetComponent<NPCBrain>();
                    if (!npcTicketCheck.IsOnTrain()) return;
                    npcTicketCheck.ticketIsBeingChecked = true;
                    stats.ticketName = npcTicketCheck.profile.fullName;
                    stats.boardingStationName = trip.stationsDataArray[npcTicketCheck.profile.boardingStationIndex].name;
                    stats.disembarkingStationName = trip.stationsDataArray[npcTicketCheck.profile.disembarkingStationIndex].name;
                    canExitCheckTicket = false;
                    checkingTicket = true;
                }
            }
            break;
            case State.Notepad:
            {
                checkingNotepad = true;
                canExitNotepad = false;
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (stats.curState)
        {
            case State.Idle:
            {
                
            }
            break;
            case State.Walk:
            {
                stats.moveVelocity.x = 0;
            }
            break;
            case State.Run:
            {
                stats.moveVelocity.x = 0;
            }
            break;
            case State.Ticket:
            {
                if (npcTicketCheck != null) npcTicketCheck.ticketIsBeingChecked = false;
            }
            break;

            case State.Notepad:
            {
                checkingNotepad = false;
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
                        curCarriage.MoveDown();
                    }
                    stats.curGroundLayer = layerSettings.trainLayers.ground;
                    stats.curWallLayer = layerSettings.trainWallLayers;
                    stats.onTrain = true;

                    rigidBody.includeLayers = layerSettings.trainMask;

                    collisionData.stepFilter.layerMask = layerSettings.trainLayers.ground;

                    UpdateDepth(atlasRenderer.batchKey.depthOrder).Forget();
                    atlasRenderer.UpdateDepthRealtime(trainStats.depthSections.frontMin);

                    trainStats.curPassengersBoarded++;
                    
                    gameEventData.OnBoardingSpy.Raise();
                    
                }
            }
        }
    }

    private void SpyCanBoardTrain()
    {
        stats.canBoardTrain = true;
    }
    private async UniTask UpdateDepth(float oldDepth)
    {
        float elaspedTime = 0;

        while(elaspedTime < DEPTH_CHANGE_TIME)
        {
            elaspedTime += Time.deltaTime;
            float t = elaspedTime / DEPTH_CHANGE_TIME;
            t *= t;
            float depth = Mathf.Lerp(oldDepth, atlasRenderer.batchKey.depthOrder, t);
            matrixMaterial.SetFloat("_PlayerDepth", depth);
            await UniTask.Yield();
        }
        matrixMaterial.SetFloat("_PlayerDepth", atlasRenderer.batchKey.depthOrder);
    }
    private void SetLocationData(Bounds bounds, LayerMask layerMask)
    {
        stats.curLocationBounds = bounds;
        stats.curLocationLayer = layerMask;
    }
    private void Flip(bool flip)
    {
        stats.spriteFlip = flip;
        atlasRenderer.FlipH(flip, atlasRenderer.sprite);
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
