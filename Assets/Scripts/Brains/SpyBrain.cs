using UnityEngine;

using static Atlas;
using static AtlasUI;
using static Spy;

public class SpyBrain : MonoBehaviour
{
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
    public CameraStatsSO camStats;
    public NotepadData notepadData;
    public TripSO trip;

    [Header("Generated")]
    public AtlasSO atlas;
    public SlideDoors slideDoors;
    public HingedDoor smokingRoomDoor;
    public GangwayDoor curGangwayDoor;
    public Carriage curCarriage;
    public CarriageMap curCarriageMap;
    public NPCBrain npcTicketCheck;
    public AtlasClip curClip;
    public NotepadState curNotepadState;

    public float clipTime;
    public int curFrameIndex;
    public int prevFrameIndex;

    public bool wasTouchingGangwayDoorLeft;
    public bool wasTouchingGangwayDoorRight;

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

        gameEventData.OnStationLeave.RegisterListener(MoveUpCurrentCarriageWall);
    }
    private void OnDisable()
    {
        gameEventData.OnInteract.UnregisterListener(OpenTrainDoors);
        gameEventData.OnInteract.UnregisterListener(LookAtCarriageMap);

        gameEventData.OnStationLeave.UnregisterListener(MoveUpCurrentCarriageWall);

        stats.ticketsCheckedTotal = 0;
        stats.signedNotepad = false;
    }
    private void Start()
    {
        atlas = atlasRenderer.atlas;
        atlas.UpdateClipDictionary();
        stats.curState = SpyState.None;
        stats.curGroundLayer = layerSettings.stationLayers.ground;
        stats.curWallLayer = layerSettings.stationWallLayers;
        rigidBody.includeLayers = layerSettings.stationMask;

        stats.curLocationState = LocationState.Station;

        stats.curWorldPos = transform.position;
    }
    private void Update()
    {
        ChooseState();
        UpdateStates();
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        bool leftWallTouch = Physics2D.Linecast(boxCollider.bounds.center, collisionData.wallLeft, stats.curWallLayer);
        bool rightWallTouch = Physics2D.Linecast(boxCollider.bounds.center, collisionData.wallRight, stats.curWallLayer);

        stats.walkingIntoWall = (leftWallTouch && playerInputs.move == -1) || (rightWallTouch && playerInputs.move == 1);

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

                stats.targetXVelocity = (settings.moveSpeed * playerInputs.move);
                stats.moveVelocity.x = Mathf.Lerp(stats.moveVelocity.x, stats.targetXVelocity, settings.groundAccelation * Time.deltaTime);

                stats.curWorldPos.x += stats.moveVelocity.x * Time.deltaTime;
                stats.curWorldPos.y = transform.position.y;
                stats.curWorldPos.z = transform.position.z;
                transform.position = stats.curWorldPos;
            }
            break;
            case SpyState.Ticket:
            {
                if (!playerInputs.ticketCheckKeyDown) canExitCheckTicket = true;
                if((playerInputs.ticketCheckKeyDown && canExitCheckTicket) || playerInputs.notepadPreviewAnswerAndFlip.x == 1)
                {
                    checkingTicket = false;
                }
                if (playerInputs.notepadPreviewAnswerAndFlip.x == -1)
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

                if (notepadData.curState != curNotepadState)
                {
                    switch (notepadData.curState)
                    {
                        case NotepadState.FlippingUp:
                        {
                            curClip = atlas.clipDict[(int)SpyMotion.NotepadFlipping];
                            atlasRenderer.PlayClipOneShot(curClip);
                        }
                        break;
                        case NotepadState.FlippingDown:
                        {
                            curClip = atlas.clipDict[(int)SpyMotion.NotepadFlipping];
                            atlasRenderer.PlayClipOneShotReverse(curClip);

                        }
                        break;
                        case NotepadState.Stationary:
                        {
                            curClip = atlas.clipDict[(int)SpyMotion.NotepadHolding];
                        }
                        break;
                        case NotepadState.Writing:
                        {
                            curClip = atlas.clipDict[(int)SpyMotion.NotepadWriting];
                            atlasRenderer.PlayClipOneShot(curClip);
                        }
                        break;
                        case NotepadState.Erasing:
                        {
                            curClip = atlas.clipDict[(int)SpyMotion.NotepadWriting];
                            atlasRenderer.PlayClipOneShotReverse(curClip);

                        }
                        break;
                    }
                    curNotepadState = notepadData.curState;
                }

                if (curNotepadState == NotepadState.Stationary)
                {
                    atlasRenderer.PlayClip(ref curClip);
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
                if (stats.curLocationState != LocationState.Station)
                {
                    CalculateCollisionPoints();

                    RaycastHit2D gangwayDoorLeftHit = Physics2D.Linecast(boxCollider.bounds.center, collisionData.wallLeft, layerSettings.trainLayers.gangwayDoor);
                    RaycastHit2D gangwayDoorRightHit = Physics2D.Linecast(boxCollider.bounds.center, collisionData.wallRight, layerSettings.trainLayers.gangwayDoor);
                    bool isTouchingGangwayDoorLeft = gangwayDoorLeftHit.collider != null;
                    bool isTouchingGangwayDoorRight = gangwayDoorRightHit.collider != null;

                    if ((!isTouchingGangwayDoorRight && wasTouchingGangwayDoorRight) && !isTouchingGangwayDoorLeft)
                    {
                        if (curGangwayDoor.isLeftOfCarriage)
                        {
                            curGangwayDoor.carriage.MoveUp();
                            stats.curLocationBounds = curGangwayDoor.gangway.exteriorRenderer.bounds;
                            stats.curLocationState = LocationState.Gangway;
                        }
                        else
                        {
                            curGangwayDoor.gangway.MoveUp();
                            stats.curLocationBounds = curGangwayDoor.carriage.totalBounds;
                            stats.curLocationState = LocationState.Carriage;
                        }
                        curGangwayDoor.CloseDoors();
                    }
                    else if ((!isTouchingGangwayDoorLeft && wasTouchingGangwayDoorLeft) && !isTouchingGangwayDoorRight)
                    {
                        if (curGangwayDoor.isLeftOfCarriage)
                        {
                            curGangwayDoor.gangway.MoveUp();
                            curGangwayDoor.carriage.MoveDown();
                            stats.curLocationBounds = curGangwayDoor.carriage.totalBounds;
                            stats.curLocationState = LocationState.Carriage;
                        }
                        else
                        {
                            curGangwayDoor.carriage.MoveUp();
                            curGangwayDoor.gangway.MoveDown();
                            stats.curLocationBounds = curGangwayDoor.gangway.exteriorRenderer.bounds;
                            stats.curLocationState = LocationState.Gangway;
                        }
                        curGangwayDoor.CloseDoors();

                    }
                    else if ((isTouchingGangwayDoorLeft && !wasTouchingGangwayDoorLeft) && !isTouchingGangwayDoorRight)
                    {
                        curGangwayDoor = gangwayDoorLeftHit.collider.GetComponent<GangwayDoor>();
                        if (curGangwayDoor.isLeftOfCarriage)
                        {
                            curGangwayDoor.gangway.MoveDown();
                        }
                        else
                        {
                            curGangwayDoor.carriage.MoveDown();
                        }
                    }
                    else if ((isTouchingGangwayDoorRight && !wasTouchingGangwayDoorRight) && !isTouchingGangwayDoorLeft)
                    {
                        curGangwayDoor = gangwayDoorRightHit.collider.GetComponent<GangwayDoor>();
                    }
                    wasTouchingGangwayDoorLeft = isTouchingGangwayDoorLeft;
                    wasTouchingGangwayDoorRight = isTouchingGangwayDoorRight;
                }
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
                curClip = atlas.clipDict[(int)SpyMotion.Ticket];
                atlasRenderer.PlayClipOneShot(curClip);

                RaycastHit2D npcHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, transform.right, boxCollider.bounds.size.x, layerSettings.npc);

                if (npcHit.collider != null)
                {
                    npcTicketCheck = npcHit.transform.gameObject.GetComponent<NPCBrain>();
                    if (!npcTicketCheck.onTrain || npcTicketCheck.ticketHasBeenChecked) return;
                    npcTicketCheck.ticketIsBeingChecked = true;
                    
                    stats.boardingStationName = trip.stationsDataArray[npcTicketCheck.profile.boardingStationIndex].name;
                    stats.disembarkingStationName = trip.stationsDataArray[npcTicketCheck.profile.disembarkingStationIndex].name;
                    canExitCheckTicket = false;
                    stats.ticketsCheckedTotal++;
                    checkingTicket = true;

                    gameEventData.OnTicketInspect.Raise();
                }
            }
            break;
            case SpyState.Notepad:
            {
                curClip = atlas.clipDict[(int)SpyMotion.NotepadHolding];
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
                if (npcTicketCheck != null)
                {
                    trip.ticketsCheckedSinceLastStation++;
                    npcTicketCheck.ticketIsBeingChecked = false;
                    atlasRenderer.PlayClipOneShotReverse(curClip);
                    npcTicketCheck = null;
                }
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
        float wallLeft = boxCollider.bounds.center.x - settings.wallWidthBuffer;
        float wallRight = boxCollider.bounds.center.x + settings.wallWidthBuffer;
        collisionData.groundLeft = new Vector2(groundLeft, groundBottom);
        collisionData.groundRight = new Vector2(groundRight, groundBottom);

        collisionData.wallLeft = new Vector2(wallLeft, boxCollider.bounds.center.y);
        collisionData.wallRight = new Vector2(wallRight, boxCollider.bounds.center.y);

    }
    private void OpenTrainDoors()
    {
        if (stats.curLocationState == LocationState.Station || (stats.curLocationState == LocationState.Carriage && stats.signedNotepad))
        {
            RaycastHit2D slideDoorHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, trainStats.activeSlideDoorsMask);

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
                        if (stats.signedNotepad)
                        {
                            stats.curGroundLayer = layerSettings.stationLayers.ground;
                            stats.curWallLayer = layerSettings.stationWallLayers;
                            stats.curLocationState = LocationState.Station;
                            rigidBody.includeLayers = layerSettings.stationMask;

                            Station station = TrainController.NextStationInstance;
                            AtlasRenderer stationPlatform = station.station.isFrontOfTrain ? station.frontPlatformRenderer : station.backPlatformRenderer;
                            transform.SetParent(stationPlatform.transform, true);
                            atlasRenderer.UpdateDepthRealtime((int)stationPlatform.transform.position.z);
                        }
                        else
                        {
                            RaycastHit2D insideCarriageHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, layerSettings.trainLayers.insideCarriageBounds);

                            if (insideCarriageHit.collider != null)
                            {
                                curCarriage = TrainController.GetCarriage(insideCarriageHit.collider);
                                curCarriage.MoveDown();
                            }
                            stats.curGroundLayer = layerSettings.trainLayers.ground;
                            stats.curWallLayer = layerSettings.trainWallLayers;
                            stats.curLocationState = LocationState.Carriage;
                            stats.curLocationBounds = curCarriage.totalBounds;
                            rigidBody.includeLayers = layerSettings.trainMask;

                            atlasRenderer.UpdateDepthRealtime(trainStats.depthSections.frontMin);
                        }
                    }
                    break;
                }
            }
        }
        else
        {
            if (atlasRenderer.flipX)
            {
                RaycastHit2D leftSmokingRoomDoorHit = Physics2D.Linecast(boxCollider.bounds.center, collisionData.wallLeft, layerSettings.trainLayers.smokingRoomDoor);
                if (leftSmokingRoomDoorHit.collider != null)
                {
                    smokingRoomDoor = leftSmokingRoomDoorHit.collider.GetComponent<HingedDoor>();
                    smokingRoomDoor.OpenDoor();
                }
                else
                {
                    RaycastHit2D leftGangwayDoorHit = Physics2D.Linecast(boxCollider.bounds.center, collisionData.wallLeft, layerSettings.trainLayers.gangwayDoor);
                    if (leftGangwayDoorHit.collider != null)
                    {
                        curGangwayDoor = leftGangwayDoorHit.collider.GetComponent<GangwayDoor>();
                        curGangwayDoor.OpenDoors();

                        if (curGangwayDoor.isLeftOfCarriage)
                        {
                            stats.curLocationBounds = curGangwayDoor.gangway.exteriorRenderer.bounds;
                            stats.curLocationState = LocationState.Gangway;
                        }
                        else
                        {
                            stats.curLocationBounds = curGangwayDoor.carriage.totalBounds;
                            stats.curLocationState = LocationState.Carriage;
                        }

                    }
                }

            }
            else
            {
                RaycastHit2D rightSmokingRoomDoorHit = Physics2D.Linecast(boxCollider.bounds.center, collisionData.wallRight, layerSettings.trainLayers.smokingRoomDoor);

                if (rightSmokingRoomDoorHit.collider != null)
                {
                    smokingRoomDoor = rightSmokingRoomDoorHit.collider.GetComponent<HingedDoor>();
                    smokingRoomDoor.OpenDoor();
                }
                else
                {
                    RaycastHit2D rightGangwayDoorHit = Physics2D.Linecast(boxCollider.bounds.center, collisionData.wallRight, layerSettings.trainLayers.gangwayDoor);
                    if (rightGangwayDoorHit.collider != null)
                    {
                        curGangwayDoor = rightGangwayDoorHit.collider.GetComponent<GangwayDoor>();
                        curGangwayDoor.OpenDoors();
                        if (curGangwayDoor.isLeftOfCarriage)
                        {
                            curGangwayDoor.carriage.MoveDown();
                        }
                        else
                        {
                            curGangwayDoor.gangway.MoveDown();
                        }
                    }

                }
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
    private void MoveUpCurrentCarriageWall()
    {
        if (stats.signedNotepad)
        {
            curCarriage.MoveUp();
        }
    }
    private void Flip(bool flip)
    {
        stats.spriteFlip = flip;
        atlasRenderer.FlipH(flip);
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

        Gizmos.color = stats.walkingIntoWall ? Color.forestGreen : Color.red;
        Gizmos.DrawLine(collisionData.wallLeft, boxCollider.bounds.center);
        Gizmos.DrawLine(collisionData.wallRight, boxCollider.bounds.center);

    }
}
