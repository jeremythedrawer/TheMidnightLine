using System;
using UnityEngine;

using static Atlas;
using static AtlasUI;
using static Spy;
using static NPC;
using Cysharp.Threading.Tasks;
public class SpyBrain : MonoBehaviour
{
    const float PLAY_AGAIN_HOLD_TIME = 3f;
    public static Carriage CurCarriage;
    public NPCBrain chosenNPC;

    public static event Action OnAfterOutcomeSequence;
    public static event Action OnTicketCheckHoverEnabled;
    public static event Action<Vector2> OnTicketCheckHoverEnabledFirstTime;
    public static event Action OnTicketCheckHoverDisabled;
    public static event Action<Vector2> OnAtSlideDoors;
    public static event Action OnWalkPastSlideDoors;
    public static event Action OnEnteredTrain;
    public static event Action OnTicketInspect;
    public static event Action OnFinishTicketInspect;
    public static event Action OnOpenNotepad;
    public static event Action OnCloseNotepad;
    public static event Action OnEnteredElevatorGoingUp;
    public static event Action OnCheckCarriageMap;
    public static event Action OnUncheckCarriageMap;

    public static bool CanCheckTicket;
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
    public OptionsSO options;
    public NotepadData notepadData;
    public TripSO trip;
    public SceneData sceneData;
    public MeridiaTowerData meridiaTowerData;

    [Header("Generated")]
    public NPCBrain[] possibleNPCsToTicketCheck;

    public AtlasSO atlas;
    
    public SlideDoors slideDoors;
    
    public HingedDoor smokingRoomDoor;
    
    public GangwayDoor curGangwayDoor;
    
    public CarriageMapProp curCarriageMap;


    public AtlasClip curClip;

    public CollisionData collisionData;

