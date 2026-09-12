using System;
using UnityEngine;

using static Atlas;
using static AtlasUI;
using static Spy;
using static Passenger;
using Cysharp.Threading.Tasks;
public class SpyBrain : MonoBehaviour
{
    const float PLAY_AGAIN_HOLD_TIME = 3f;
    public static Carriage CurCarriage;

    public static event Action OnHoverTalkEnabled;
    public static event Action<Vector2> OnHoverTalkFirstTime;
    public static event Action OnHoverTalkDisabled;
    public static event Action<Vector2> OnAtSlideDoors;
    public static event Action OnWalkPastSlideDoors;
    public static event Action OnEnteredTrain;
    public static event Action OnTalkToPassenger;
    public static event Action OnOpenNotepad;
    public static event Action OnCloseNotepad;
    public static event Action OnCheckCarriageMap;
    public static event Action OnUncheckCarriageMap;
    public static event Action OnInteract;
    public static event Action OnExitTrain;

    public static bool PickingNPCToTicketCheck;

    [Header("Components")]
    public Rigidbody2D rigidBody;
    public BoxCollider2D boxCollider;
    public AtlasRenderer atlasRenderer;
    public AudioSource audioSource;

    [Header("Scriptable Objects")]
    public SpyData spyData;
    public InputData inputData;
    public TrainData trainData;
    public LayerData layerData;
    public CameraData camData;
    public NotepadData notepadData;
    public AudioData audioData;
    public Options options;

    [Header("Generated")]
    public PassengerBrain[] possiblePassengers;
    public PassengerBrain passengerToTalkTo;

    public AtlasSO atlas;
    
    public SlideDoors slideDoors;
    
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
    public int curPassengerHoverTalkCount;

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

        atlasRenderer.onChangeKeyframe += OnChangeFootStepSound;

