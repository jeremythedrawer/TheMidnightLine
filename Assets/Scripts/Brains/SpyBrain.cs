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
    public static event Action OnInteract;
    public static event Action OnMoveFirstTime;
    public static event Action OnExitTrain;

    public static bool PickingNPCToTicketCheck;

    [Header("Components")]
    public Rigidbody2D rigidBody;
    public BoxCollider2D boxCollider;
    public AtlasRenderer atlasRenderer;
    
    [Header("Scriptable Objects")]
    public SpyData spyData;
    public InputData inputData;
    public TrainData trainData;
    public TrainSettingsSO trainSettings;
    public LayerData layerSettings;
    public CameraData camStats;
    public Options options;
    public NotepadData notepadData;
    public TripData trip;
    public SceneData sceneData;
    public MeridiaTowerData meridiaTowerData;

    [Header("Generated")]
    public NPCBrain[] possibleNPCsToTicketCheck;

    public NPCBrain chosenNPC;

    public AtlasSO atlas;
    
    public SlideDoors slideDoors;
    
    public HingedDoor smokingRoomDoor;
    
    public GangwayDoor curGangwayDoor;
    
    public CarriageMapProp curCarriageMapProp;


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
        SpyBrain.OnInteract += OpenSlideDoors;
        SpyBrain.OnInteract += LookAtCarriageMap;

        TrainController.OnStationArrival += SetInputsForTrainStop;
        TrainController.OnStationLeave += SetInputsForTrainStart;

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
        SpyBrain.OnInteract -= OpenSlideDoors;
        SpyBrain.OnInteract -= LookAtCarriageMap;

        TrainController.OnStationArrival -= SetInputsForTrainStop;
        TrainController.OnStationLeave -= SetInputsForTrainStart;

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
        bool leftWallTouch = Physics2D.Linecast(boxCollider.bounds.center, collisionData.wallLeft, spyData.curWallLayer);
        bool rightWallTouch = Physics2D.Linecast(boxCollider.bounds.center, collisionData.wallRight, spyData.curWallLayer);

        spyData.walkingIntoWall = (leftWallTouch && inputData.move == -1) || (rightWallTouch && inputData.move == 1);
    }
    private void PlayAgainInit()
    {
        spyData.playerInputsEnabled = true;
        sceneData.activeSceneType = Scenes.SceneType.Start;

        EveryInit();
        StartInit();
    }
    private void EveryInit()
    {
        curWorldPos = transform.position;
        atlas = atlasRenderer.atlas;
        atlas.UpdateClipDictionary();

        spyData.curGroundLayer = layerSettings.stationLayers.ground;
        spyData.curWallLayer = layerSettings.stationWallLayers;
        spyData.bounds = atlasRenderer.bounds;
        spyData.checkingNotepad = false;

        spyData.curTutorialState = TutorialState.None;
        spyData.playerInputsEnabled = true;

        rigidBody.includeLayers = layerSettings.stationMask;

        spyData.curState = SpyState.Idle;
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
        trip.ticketsCheckedSinceLastStation = 0;
        trip.ticketsCheckedTotal = 0;
        possibleNPCsToTicketCheck = new NPCBrain[8];
    }
    private void StartInit()
    {
        trip.failed = false;
        spyData.tutorialsCompleted = TutorialState.None;
        spyData.startTrip = false;
        spyData.canCheckTicket = false;
        spyData.startPos = new Vector2(transform.position.x, transform.position.y);
        spyData.movedFirstTime = false;

        trip.curUnlocks = UnlockType.None;
    }
    private void ChooseState()
    {
        if (!spyData.playerInputsEnabled) return;

        if ((inputData.ticketCheckKeyDown && spyData.canCheckTicket && curNPCTicketCheckHoverCount == 1 && !spyData.checkingNotepad) || chosenNPC != null)
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
        else if ((inputData.ticketCheckKeyDown && spyData.canCheckTicket && curNPCTicketCheckHoverCount > 1) || PickingNPCToTicketCheck)
        {
            SetState(SpyState.PickingNPCTicketCheck);
        }
        else if ((notepadData.collected && inputData.notepadToggleKeyDown) || spyData.checkingNotepad)
        {
            SetState(SpyState.Notepad);
        }
        else if (checkingCarriageMap)
        {
            SetState(SpyState.CarriageMap);
        }
        else if (inputData.move != 0 && !spyData.walkingIntoWall)
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
        switch (spyData.curState)
        {
            case SpyState.Idle:
            {
                atlasRenderer.PlayClip(ref curClip);

                if (canOpenSlideDoor && !spyData.checkingNotepad)
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
                            if (notepadData.profileWriteCount == trip.traitorProfiles.Length && trainData.curStationIndex > 0)
                            {
                                GetSlideDoorInTrain();
                            }
                        }
                        break;
                    }
                }
                if (inputData.interactKeyDown)
                {
                    OnInteract?.Invoke();
                }

            }
            break;
            case SpyState.Walk:
            {
                if (spyData.playerInputsEnabled)
                {
                    Flip(inputData.move < 0);
                    spyData.targetXVelocity = spyData.moveSpeed * inputData.move;
                    
                    if (inputData.interactKeyDown) OnInteract?.Invoke();
                }
                else
                {
                    if (curWorldPos.x <= spyData.startPos.x)
                    {
                        OnEnteredElevatorGoingUp.Invoke();
                        PlayAgainInit();
                    }
                }
                atlasRenderer.PlayClip(ref curClip);
                spyData.moveVelocity.x = Mathf.Lerp(spyData.moveVelocity.x, spyData.targetXVelocity, spyData.groundAccelation * Time.deltaTime);

                curWorldPos.x += spyData.moveVelocity.x * Time.deltaTime;
                curWorldPos.y = transform.position.y;
                curWorldPos.z = transform.position.z;

                transform.position = curWorldPos;

                spyData.bounds = atlasRenderer.bounds;

                switch (camStats.curLocationState)
                { 
                    case LocationState.Station:
                    {
                        if (canOpenSlideDoor && !spyData.checkingNotepad)
                        {
                            GetSlideDoorAtStation();
                        }
                    }
                    break;
                    
                    case LocationState.Carriage:
                    {
                        if (canOpenSlideDoor && !spyData.checkingNotepad && (notepadData.profileWriteCount == trip.traitorProfiles.Length || trainData.curStationIndex == trip.stationsDataArray.Length - 1) && trainData.curStationIndex > 0)
                        {
                            GetSlideDoorInTrain();
                        }

                        if (curCarriageMapProp == null)
                        {
                            EnterCarriageMap();
                        }
                        else
                        {
                            ExitCarriageMap();
                        }
                    }
                    break;
                }
            }
            break;
            case SpyState.TicketCheck:
            {
                atlasRenderer.PlayClip(ref curClip);

                if((inputData.ticketCheckKeyUp || inputData.mouseLeftUp || inputData.moveKeyDown || inputData.writeKeyDown) && canExitState)
                {
                    FinishWithChosenNPC();
                }
                if (!inputData.ticketCheckKeyHold && !inputData.mouseLeftHold && inputData.move == 0) canExitState = true;
            }
            break;
            case SpyState.CarriageMap:
            {
                atlasRenderer.PlayClip(ref curClip);
                if (!inputData.interactKeyDown) canExitState = true;

                if (inputData.interactKeyDown && canExitState) checkingCarriageMap = false;
            }
            break;
            case SpyState.TalkingToAccomplice:
            {
                atlasRenderer.PlayClip(ref curClip);
                if ((inputData.ticketCheckKeyUp || inputData.mouseLeftUp || inputData.moveKeyDown || inputData.writeKeyDown) && canExitState)
                {
                    if (spyData.curTutorialState == TutorialState.None)
                    {
                        chosenNPC.talkingToSpy = false;
                        chosenNPC = null;
                        SceneController.GetUnlockPicker().Close();
                    }
                }

                if (!inputData.ticketCheckKeyHold && !inputData.mouseLeftHold && inputData.move == 0) canExitState = true;
            }
            break;
            case SpyState.PickingNPCTicketCheck:
            {
                if ((inputData.mouseLeftUp || inputData.move != 0) && canExitState)
                {
                    PickingNPCToTicketCheck = false;
                }

                if (!inputData.ticketCheckKeyHold && !inputData.mouseLeftHold && inputData.move == 0) canExitState = true;
            }
            break;
            case SpyState.Notepad:
            {
                if (inputData.notepadToggleKeyUp) canExitState = true;

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
                    }
                    curNotepadState = notepadData.curState;
                }

                if (curNotepadState == NotepadState.Stationary)
                {
                    atlasRenderer.PlayClip(ref curClip);
                    if (inputData.notepadToggleKeyDown && canExitState)
                    {
                        spyData.checkingNotepad = false;
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
        switch (spyData.curState)
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
        if (spyData.curState == newState) return;
        ExitState();
        spyData.curState = newState;
        EnterState();
    }
    private void EnterState()
    {
        canExitState = false;

        switch (spyData.curState)
        {
            case SpyState.Idle:
            {
                curClip = atlas.clipDict[(int)SpyMotion.StandingBreathing];
            }
            break;
            case SpyState.Walk:
            {
                curClip = atlas.clipDict[(int)SpyMotion.Walking];
                if (!spyData.movedFirstTime)
                {
                    OnMoveFirstTime?.Invoke();
                    spyData.movedFirstTime = true;
                }
            }
            break;
            case SpyState.TicketCheck:
            {
                chosenNPC.talkingToSpy = true;

                curClip = atlas.clipDict[(int)SpyMotion.Ticket];

                spyData.boardingStationName = trip.stationsDataArray[chosenNPC.profile.boardingStationIndex].name;
                spyData.disembarkingStationName = trip.stationsDataArray[chosenNPC.profile.disembarkingStationIndex].name;
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
                QuickSortNPCByXPos(possibleNPCsToTicketCheck, 0, curNPCTicketCheckHoverCount - 1);

                for (int i = 0; i < curNPCTicketCheckHoverCount; i++)
                {
                    possibleNPCsToTicketCheck[i].talkingToSpy = true;
                }

                NPCPicker npcPicker = SceneController.GetNPCPicker();
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
                spyData.playerInputsEnabled = false;
            }
            break;
            case SpyState.HandShake:
            {
                curClip = atlas.clipDict[(int)SpyMotion.Handshake];
                spyData.playerInputsEnabled = false;
                WaitToPlayAgain();
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (spyData.curState)
        {
            case SpyState.Idle:
            {
                
            }
            break;
            case SpyState.Walk:
            {
                spyData.moveVelocity.x = 0;
            }
            break;
            case SpyState.TicketCheck:
            {
                trip.ticketsCheckedSinceLastStation++;
                OnFinishTicketInspect?.Invoke();
                if (trip.ticketsCheckedSinceLastStation == trip.stationAhead.ticketsToCheckBeforeSpawn)
                {
                    spyData.canCheckTicket = false;
                }

            }
            break;
            case SpyState.CarriageMap:
            {
                OnUncheckCarriageMap?.Invoke();
                ExitCarriageMap();
                checkingCarriageMap = false;

                curCarriageMapProp.Invert();
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
        if (spyData.canCheckTicket)
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
        if (spyData == null) return;

        float groundLeft = transform.position.x - spyData.groundBufferHorizontal;
        float groundRight = transform.position.x + spyData.groundBufferHorizontal;
        float groundBottom = transform.position.y - spyData.groundBufferVertical;
        float wallLeft = boxCollider.bounds.center.x - spyData.wallWidthBuffer;
        float wallRight = boxCollider.bounds.center.x + spyData.wallWidthBuffer;
        collisionData.groundLeft = new Vector2(groundLeft, groundBottom);
        collisionData.groundRight = new Vector2(groundRight, groundBottom);

        collisionData.wallLeft = new Vector2(wallLeft, boxCollider.bounds.center.y);
        collisionData.wallRight = new Vector2(wallRight, boxCollider.bounds.center.y);

    }
    private void GetSlideDoorAtStation()
    {
        Bounds spyBounds = spyData.bounds;
        SlideDoors foundSlideDoor = null;

        if (trip.stationsDataArray[trainData.curStationIndex].isFrontOfTrain)
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
        Bounds spyBounds = spyData.bounds;

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
    private void SetInputsForTrainStop()
    {
        slideDoors = null;
        canOpenSlideDoor = true;
        spyData.canCheckTicket = false;
    }
    private void SetInputsForTrainStart()
    {
        canOpenSlideDoor = false;
        spyData.canCheckTicket = true;
    }
    private void OpenSlideDoors()
    {
        if (slideDoors == null || !canOpenSlideDoor || spyData.checkingNotepad) return;

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
                        if (trainData.curStationIndex > 0)
                        {
                            spyData.curGroundLayer = layerSettings.stationLayers.ground;
                            spyData.curWallLayer = layerSettings.stationWallLayers;
                            camStats.curLocationState = LocationState.Station;

                            rigidBody.includeLayers = layerSettings.stationMask;

                            Station station = TrainController.NextStationInstance;
                        
                            AtlasRenderer stationPlatform = station.platformRenderer;
                        
                            transform.SetParent(stationPlatform.transform, true);
                        
                            atlasRenderer.SetWorldDepth((int)stationPlatform.transform.position.z);
                            OnExitTrain?.Invoke();
                        }
                    }
                    break;

                    case LocationState.Station:
                    {                        
                        CurCarriage = slideDoors.carriage;
                        CurCarriage.MoveDown();

                        spyData.curGroundLayer = layerSettings.trainLayers.ground;
                        spyData.curWallLayer = layerSettings.trainWallLayers;
                        camStats.curLocationState = LocationState.Carriage;
                        camStats.curLocationBounds = CurCarriage.totalBounds;
                        rigidBody.includeLayers = layerSettings.trainMask;
                        
                        transform.SetParent(CurCarriage.transform, true);

                        atlasRenderer.SetWorldDepth(trainData.depthSections.frontStandingBack);
                        OnEnteredTrain?.Invoke();
                    }
                    break;
                }

            }
            break;
        }
    }
    private void EnterCarriageMap()
    {
        for (int i = 0; i < CurCarriage.maps.Length; i++)
        {
            CarriageMapProp map = CurCarriage.maps[i];
            Bounds mapBounds = map.atlasRenderer.bounds;
            if (spyData.bounds.center.x > mapBounds.min.x && spyData.bounds.center.x < mapBounds.max.x)
            {
                curCarriageMapProp = map;
                curCarriageMapProp.Enter();
            }
        }
    }
    private void LookAtCarriageMap()
    {
        if (checkingCarriageMap || curCarriageMapProp == null || camStats.curLocationState != LocationState.Carriage || spyData.curTutorialState != TutorialState.None) return;

        curCarriageMapProp.Revert();
        checkingCarriageMap = true;
    }
    private void ExitCarriageMap()
    {
        if (spyData.bounds.center.x < curCarriageMapProp.atlasRenderer.bounds.min.x || spyData.bounds.center.x > curCarriageMapProp.atlasRenderer.bounds.max.x)
        {
            curCarriageMapProp.Exit();
            curCarriageMapProp = null;
        }
    }
    private void Flip(bool flip)
    {
        spyData.spriteFlip = flip;
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
        chosenNPC.ToggleUnveil(true);

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
        spyData.targetXVelocity = -spyData.moveSpeed;
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

    private void OnDrawGizmos()
    {
        CalculateCollisionPoints();
        Gizmos.color = Color.orange;
        Gizmos.DrawLine(collisionData.groundLeft, collisionData.groundRight);

        Gizmos.color = Color.indianRed;

        Gizmos.color = spyData.walkingIntoWall ? Color.forestGreen : Color.red;
        Gizmos.DrawLine(collisionData.wallLeft, boxCollider.bounds.center);
        Gizmos.DrawLine(collisionData.wallRight, boxCollider.bounds.center);

    }
}