    public Vector3 curWorldPos;

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
    public bool canOpenSlideDoor;
    public bool finishedGettingShot;
    private void OnValidate()
    {
        CalculateCollisionPoints();
    }
    private void OnEnable()
    {     
        gameEventData.OnInteract.RegisterListener(OpenSlideDoors);
        gameEventData.OnInteract.RegisterListener(LookAtCarriageMap);
        gameEventData.OnStationArrival.RegisterListener(EnableCanOpenSlideDoor);
        gameEventData.OnStationLeave.RegisterListener(DisableCanOpenSlideDoor);

        Scenes.OnLoadStart += StartInit;
        Scenes.OnLoadTrip0 += TripInit;

        GameplayUI.OnIncreaseTraitorCountFirstTime += SetStateToIdle;
        
        AgreementProp.OnAgreementCollect += SetStateToIdle;
        AgreementProp.OnNotepadReturn += SetStateToIdle;

        HenchmanBrain.OnShoot += SetStateToShotAt;

        MeridiaTower.OnArriveAtBottomFloor += SetSpyToBottomFloor;

        PresidentBrain.OnShakeHands += SetStateToHandShake;

        EveryInit();
    }
    private void OnDisable()
    {
        gameEventData.OnInteract.UnregisterListener(OpenSlideDoors);
        gameEventData.OnInteract.UnregisterListener(LookAtCarriageMap);
        gameEventData.OnStationArrival.UnregisterListener(EnableCanOpenSlideDoor);
        gameEventData.OnStationLeave.UnregisterListener(DisableCanOpenSlideDoor);

        Scenes.OnLoadStart -= StartInit;
        Scenes.OnLoadTrip0 -= TripInit;

        GameplayUI.OnIncreaseTraitorCountFirstTime -= SetStateToIdle;

        AgreementProp.OnAgreementCollect -= SetStateToIdle;
        AgreementProp.OnNotepadReturn -= SetStateToIdle;

        HenchmanBrain.OnShoot -= SetStateToShotAt;

        PresidentBrain.OnShakeHands -= SetStateToHandShake;

        MeridiaTower.OnArriveAtBottomFloor -= SetSpyToBottomFloor;
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
    private void PlayAgainInit()
    {
        stats.playerInputsEnabled = true;
        sceneData.activeSceneType = Scenes.SceneType.Start;

        EveryInit();
        StartInit();
    }
    private void EveryInit()
    {
        curWorldPos = transform.position;
        atlas = atlasRenderer.atlas;
        atlas.UpdateClipDictionary();

        stats.curGroundLayer = layerSettings.stationLayers.ground;
        stats.curWallLayer = layerSettings.stationWallLayers;
        stats.bounds = atlasRenderer.bounds;
        stats.checkingNotepad = false;

        stats.curTutorialState = TutorialState.None;
        stats.playerInputsEnabled = true;

        rigidBody.includeLayers = layerSettings.stationMask;

        SetState(SpyState.None);

        SceneController.SetSpyBrain(this);
    }
    public void SetNewPosition(Vector3 newPosition)
    {
        curWorldPos = newPosition;
        transform.position = newPosition;
    }
    private void TripInit()
    {
        trip.ticketsCheckedTotal = 0;
        possibleNPCsToTicketCheck = new NPCBrain[8];
    }
    private void StartInit()
    {
        trip.failed = false;
        stats.tutorialsCompleted = TutorialState.None;
        stats.startTrip = false;
        stats.startPos = new Vector2(transform.position.x, transform.position.y);

        trip.curUnlocks = UnlockType.None;
    }
    private void ChooseState()
    {
        if (!stats.playerInputsEnabled) return;

        if ((playerInputs.ticketCheckKeyDown && CanCheckTicket && curNPCTicketCheckHoverCount == 1) || chosenNPC != null)
        {
            if (chosenNPC == null)
            {
                chosenNPC = possibleNPCsToTicketCheck[0];
            }

            if (chosenNPC.role == Role.Accomplice)
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
        else if ((notepadData.collected && playerInputs.notepadToggleKeyDown) || stats.checkingNotepad)
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

                if (canOpenSlideDoor && !stats.checkingNotepad)
                {
                    switch (camStats.curLocationState)
                    {
                        case LocationState.Station:
                        {
                            GetSlideDoorAtStation();
                        }
                        break;

                        case LocationState.Carriage:
                        {
                            if (notepadData.profileWriteCount == trip.traitorProfiles.Length && trainStats.curStationIndex > 0)
                            {
                                GetSlideDoorInTrain();
                            }
                        }
                        break;
                    }
                }
                if (playerInputs.interact)
                {
                    gameEventData.OnInteract?.Raise();
                }
            }
            break;
            case SpyState.Walk:
            {
                if (stats.playerInputsEnabled)
                {
                    Flip(playerInputs.move < 0);
                    stats.targetXVelocity = settings.moveSpeed * playerInputs.move;
                    
                    if (playerInputs.interact) gameEventData.OnInteract?.Raise();
                }
                else
                {
                    if (curWorldPos.x <= stats.startPos.x)
                    {
                        OnEnteredElevatorGoingUp.Invoke();
                        PlayAgainInit();
                    }
                }
                atlasRenderer.PlayClip(ref curClip);
                stats.moveVelocity.x = Mathf.Lerp(stats.moveVelocity.x, stats.targetXVelocity, settings.groundAccelation * Time.deltaTime);

                curWorldPos.x += stats.moveVelocity.x * Time.deltaTime;
                curWorldPos.y = transform.position.y;
                curWorldPos.z = transform.position.z;

                transform.position = curWorldPos;

                stats.bounds = atlasRenderer.bounds;

                if (canOpenSlideDoor && !stats.checkingNotepad)
                {
                    switch (camStats.curLocationState)
                    { 
                        case LocationState.Station:
                        {
                            GetSlideDoorAtStation();
                        }
                        break;
                    
                        case LocationState.Carriage:
                        {
                            if (notepadData.profileWriteCount == trip.traitorProfiles.Length && trainStats.curStationIndex > 0)
                            {
                                GetSlideDoorInTrain();
                            }
                        }
                        break;
                    }
                }
            }
            break;
            case SpyState.TicketCheck:
            {
                atlasRenderer.PlayClip(ref curClip);

                if((playerInputs.ticketCheckKeyUp || playerInputs.mouseLeftUp || playerInputs.moveDown || playerInputs.spacebarDown) && canExitState)
                {
                    chosenNPC.ToggleUnveil(true);
                    FinishWithChosenNPC();
                }
                if (!playerInputs.ticketCheckKeyHold && !playerInputs.mouseLeftHold && playerInputs.move == 0) canExitState = true;
            }
            break;
            case SpyState.CarriageMap:
            {
                atlasRenderer.PlayClip(ref curClip);
                if (!playerInputs.interact) canExitState = true;

                if (playerInputs.interact && canExitState) checkingCarriageMap = false;
            }
            break;
            case SpyState.TalkingToAccomplice:
            {
                atlasRenderer.PlayClip(ref curClip);
                if ((playerInputs.ticketCheckKeyUp || playerInputs.mouseLeftUp || playerInputs.moveDown || playerInputs.spacebarDown) && canExitState && stats.curTutorialState == TutorialState.None)
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
                if (playerInputs.notepadExitKeyUp) canExitState = true;

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
                    if (playerInputs.notepadToggleKeyDown && canExitState)
                    {
                        stats.checkingNotepad = false;
                    }
                }
            }
            break;
            case SpyState.ShotAt:
            {
                if (!atlasRenderer.isAnimating && !finishedGettingShot)
                {
                    finishedGettingShot = true;
                    WaitToPlayAgain();
                }
            }
            break;
            case SpyState.HandShake:
            {
                atlasRenderer.PlayClip(ref curClip);
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
                if (camStats.curLocationState != LocationState.Station)
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
                            camStats.curLocationBounds = curGangwayDoor.gangway.exteriorRenderer.bounds;
                            camStats.curLocationState = LocationState.Gangway;
                        }
                        else
                        {
                            curGangwayDoor.gangway.MoveUp();
                            camStats.curLocationBounds = curGangwayDoor.carriage.totalBounds;
                            camStats.curLocationState = LocationState.Carriage;
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
                            camStats.curLocationBounds = curGangwayDoor.carriage.totalBounds;
                            camStats.curLocationState = LocationState.Carriage;
                        }
                        else
                        {
                            curGangwayDoor.carriage.MoveUp();
                            curGangwayDoor.gangway.MoveDown();
                            camStats.curLocationBounds = curGangwayDoor.gangway.exteriorRenderer.bounds;
                            camStats.curLocationState = LocationState.Gangway;
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
                chosenNPC.talkingToSpy = true;

                curClip = atlas.clipDict[(int)SpyMotion.Ticket];

                stats.boardingStationName = trip.stationsDataArray[chosenNPC.profile.boardingStationIndex].name;
                stats.disembarkingStationName = trip.stationsDataArray[chosenNPC.profile.disembarkingStationIndex].name;
                trip.ticketsCheckedTotal++;

                OnTicketInspect?.Invoke();
            }
            break;

            case SpyState.CarriageMap:
            {
                curClip = atlas.clipDict[(int)SpyMotion.StandingBreathing];
                OnCheckCarriageMap?.Invoke();
            }
            break;
            case SpyState.TalkingToAccomplice:
            {
                chosenNPC.talkingToSpy = true;

                curClip = atlas.clipDict[(int)SpyMotion.StandingBreathing];

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
                SceneController.GetUnlockPicker().Open(unlockSelectionAmount: 1, curUnlockType, chosenNPC);

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
                OnOpenNotepad?.Invoke();
            }
            break;
            case SpyState.ShotAt:
            {
                curClip = atlas.clipDict[(int)SpyMotion.ShotByGun];
                atlasRenderer.PlayClipOneShot(curClip);
                stats.playerInputsEnabled = false;
            }
            break;
            case SpyState.HandShake:
            {
                curClip = atlas.clipDict[(int)SpyMotion.Handshake];
                stats.playerInputsEnabled = false;
                WaitToPlayAgain();
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
                OnFinishTicketInspect?.Invoke();
                if (trip.ticketsCheckedSinceLastStation == trip.stationAhead.ticketsToCheckBeforeSpawn)
                {
                    CanCheckTicket = false;
                }

            }
            break;
            case SpyState.CarriageMap:
            {
                OnUncheckCarriageMap?.Invoke();
                curCarriageMap.StopUsing();
                checkingCarriageMap = false;
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
                    if (npc != chosenNPC)
                    {
                        npc.talkingToSpy = false;
                        npc.ToggleTicketCheckHover(false);
                    }
                }
            }
            break;

