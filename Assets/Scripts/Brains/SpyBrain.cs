using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using static Atlas;
using static Spy;

public class SpyBrain : MonoBehaviour
{
    const float DEPTH_CHANGE_TIME = 0.5f;

    [Header("Components")]
    public Rigidbody2D rigidBody;
    public BoxCollider2D boxCollider;
    public AtlasRenderer atlasRenderer;
    [Header("Scriptable Objects")]
    public SpySettingsSO settings;
    public MaterialIDSO materialIDs;
    public SpyStatsSO stats;
    public PlayerInputsSO playerInputs;
    public TrainStatsSO trainStats;
    public TrainSettingsSO trainSettings;
    public LayerSettingsSO layerSettings;
    public GameEventDataSO gameEventData;
    public TripSO trip;

    [Header("Generated")]
    public AtlasSO atlas;
    public SlideDoors slideDoors;
    public GangwayDoor gangwayDoor;
    public Carriage curCarriage;
    public CarriageMap curCarriageMap;
    public NPCBrain npcTicketCheck;
    public AtlasClip curClip;
    public float clipTime;
    public int curFrameIndex;
    public int prevFrameIndex;

    public bool canExitCheckTicket;
    public bool canExitNotepad;
    public bool checkingCarriageMap;

    public static bool canCheckTicket;
    public static bool checkingTicket;
    public static bool checkingNotepad;

    public CollisionData collisionData;

    public float lastGroundYPos;
    private void OnValidate()
    {
        CalculateCollisionPoints();
    }
    private void OnEnable()
    {     
        gameEventData.OnInteract.RegisterListener(OpenTrainDoors);
        gameEventData.OnInteract.RegisterListener(LookAtCarriageMap);
    }
    private void OnDisable()
    {
        gameEventData.OnInteract.UnregisterListener(OpenTrainDoors);
        gameEventData.OnInteract.UnregisterListener(LookAtCarriageMap);
    }
    private void Start()
    {
        atlas = atlasRenderer.atlas;
        atlas.UpdateClipDictionary();
        stats.curState = SpyState.None;
        rigidBody.gravityScale = settings.gravityScale;
        stats.curGroundLayer = layerSettings.stationLayers.ground;
        stats.curWallLayer = layerSettings.stationWallLayers;
        rigidBody.includeLayers = layerSettings.stationMask;

        collisionData.leftStepResults = new RaycastHit2D[1];
        collisionData.rightStepResults = new RaycastHit2D[1];

        collisionData.stepFilter = new ContactFilter2D() { useLayerMask = true, layerMask = layerSettings.stationLayers.ground };

        stats.curLocationState = LocationState.Station;
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

        Physics2D.Linecast(collisionData.stepBottomLeft, collisionData.stepTopLeft, collisionData.stepFilter, collisionData.leftStepResults);
        Physics2D.Linecast(collisionData.stepBottomRight, collisionData.stepTopRight, collisionData.stepFilter, collisionData.rightStepResults);

        if (collisionData.leftStepResults[0].collider != default)
        {
            Bounds hitColliderHeight = collisionData.leftStepResults[0].collider.bounds;
            if (hitColliderHeight.max.y < collisionData.stepTopLeft.y)
            {
                rigidBody.gravityScale = 0;
                rigidBody.position = new Vector2(rigidBody.position.x - 0.1f, hitColliderHeight.max.y);
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
            }
            collisionData.rightStepResults[0] = default;
        }

        bool leftWallTouch = Physics2D.Linecast(collisionData.wallBottomLeft, collisionData.wallTopLeft, stats.curWallLayer);
        bool rightWallTouch = Physics2D.Linecast(collisionData.wallBottomRight, collisionData.wallTopRight, stats.curWallLayer);

        stats.walkingIntoWall = (leftWallTouch && playerInputs.move == -1) || (rightWallTouch && playerInputs.move == 1);

        rigidBody.linearVelocity = stats.moveVelocity;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (stats.curLocationState == LocationState.Station) return;

        int collisionLayerValue = 1 << collision.gameObject.layer;

        if (((layerSettings.trainLayers.insideCarriageBounds.value & collisionLayerValue) != 0) || (layerSettings.trainLayers.smokingRoom.value & collisionLayerValue) != 0)
        {
            stats.curLocationState = LocationState.Carriage;
            curCarriage = TrainController.GetCarriage(transform.position.x);
            stats.curLocationBounds = curCarriage.insideBoundsCollider.bounds;
        }
        else if ((layerSettings.trainLayers.gangwayBounds.value & (1 << collision.gameObject.layer)) != 0)
        {
            stats.curLocationState = LocationState.Gangway;
            stats.curLocationBounds = collision.bounds;
            curCarriage = null;
        }
    }
    private void ChooseState()
    {
        if ((playerInputs.ticketCheckKeyDown && canCheckTicket) || checkingTicket)
        {
            SetState(SpyState.Ticket);
        }
        else if (playerInputs.notepadKeyDown || checkingNotepad)
        {
            SetState(SpyState.Notepad);
        }
        else if (checkingCarriageMap)
        {
            SetState(SpyState.CarriageMap);
        }
        else if (playerInputs.move != 0 && !playerInputs.run && !stats.walkingIntoWall)
        {
            SetState(SpyState.Walk);
        }
        else
        {
            SetState(SpyState.Idle);
        }
    }
    private void UpdateStates()
    {
        switch (stats.curState)
        {
            case SpyState.Idle:
            {
                atlasRenderer.PlayClip(ref curClip);
            }
            break;
            case SpyState.Walk:
            {
                Flip(playerInputs.move < 0);
                atlasRenderer.PlayClip(ref curClip);
            }
            break;
            case SpyState.Ticket:
            {
                if (!playerInputs.ticketCheckKeyDown) canExitCheckTicket = true;
                if((playerInputs.ticketCheckKeyDown && canExitCheckTicket) || playerInputs.notepadChooseStationAndFlip.x == 1)
                {
                    checkingTicket = false;
                }
                if (playerInputs.notepadChooseStationAndFlip.x == -1)
                {
                    checkingTicket = false;
                }
            }
            break;
            case SpyState.Notepad:
            {
                if(!playerInputs.notepadKeyDown) canExitNotepad = true;

                if (playerInputs.notepadKeyDown && canExitNotepad)
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
            case SpyState.Idle:
            {

            }
            break;
            case SpyState.Walk:
            {
                stats.targetXVelocity = (settings.moveSpeed * playerInputs.move);
                stats.moveVelocity.x = Mathf.Lerp(stats.moveVelocity.x, stats.targetXVelocity, settings.groundAccelation * Time.fixedDeltaTime);
            }
            break;
        }
    }
    private void SetState(SpyState newState)
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
            case SpyState.Idle:
            {
                curClip = atlas.clipDict[(int)SpyMotion.StandingBreathing];
            }
            break;
            case SpyState.Walk:
            {
                curClip = atlas.clipDict[(int)SpyMotion.Walking];
            }
            break;
            case SpyState.Ticket:
            {
                curClip = atlas.clipDict[(int)SpyMotion.StandingBreathing];

                RaycastHit2D npcHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, transform.right, boxCollider.bounds.size.x, layerSettings.npc);

                if (npcHit.collider != null)
                {
                    npcTicketCheck = npcHit.transform.gameObject.GetComponent<NPCBrain>();
                    if (!npcTicketCheck.onTrain || npcTicketCheck.ticketHasBeenChecked) return;
                    npcTicketCheck.ticketIsBeingChecked = true;
                    stats.ticketName = npcTicketCheck.profile.fullName;
                    
                    stats.boardingStationName = trip.stationsDataArray[npcTicketCheck.profile.boardingStationIndex].name;
                    stats.disembarkingStationName = trip.stationsDataArray[npcTicketCheck.profile.disembarkingStationIndex].name;
                    canExitCheckTicket = false;

                    checkingTicket = true;
                }
            }
            break;
            case SpyState.Notepad:
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
            case SpyState.Idle:
            {
                
            }
            break;
            case SpyState.Walk:
            {
                stats.moveVelocity.x = 0;
            }
            break;
            case SpyState.Ticket:
            {
                if (npcTicketCheck != null) npcTicketCheck.ticketIsBeingChecked = false;
            }
            break;

