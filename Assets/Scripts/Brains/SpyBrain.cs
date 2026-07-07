using System;
using System.Linq;
using UnityEngine;

using static Atlas;
using static AtlasUI;
using static Spy;
using static NPC;
public class SpyBrain : MonoBehaviour
{
    public static Carriage CurCarriage;
    
    public static NPCBrain ChosenNPC;

    public static event Action OnTicketCheckHoverEnabled;
    public static event Action OnTicketCheckHoverDisabled;

    public static bool CanCheckTicket;
    public static bool CheckingNotepad;
    public static bool PickingNPCToTicketCheck;

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
    public NPCBrain[] possibleNPCsToTicketCheck;

    public AtlasSO atlas;
    
    public SlideDoors slideDoors;
    
    public HingedDoor smokingRoomDoor;
    
    public GangwayDoor curGangwayDoor;
    
    public CarriageMap curCarriageMap;


    public AtlasClip curClip;

    public CollisionData collisionData;
    
    public NotepadState curNotepadState;

    public float clipTime;
    public float lastGroundYPos;
    
    public int curFrameIndex;
    public int prevFrameIndex;
    public int curNPCTicketCheckHoverCount;

    public bool wasTouchingGangwayDoorLeft;
    public bool wasTouchingGangwayDoorRight;
    public bool canExitState;
    public bool checkingCarriageMap;