            case SpyState.Notepad:
            {
                OnCloseNotepad?.Invoke();
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
                    }
                    else
                    {
                        npc.ToggleTicketCheckHover(toggle: false);
                    }
                }

                if (curNPCTicketCheckHoverCount == 0)
                {
                    OnTicketCheckHoverDisabled?.Invoke();
                }
                else
                {
                    OnTicketCheckHoverEnabled?.Invoke();

                    if (trip.ticketsCheckedTotal == 0)
                    {
                        AtlasRenderer npcRend = possibleNPCsToTicketCheck[0].atlasRenderer;
                        OnTicketCheckHoverEnabledFirstTime?.Invoke(new Vector2(npcRend.transform.position.x, npcRend.bounds.max.y));
                    }

                }
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
    private void GetSlideDoorAtStation()
    {
        Bounds spyBounds = stats.bounds;
        SlideDoors foundSlideDoor = null;

        if (trip.stationsDataArray[trainStats.curStationIndex].isFrontOfTrain)
        {
            for (int i = 0; i < TrainController.ExteriorSlideDoors.Length; i++)
            {
                SlideDoors slideDoor = TrainController.ExteriorSlideDoors[i];
                Bounds slideDoorBounds = slideDoor.boxCollider.bounds;
                if (spyBounds.center.x > slideDoorBounds.min.x && spyBounds.center.x < slideDoorBounds.max.x)
                {
                    if (slideDoor.curState == SlideDoors.State.Unlocked || slideDoor.curState == SlideDoors.State.Opened)
                    {
                        foundSlideDoor = TrainController.ExteriorSlideDoors[i];
                        break;
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < TrainController.InteriorSlideDoors.Length; i++)
            {
                SlideDoors slideDoor = TrainController.ExteriorSlideDoors[i];
                Bounds slideDoorBounds = slideDoor.boxCollider.bounds;
                if (spyBounds.center.x > slideDoorBounds.min.x && spyBounds.center.x < slideDoorBounds.max.x)
                {
                    foundSlideDoor = TrainController.InteriorSlideDoors[i];
                    break;
                }
            }
        }

        if (foundSlideDoor != null && slideDoors == null)
        {
            OnAtSlideDoors?.Invoke(new Vector2(foundSlideDoor.boxCollider.bounds.center.x, foundSlideDoor.boxCollider.bounds.max.y));
        }
        else if (foundSlideDoor == null && slideDoors != null)
        {
            OnWalkPastSlideDoors?.Invoke();
        }
        slideDoors = foundSlideDoor;
    }
    private void GetSlideDoorInTrain()
    {
        Bounds spyBounds = stats.bounds;
        SlideDoors foundSlideDoor = null;
        for (int i = 0; i < CurCarriage.interiorSlideDoors.Length; i++)
        {
            SlideDoors slideDoor = CurCarriage.interiorSlideDoors[i];
            Bounds slideDoorBounds = slideDoor.boxCollider.bounds;
            if (spyBounds.center.x > slideDoorBounds.min.x && spyBounds.center.x < slideDoorBounds.max.x)
            {
                foundSlideDoor = slideDoor;
                break;
            }
        }

        if (foundSlideDoor != null && slideDoors == null)
        {
            OnAtSlideDoors?.Invoke(new Vector2(foundSlideDoor.boxCollider.bounds.center.x, foundSlideDoor.boxCollider.bounds.max.y));

        }
        else if (foundSlideDoor == null && slideDoors != null)
        {
            OnWalkPastSlideDoors?.Invoke();
        }
        slideDoors = foundSlideDoor;
    }
    private void EnableCanOpenSlideDoor()
    {
        slideDoors = null;
        canOpenSlideDoor = true;
    }
    private void DisableCanOpenSlideDoor()
    {
        canOpenSlideDoor = false;
    }
    private void OpenSlideDoors()
    {
        if (slideDoors == null || !canOpenSlideDoor || stats.checkingNotepad) return;

        switch(slideDoors.curState)
        {
            case SlideDoors.State.Unlocked:
            {
                slideDoors.OpenDoors();
            }
            break;

            case SlideDoors.State.Opened:
            {
                switch (camStats.curLocationState)
                {
                    case LocationState.Carriage:
                    {
                        if (trainStats.curStationIndex > 0)
                        {
                            stats.curGroundLayer = layerSettings.stationLayers.ground;
                            stats.curWallLayer = layerSettings.stationWallLayers;
                            camStats.curLocationState = LocationState.Station;

                            rigidBody.includeLayers = layerSettings.stationMask;

                            Station station = TrainController.NextStationInstance;
                        
                            AtlasRenderer stationPlatform = station.station.isFrontOfTrain ? station.frontPlatformRenderer : station.backPlatformRenderer;
                        
                            transform.SetParent(stationPlatform.transform, true);
                        
                            atlasRenderer.SetWorldDepth((int)stationPlatform.transform.position.z);
                        }
                    }
                    break;

                    case LocationState.Station:
                    {                        
                        CurCarriage = slideDoors.carriage;
                        CurCarriage.MoveDown();

                        stats.curGroundLayer = layerSettings.trainLayers.ground;
                        stats.curWallLayer = layerSettings.trainWallLayers;
                        camStats.curLocationState = LocationState.Carriage;
                        camStats.curLocationBounds = CurCarriage.totalBounds;
                        rigidBody.includeLayers = layerSettings.trainMask;
                        
                        transform.SetParent(CurCarriage.transform, true);

                        atlasRenderer.SetWorldDepth(trainStats.depthSections.frontStandingBack);
                        OnEnteredTrain?.Invoke();
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
            for (int i = 0; i < CurCarriage.maps.Length; i++)
            {
                CarriageMapProp map = CurCarriage.maps[i];
                Bounds mapBounds = map.atlasRenderer.bounds;
                if (stats.bounds.center.x > mapBounds.min.x && stats.bounds.center.x < mapBounds.max.x)
                {
                    curCarriageMap = map;
                    curCarriageMap.Use();
                    checkingCarriageMap = true;
                }
            }
        }
    }
    private void Flip(bool flip)
    {
        stats.spriteFlip = flip;
        atlasRenderer.FlipHSimple(flip);
    }
    public void SetStateToNotepad()
    {
        SetState(SpyState.Notepad);
    }
    public void SetStateToIdle()
    {
        SetState(SpyState.Idle);
    }
    public void SetStateToShotAt()
    {
        SetState(SpyState.ShotAt);
    }
    public void SetStateToHandShake()
    {
        SetState(SpyState.HandShake);
    }
    public void FinishWithChosenNPC()
    {
        chosenNPC.talkingToSpy = false;
        chosenNPC = null;
    }
    public void ChooseNPCTicketToCheck(NPCBrain npc)
    {
        chosenNPC = npc;
    }
    private void WaitToPlayAgain()
    {
        WaitingToPlayAgain().Forget();
    }
    private void SetSpyToBottomFloor()
    {
        SetState(SpyState.Walk);
        Flip(true);
        stats.targetXVelocity = -settings.moveSpeed;
        transform.position = new Vector3(meridiaTowerData.bottomFloorCenterTopPos.x, meridiaTowerData.bottomFloorCenterTopPos.y, transform.position.z);
        curWorldPos = transform.position;
    }
    private async UniTask WaitingToPlayAgain()
    {
        float clock = 0;
        while(clock < PLAY_AGAIN_HOLD_TIME)
        {
            clock += Time.deltaTime;
            await UniTask.Yield();
        }
        camStats.curLocationState = LocationState.Elevator;
        OnAfterOutcomeSequence?.Invoke();
    }

    public static void ToggleTicketCheckAbility(bool toggle)
    {
        CanCheckTicket = toggle;
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