            case SpyState.Notepad:
            {
                checkingNotepad = false;
            }
            break;

            case SpyState.CarriageMap:
            {
                curCarriageMap.CancelEffect();
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
        if (stats.curLocationState == LocationState.Station)
        {
            RaycastHit2D slideDoorHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, layerSettings.trainLayers.exteriorSlideDoors);

            if (slideDoorHit.collider != null)
            {
                slideDoors = slideDoorHit.collider.GetComponent<SlideDoors>(); //TODO: Put into dictionary

                switch(slideDoors.curState)
                {
                    case SlideDoors.State.Unlocked:
                    {
                        slideDoors.OpenDoors();
                    }
                    break;

                    case SlideDoors.State.Opened:
                    {
                        RaycastHit2D insideCarriageHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, layerSettings.trainLayers.insideCarriageBounds);

                        if (insideCarriageHit.collider != null)
                        {
                            curCarriage = TrainController.GetCarriage(transform.position.x);
                            curCarriage.MoveDown();
                        }
                        stats.curGroundLayer = layerSettings.trainLayers.ground;
                        stats.curWallLayer = layerSettings.trainWallLayers;
                        stats.curLocationState = LocationState.Carriage;

                        rigidBody.includeLayers = layerSettings.trainMask;

                        collisionData.stepFilter.layerMask = layerSettings.trainLayers.ground;
                        atlasRenderer.UpdateDepthRealtime(trainStats.depthSections.frontMin);
                    }
                    break;
                }
            }
        }
        else
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
    private void LookAtCarriageMap()
    {
        if (curCarriage == null) return;

        if (!checkingCarriageMap)
        {
            RaycastHit2D carriageMapHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, layerSettings.trainLayers.carriageMap);
            if (carriageMapHit.collider != null)
            {
                curCarriageMap= curCarriage.map;
                curCarriageMap.InteractEffect();
                checkingCarriageMap = true;
            }
        }
        else
        {
            checkingCarriageMap = false;
        }
    }
    private void Flip(bool flip)
    {
        stats.spriteFlip = flip;
        atlasRenderer.FlipH(flip, atlasRenderer.sprite);
    }
    public static void ToggleTicketCheckAbility(bool toggle)
    {
        canCheckTicket = toggle;
    }
    public static void ToggleNotepad(bool toggle)
    {
        checkingNotepad = toggle;
    }
    private void OnDrawGizmos()
    {
        CalculateCollisionPoints();
        Gizmos.color = Color.orange;
        Gizmos.DrawLine(collisionData.groundLeft, collisionData.groundRight);

        Gizmos.color = Color.indianRed;
        Gizmos.DrawLine(collisionData.stepBottomLeft, collisionData.stepTopLeft);
        Gizmos.DrawLine(collisionData.stepBottomRight, collisionData.stepTopRight);

        Gizmos.color = stats.walkingIntoWall ? Color.forestGreen : Color.red;
        Gizmos.DrawLine(collisionData.wallTopLeft, collisionData.wallBottomLeft);
        Gizmos.DrawLine(collisionData.wallTopRight, collisionData.wallBottomRight);

    }

}