        Init();
    }
    private void OnDisable()
    {
        SpyBrain.OnInteract -= OpenSlideDoors;
        SpyBrain.OnInteract -= LookAtCarriageMap;

        TrainController.OnStationArrival -= SetInputsForTrainStop;
        TrainController.OnStationLeave -= SetInputsForTrainStart;

        atlasRenderer.onChangeKeyframe -= OnChangeFootStepSound;
    }
    private void Start()
    {
        Init();
    }
    private void Update()
    {
        ChooseState();
        UpdateStates();
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
    }
    private void Init()
    {
        layerData.CombineAllLayerMasks();

        spyData.startTrip = false;
        spyData.curGroundLayer = layerData.stationLayers.ground;
        spyData.curWallLayer = layerData.stationWallLayers;
        spyData.bounds = atlasRenderer.GetBounds();
        spyData.curState = SpyState.Idle;
        spyData.playerInputsEnabled = true;

        possiblePassengers = new PassengerBrain[8];
        
        rigidBody.includeLayers = layerData.stationMask;
        curWorldPos = transform.position;
        atlas = atlasRenderer.atlas;
        atlas.UpdateClipDictionary();

        SetState(SpyState.None);
    }
    public void SetNewPosition(Vector3 newPosition)
    {
        curWorldPos = newPosition;
        transform.position = newPosition;
    }
    private void ChooseState()
    {
        if (!spyData.playerInputsEnabled) return;

        if ((inputData.talkKeyDown && curPassengerHoverTalkCount == 1 && (notepadData.subState & Notepad.SubState.InUse) == 0) || passengerToTalkTo != null)
        {
            if (passengerToTalkTo == null)
            {
                passengerToTalkTo = possiblePassengers[0];
            }

            SetState(SpyState.TalkingToPassenger);
        }
        else if ((inputData.talkKeyDown && curPassengerHoverTalkCount > 1) || PickingNPCToTicketCheck)
        {
            SetState(SpyState.PickingPassenger);
        }
        else if ((notepadData.collected && inputData.notepadToggleKeyDown) || (notepadData.subState & Notepad.SubState.InUse) != 0)
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
                atlasRenderer.PlayClip(curClip);

                if (canOpenSlideDoor && (notepadData.subState & Notepad.SubState.InUse) == 0)
                {
                    switch (camData.curLocationState)
                    {
                        case LocationState.Station:
                        {
                            GetSlideDoorAtStation();
                        }
                        break;

                        case LocationState.Carriage:
                        {
                            if (trainData.curStationIndex > 0)
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

                atlasRenderer.PlayClip(curClip, audioSource: audioSource, audioData: audioData);
                spyData.moveVelocity.x = Mathf.Lerp(spyData.moveVelocity.x, spyData.targetXVelocity, spyData.groundAccelation * Time.deltaTime);

                curWorldPos.x += spyData.moveVelocity.x * Time.deltaTime;
                curWorldPos.y = transform.position.y;
                curWorldPos.z = transform.position.z;

                transform.position = curWorldPos;

                spyData.bounds = atlasRenderer.bounds;

                switch (camData.curLocationState)
                { 
                    case LocationState.Station:
                    {
                        if (canOpenSlideDoor && (notepadData.subState & Notepad.SubState.InUse) == 0)
                        {
                            GetSlideDoorAtStation();
                        }
                    }
                    break;
                    
                    case LocationState.Carriage:
                    {
                        if (canOpenSlideDoor && (notepadData.subState & Notepad.SubState.InUse) == 0 && trainData.curStationIndex > 0)
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

            case SpyState.CarriageMap:
            {
                atlasRenderer.PlayClip(curClip);
                if (!inputData.interactKeyDown) canExitState = true;

                if (inputData.interactKeyDown && canExitState) checkingCarriageMap = false;
            }
            break;
            case SpyState.TalkingToPassenger:
            {
                atlasRenderer.PlayClip(curClip);
                if ((inputData.talkKeyUp || inputData.mouseLeftUp || inputData.moveKeyDown) && canExitState)
                {
                    passengerToTalkTo.talkingToSpy = false;
                    passengerToTalkTo = null;
                }

                if (!inputData.talkKeyHold && !inputData.mouseLeftHold && inputData.move == 0) canExitState = true;
            }
            break;
            case SpyState.PickingPassenger:
            {
                if ((inputData.mouseLeftUp || inputData.move != 0) && canExitState)
                {
                    PickingNPCToTicketCheck = false;
                }

                if (!inputData.talkKeyHold && !inputData.mouseLeftHold && inputData.move == 0) canExitState = true;
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
                    atlasRenderer.PlayClip(curClip);
                }
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
                if (camData.curLocationState == LocationState.Carriage)
                {
                    CheckIfTicketCheckHover();
                }
            }
            break;
            case SpyState.Walk:
            {
                CalculateCollisionPoints();
                if (camData.curLocationState == LocationState.Carriage || camData.curLocationState == LocationState.Gangway)
                {
                    RaycastHit2D gangwayDoorLeftHit = Physics2D.Linecast(boxCollider.bounds.center, collisionData.wallLeft, layerData.trainLayers.gangwayDoor);
                    RaycastHit2D gangwayDoorRightHit = Physics2D.Linecast(boxCollider.bounds.center, collisionData.wallRight, layerData.trainLayers.gangwayDoor);
                    bool isTouchingGangwayDoorLeft = gangwayDoorLeftHit.collider != null;
                    bool isTouchingGangwayDoorRight = gangwayDoorRightHit.collider != null;

                    if ((!isTouchingGangwayDoorRight && wasTouchingGangwayDoorRight) && !isTouchingGangwayDoorLeft)
                    {
                        if (curGangwayDoor.isLeftOfCarriage)
                        {
                            curGangwayDoor.carriage.MoveUp();
                            camData.curLocationBounds = curGangwayDoor.gangway.exteriorRenderer.bounds;
                            camData.curLocationState = LocationState.Gangway;
                        }
                        else
                        {
                            curGangwayDoor.gangway.MoveUp();
                            camData.curLocationBounds = curGangwayDoor.carriage.totalBounds;
                            camData.curLocationState = LocationState.Carriage;
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
                            camData.curLocationBounds = curGangwayDoor.carriage.totalBounds;
                            camData.curLocationState = LocationState.Carriage;
                        }
                        else
                        {
                            curGangwayDoor.carriage.MoveUp();
                            curGangwayDoor.gangway.MoveDown();
                            camData.curLocationBounds = curGangwayDoor.gangway.exteriorRenderer.bounds;
                            camData.curLocationState = LocationState.Gangway;
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
            case SpyState.PickingPassenger:
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
            }
            break;

            case SpyState.CarriageMap:
            {
                curClip = atlas.clipDict[(int)SpyMotion.StandingBreathing];
                OnCheckCarriageMap?.Invoke();
            }
            break;
            case SpyState.TalkingToPassenger:
            {
                passengerToTalkTo.talkingToSpy = true;

                spyData.boardingStationName = options.curTrip.stationsDataArray[passengerToTalkTo.profile.boardingStationIndex].name;
                spyData.disembarkingStationName = options.curTrip.stationsDataArray[passengerToTalkTo.profile.disembarkingStationIndex].name;

                options.curTrip.passengersTalkToTotal++;

                curClip = atlas.clipDict[(int)SpyMotion.StandingBreathing];

                OnTalkToPassenger?.Invoke();
            }
            break;
            case SpyState.PickingPassenger:
            {
                PickingNPCToTicketCheck = true;
                QuickSortNPCByXPos(possiblePassengers, 0, curPassengerHoverTalkCount - 1);

                for (int i = 0; i < curPassengerHoverTalkCount; i++)
                {
                    possiblePassengers[i].talkingToSpy = true;
                }

            }
            break;
            case SpyState.Notepad:
            {
                curClip = atlas.clipDict[(int)SpyMotion.NotepadHolding];
                OnOpenNotepad?.Invoke();
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

            case SpyState.CarriageMap:
            {
                OnUncheckCarriageMap?.Invoke();
                ExitCarriageMap();
                checkingCarriageMap = false;

                curCarriageMapProp.Invert();
            }
            break;
            case SpyState.TalkingToPassenger:
            {
                options.curTrip.passengersTalkToSinceLastStation++;
            }
            break;

            case SpyState.PickingPassenger:
            {
                PickingNPCToTicketCheck = false;

                for (int i = 0; i < curPassengerHoverTalkCount; i++)
                {
                    PassengerBrain npc = possiblePassengers[i];
                    if (npc != passengerToTalkTo)
                    {
                        npc.talkingToSpy = false;
                        npc.ToggleHoverTalk(false);
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
    private void OnChangeFootStepSound(MotionSprite curSprite)
    {
        if (curSprite.audioIndex == 0)
        {
            int randFootstepIndex = UnityEngine.Random.Range(0, audioData.footStepsConcrete.Length);
            curClip.audioClips[0] = audioData.footStepsConcrete[randFootstepIndex];
        }
    }
    private void CheckIfTicketCheckHover()
    {
        Bounds spyBounds = atlasRenderer.bounds;

        curPassengerHoverTalkCount = 0;

        for (int i = 0; i < CurCarriage.curNPCList.Count; i++)
        {
            if (curPassengerHoverTalkCount < possiblePassengers.Length)
            {
                PassengerBrain npc = CurCarriage.curNPCList[i];
                if (npc.ticketHasBeenChecked) continue;

                Bounds npcBounds = npc.atlasRenderer.bounds;

                if (spyBounds.max.x > npcBounds.min.x && spyBounds.min.x < npcBounds.max.x)
                {
                    npc.ToggleHoverTalk(toggle: true);

                    possiblePassengers[curPassengerHoverTalkCount] = npc;
                    curPassengerHoverTalkCount++;
                }
                else
                {
                    npc.ToggleHoverTalk(toggle: false);
                }
            }

            if (curPassengerHoverTalkCount == 0)
            {
                OnHoverTalkDisabled?.Invoke();
            }
            else
            {
                OnHoverTalkEnabled?.Invoke();

                if (options.curTrip.passengersTalkToTotal == 0)
                {
                    AtlasRenderer npcRend = possiblePassengers[0].atlasRenderer;
                    OnHoverTalkFirstTime?.Invoke(new Vector2(npcRend.transform.position.x, npcRend.bounds.max.y));
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

        bool leftWallTouch = Physics2D.Linecast(boxCollider.bounds.center, collisionData.wallLeft, spyData.curWallLayer);
        bool rightWallTouch = Physics2D.Linecast(boxCollider.bounds.center, collisionData.wallRight, spyData.curWallLayer);
        spyData.walkingIntoWall = (leftWallTouch && inputData.move == -1) || (rightWallTouch && inputData.move == 1);

    }
    private void GetSlideDoorAtStation()
    {
        Bounds spyBounds = spyData.bounds;
        SlideDoors foundSlideDoor = null;

        if (options.curTrip.stationsDataArray[trainData.curStationIndex].isFrontOfTrain)
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
    }
    private void SetInputsForTrainStart()
    {
        canOpenSlideDoor = false;
    }
    private void OpenSlideDoors()
    {
        if (slideDoors == null || !canOpenSlideDoor || (notepadData.subState & Notepad.SubState.InUse) != 0) return;

        switch(slideDoors.curState)
        {
            case SlideDoors.State.Unlocked:
            {
                slideDoors.OpenDoors();
            }
            break;

            case SlideDoors.State.Opened:
            {
                switch (camData.curLocationState)
                {
                    case LocationState.Carriage:
                    {
                        if (trainData.curStationIndex > 0)
                        {
                            spyData.curGroundLayer = layerData.stationLayers.ground;
                            spyData.curWallLayer = layerData.stationWallLayers;
                            camData.curLocationState = LocationState.Station;

                            rigidBody.includeLayers = layerData.stationMask;

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

                        spyData.curGroundLayer = layerData.trainLayers.ground;
                        spyData.curWallLayer = layerData.trainWallLayers;
                        camData.curLocationState = LocationState.Carriage;
                        camData.curLocationBounds = CurCarriage.totalBounds;
                        rigidBody.includeLayers = layerData.trainMask;
                        
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
        if (checkingCarriageMap || curCarriageMapProp == null || camData.curLocationState != LocationState.Carriage) return;

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
    public void FinishWithChosenNPC()
    {
        passengerToTalkTo.talkingToSpy = false;
        passengerToTalkTo.ToggleUnveil(true);

        passengerToTalkTo = null;
    }
    public void ChooseNPCTicketToCheck(PassengerBrain npc)
    {
        passengerToTalkTo = npc;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        CalculateCollisionPoints();
        Gizmos.color = Color.orange;
        Gizmos.DrawLine(collisionData.groundLeft, collisionData.groundRight);

        Gizmos.color = Color.red;

        Gizmos.color = spyData.walkingIntoWall ? Color.forestGreen : Color.red;
        Gizmos.DrawLine(collisionData.wallLeft, boxCollider.bounds.center);
        Gizmos.DrawLine(collisionData.wallRight, boxCollider.bounds.center);

        Gizmos.DrawWireSphere(transform.position, spyData.interactionDist);


    }
#endif
}