    private void OnValidate()
    {
        CalculateCollisionPoints();
    }
    private void OnEnable()
    {     
        gameEventData.OnInteract.RegisterListener(OpenSlideDoors);
        gameEventData.OnInteract.RegisterListener(LookAtCarriageMap);

        gameEventData.OnStationLeave.RegisterListener(MoveUpCurrentCarriageWall);
    }
    private void OnDisable()
    {
        gameEventData.OnInteract.UnregisterListener(OpenSlideDoors);
        gameEventData.OnInteract.UnregisterListener(LookAtCarriageMap);

        gameEventData.OnStationLeave.UnregisterListener(MoveUpCurrentCarriageWall);

        stats.ticketsCheckedTotal = 0;
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

        possibleNPCsToTicketCheck = new NPCBrain[8];
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
        if ((playerInputs.ticketCheckKeyDown && CanCheckTicket && curNPCTicketCheckHoverCount == 1) || ChosenNPC != null)
        {
            if (ChosenNPC == null)
            {
                ChosenNPC = possibleNPCsToTicketCheck[0];
            }

            if (ChosenNPC.role == Role.Accomplice)
            {
                SetState(SpyState.TalkingToAccomplice);
            }
            else
            {
                SetState(SpyState.TicketCheck);
            }
        }
        else if ((playerInputs.ticketCheckKeyDown && CanCheckTicket && curNPCTicketCheckHoverCount > 1) || PickingNPCToTicketCheck)
        {
            SetState(SpyState.PickingNPCTicketCheck);
        }
        else if (playerInputs.notepadKeyDown || CheckingNotepad)
        {
            SetState(SpyState.Notepad);
        }
        else if (checkingCarriageMap)
        {
            SetState(SpyState.CarriageMap);
        }
        else if (playerInputs.move != 0 && !stats.walkingIntoWall)
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
            case SpyState.TicketCheck:
            {
                if((playerInputs.ticketCheckKeyUp || playerInputs.mouseLeftUp || playerInputs.moveDown)  && canExitState)
                {
                    ChosenNPC.ToggleUnveil(true);
                    FinishWithChosenNPC();
                }
                if (!playerInputs.ticketCheckKeyHold && !playerInputs.mouseLeftHold && playerInputs.move == 0) canExitState = true;
            }
            break;
            case SpyState.TalkingToAccomplice:
            {
                if ((playerInputs.ticketCheckKeyUp || playerInputs.mouseLeftUp || playerInputs.moveDown) && canExitState)
                {
                    SceneController.GetUnlockPicker().Close();
                    FinishWithChosenNPC();
                }

                if (!playerInputs.ticketCheckKeyHold && !playerInputs.mouseLeftHold && playerInputs.move == 0) canExitState = true;
            }
            break;

            case SpyState.PickingNPCTicketCheck:
            {
                if ((playerInputs.mouseLeftUp || playerInputs.move != 0) && canExitState)
                {
                    PickingNPCToTicketCheck = false;
                }

                if (playerInputs.mouseLeftUp) canExitState = true;
            }
            break;

            case SpyState.Notepad:
            {
                if(!playerInputs.notepadKeyDown) canExitState = true;

                if (playerInputs.notepadKeyDown && canExitState)
                {
                    CheckingNotepad = false;
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
                CheckIfTicketCheckHover();
            }
            break;
            case SpyState.Walk:
            {
                CalculateCollisionPoints();
                if (stats.curLocationState != LocationState.Station)
                {

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
                            CurCarriage = curGangwayDoor.carriage;
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
                        curGangwayDoor.OpenDoors();
                        if (curGangwayDoor.isLeftOfCarriage)
                        {
                            curGangwayDoor.gangway.MoveDown();
                        }
                        else
                        {
                            curGangwayDoor.carriage.MoveDown();
                            CurCarriage = curGangwayDoor.carriage;
                        }
                    }
                    else if ((isTouchingGangwayDoorRight && !wasTouchingGangwayDoorRight) && !isTouchingGangwayDoorLeft)
                    {
                        curGangwayDoor = gangwayDoorRightHit.collider.GetComponent<GangwayDoor>();
                        curGangwayDoor.OpenDoors();
                    }
                    wasTouchingGangwayDoorLeft = isTouchingGangwayDoorLeft;
                    wasTouchingGangwayDoorRight = isTouchingGangwayDoorRight;

                    CheckIfTicketCheckHover();
                }
            }
            break;
            case SpyState.TicketCheck:
            {

            }
            break;
            case SpyState.PickingNPCTicketCheck:
            {

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
        canExitState = false;

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
            case SpyState.TicketCheck:
            {
                ChosenNPC.talkingToSpy = true;

                curClip = atlas.clipDict[(int)SpyMotion.Ticket];
                atlasRenderer.PlayClipOneShot(curClip);

                stats.boardingStationName = trip.stationsDataArray[ChosenNPC.profile.boardingStationIndex].name;
                stats.disembarkingStationName = trip.stationsDataArray[ChosenNPC.profile.disembarkingStationIndex].name;
                stats.ticketsCheckedTotal++;
                gameEventData.OnTicketInspect.Raise();

                canExitState = false;
            }
            break;

            case SpyState.TalkingToAccomplice:
            {
                ChosenNPC.talkingToSpy = true;

                curClip = atlas.clipDict[(int)SpyMotion.StandingBreathing];
                atlasRenderer.PlayClipOneShot(curClip);
                canExitState = false;

                UnlockType curUnlockType = UnlockType.Color;

                if ((trip.curUnlocks & UnlockType.RuleOut) == 0)
                {
                    curUnlockType = UnlockType.RuleOut;
                }
                else if ((trip.curUnlocks & UnlockType.Color) == 0)
                {
                    curUnlockType = UnlockType.Color;
                }
                else if ((trip.curUnlocks & UnlockType.MultiColor) == 0)
                {
                    curUnlockType = UnlockType.MultiColor;
                }
                SceneController.GetUnlockPicker().Open(unlockSelectionAmount: 1, curUnlockType, ChosenNPC);
            }
            break;

            case SpyState.PickingNPCTicketCheck:
            {
                PickingNPCToTicketCheck = true;
                NPCPicker npcPicker = SceneController.GetNPCPicker();
                QuickSortNPCByXPos(possibleNPCsToTicketCheck, 0, curNPCTicketCheckHoverCount - 1);

                for (int i = 0; i < curNPCTicketCheckHoverCount; i++)
                {
                    possibleNPCsToTicketCheck[i].talkingToSpy = true;
                }

                npcPicker.Open(possibleNPCsToTicketCheck, curNPCTicketCheckHoverCount, PickerFunctionType.TicketCheck);

            }
            break;
            case SpyState.Notepad:
            {
                curClip = atlas.clipDict[(int)SpyMotion.NotepadHolding];
                CheckingNotepad = true;
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
            case SpyState.TicketCheck:
            {
                trip.ticketsCheckedSinceLastStation++;
                atlasRenderer.PlayClipOneShotReverse(curClip);
                if (trip.ticketsCheckedSinceLastStation == trip.stationAhead.ticketsToCheckBeforeSpawn)
                {
                    CanCheckTicket = false;
                }
            }
            break;

            case SpyState.TalkingToAccomplice:
            {
            }
            break;

            case SpyState.PickingNPCTicketCheck:
            {
                PickingNPCToTicketCheck = false;
                NPCPicker npcPicker = SceneController.GetNPCPicker();
                npcPicker.Close();

                for (int i = 0; i < curNPCTicketCheckHoverCount; i++)
                {
                    NPCBrain npc = possibleNPCsToTicketCheck[i];
                    if (npc != ChosenNPC)
                    {
                        npc.talkingToSpy = false;
                        npc.ToggleTicketCheckHover(false);
                    }
                }
            }
            break;
            case SpyState.Notepad:
            {
                CheckingNotepad = false;
            }
            break;

            case SpyState.CarriageMap:
            {
                curCarriageMap.CancelEffect();
            }
            break;
        }
    }
    private void CheckIfTicketCheckHover()
    {
        if (CanCheckTicket)
        {
            Bounds spyBounds = atlasRenderer.bounds;

            curNPCTicketCheckHoverCount = 0;

            for (int i = 0; i < CurCarriage.curNPCList.Count; i++)
            {
                if (curNPCTicketCheckHoverCount < possibleNPCsToTicketCheck.Length)
                {
                    NPCBrain npc = CurCarriage.curNPCList[i];
                    if (npc.ticketHasBeenChecked) continue;

                    Bounds npcBounds = npc.atlasRenderer.bounds;

                    if (spyBounds.max.x > npcBounds.min.x && spyBounds.min.x < npcBounds.max.x)
                    {
                        npc.ToggleTicketCheckHover(toggle: true);
                        possibleNPCsToTicketCheck[curNPCTicketCheckHoverCount] = npc;
                        curNPCTicketCheckHoverCount++;
                        OnTicketCheckHoverEnabled.Invoke();
                    }
                    else
                    {
                        npc.ToggleTicketCheckHover(toggle: false);
                    }
                }
            }

            if (curNPCTicketCheckHoverCount == 0)
            {
                OnTicketCheckHoverDisabled.Invoke();
            }
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
    private void OpenSlideDoors()
    {
        RaycastHit2D slideDoorHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, trainStats.activeSlideDoorsMask);

        if (slideDoorHit.collider == null) return;

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
                switch (stats.curLocationState)
                {
                    case LocationState.Carriage:
                    {
                        if (trainStats.curStationIndex > 0)
                        {
                            stats.curGroundLayer = layerSettings.stationLayers.ground;
                            stats.curWallLayer = layerSettings.stationWallLayers;
                            stats.curLocationState = LocationState.Station;

                            rigidBody.includeLayers = layerSettings.stationMask;

                            Station station = TrainController.NextStationInstance;
                        
                            AtlasRenderer stationPlatform = station.station.isFrontOfTrain ? station.frontPlatformRenderer : station.backPlatformRenderer;
                        
                            transform.SetParent(stationPlatform.transform, true);
                        
                            atlasRenderer.UpdateWorldDepth((int)stationPlatform.transform.position.z);
                        }
                    }
                    break;

                    case LocationState.Station:
                    {
                        RaycastHit2D insideCarriageHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, layerSettings.trainLayers.insideCarriageBounds);

                        if (insideCarriageHit.collider != null)
                        {
                            CurCarriage = slideDoors.carriage;
                            CurCarriage.MoveDown();
                        }
                        stats.curGroundLayer = layerSettings.trainLayers.ground;
                        stats.curWallLayer = layerSettings.trainWallLayers;
                        stats.curLocationState = LocationState.Carriage;
                        stats.curLocationBounds = CurCarriage.totalBounds;
                        rigidBody.includeLayers = layerSettings.trainMask;
                        
                        transform.SetParent(CurCarriage.transform, true);

                        atlasRenderer.UpdateWorldDepth(trainStats.depthSections.frontMin);
                    }
                    break;
                }

            }
            break;
        }
    }
    private void LookAtCarriageMap()
    {
        if (CurCarriage == null) return;

        if (!checkingCarriageMap)
        {
            RaycastHit2D carriageMapHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, layerSettings.trainLayers.carriageMap);
            if (carriageMapHit.collider != null)
            {
                curCarriageMap= CurCarriage.map;
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

    }
    private void Flip(bool flip)
    {
        stats.spriteFlip = flip;
        atlasRenderer.FlipHSimple(flip);
    }
    public static void ChooseNPCTicketToCheck(NPCBrain chosenNPC)
    {
        ChosenNPC = chosenNPC;
    }
    public static void FinishWithChosenNPC()
    {
        ChosenNPC.talkingToSpy = false;
        ChosenNPC = null;
    }
    public static void ToggleTicketCheckAbility(bool toggle)
    {
        CanCheckTicket = toggle;
    }
    public static void ToggleNotepad(bool toggle)
    {
        CheckingNotepad = toggle;
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
