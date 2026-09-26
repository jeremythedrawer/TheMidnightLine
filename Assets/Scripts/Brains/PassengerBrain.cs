using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using UnityEngine;
using UnityEngine.VFX;

using Cysharp.Threading.Tasks;

using static Atlas;
using static AtlasUI;
using static Passenger;
using static Spy;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class PassengerBrain : MonoBehaviour
{
    public AtlasRenderer atlasRenderer;
    public AudioSource audioSource;

    public Options options;
    public PassengerData passengerData;
    public PassengersData passengersData;
    public TrainData trainData;
    public InputData inputData;
    public SpawnData spawnData;
    public CameraData camData;
    public AudioData audioData;
    public ActionData actionData;

    [Header("Generated")]
    public AtlasSO atlas;
    public StationSO boardingStation;
    public StationSO disembarkingStation;
    
    public HabitData curBehaviourContext;

    public SlideDoors curSlideDoors;
    
    public Carriage curCarriage;

    public VisualEffect curGlyph;

    public Graffiti graffiti;

    public CancellationTokenSource ctsWaitForRandSeconds;

    public PassengerProfile profile;
    public AtlasClip curClip;

    public float targetXVelocity;
    public float targetXPos; 
    public float targetDist;
    public float behaviourClock;
    public float stateDuration;
    public float targetAlpha;
    public float move;
    public float alphaClock;
    public float prevDepth;
        
    public Habits curHabit;
    public PassengerState curState;
    public PassengerPath curPath;
    public PassengerPath prevPath;
    public Role role;

    public int smokerRoomIndex;
    public int seatPosIndex;

    public int curFrameIndex;
    public int prevAtlasIndex;
    public int seatQueueIndex;
    public int boardTrainQueueIndex;
    public int disembarkTrainQueueIndex;
    public int carriagePassengersIndex;

    public bool talkingToSpy;
    public bool revealed;
    public bool playingGlyph;
    public bool behaving;
    public bool stopBehaving;
    public bool onTrain;
    public bool queuedForSeat;
    public bool queuedForSlideDoor;
    public bool playingPrepBehaviour;
    public bool toQueueForSeat;
    public bool atDepthForExample;

    public delegate void Callback();
    private void OnEnable()
    {
        TrainController.OnStationArrival += PrepareToBoardTrain;
        TrainController.OnStationSpawn += PrepareToDisembarkTrain;
        atlasRenderer.onChangeKeyframe += HandleKeyframeChange;
    }
    private void OnDisable()
    {
        TrainController.OnStationArrival -= PrepareToBoardTrain;
        TrainController.OnStationSpawn -= PrepareToDisembarkTrain;

        atlasRenderer.onChangeKeyframe -= HandleKeyframeChange;
        ctsWaitForRandSeconds?.Cancel();
    }
    private void OnDestroy()
    {
        TrainController.OnStationArrival -= PrepareToBoardTrain;
        TrainController.OnStationSpawn -= PrepareToDisembarkTrain;    
        ctsWaitForRandSeconds?.Cancel();
    }
    private void Update()
    {
        ChooseStates();
        UpdateStates();
        UpdatePath();
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
    }
    public void Init()
    {
        seatPosIndex = int.MaxValue;
        atlas = atlasRenderer.atlas;

        curPath = PassengerPath.ToStandAtStation;

        smokerRoomIndex = -1;
        targetAlpha = 1;


        if (role == Role.Accomplice)
        {
            atlasRenderer.customBit |= (int)ColorBits.Meridia;
        }
        else
        {
            curHabit = GetRandomBehaviour();
        }
        atlasRenderer.customBit |= (int)ColorBits.GreenChannel;
    }
    public void BoardTrain()
    {
        trainData.totalNPCsBoarded++;
        onTrain = true;
        QueueForSeat();
        SetStandingDepthInTrain();

        if (role == Role.Traitor)
        {
            actionData.onTraitorBoarded?.Invoke();
        }
    }
    public void DisembarkTrain()
    {
        Station station = TrainController.NextStationInstance;
        AtlasRenderer stationPlatform = station.platformRenderer;
        transform.SetParent(stationPlatform.transform, true);
        atlasRenderer.SetWorldDepth((int)stationPlatform.transform.position.z);
        onTrain = false;
        curCarriage.RemovePassenger(this);
        SetPath(PassengerPath.ToExitStation);
        if (role == Role.Traitor)
        {
            actionData.onTraitorDisembarked?.Invoke();
        }
    }
    public void MoveNPCToLeftOfCarriage()
    {
        behaving = false;
        
        curPath = PassengerPath.None;
        SetPath(PassengerPath.ToStandInTrain);
    }
    public void AssignSeat(int seatIndex)
    {
        queuedForSeat = false;
        seatPosIndex = seatIndex;
        curCarriage.seatData.filled[seatPosIndex] = true;
        targetXPos = curCarriage.seatData.xPos[seatPosIndex];
        if (stopBehaving) return;
        SetPath(PassengerPath.ToSeatInTrain);
    }
    public void FindStandingPosition()
    {
        if (stopBehaving) return;
        SetPath(PassengerPath.ToStandInTrain);
    }
    public void ToggleFocus(bool toggle)
    {
        atlasRenderer.custom.w = toggle ? 1 : 0; 
    }
    public void ToggleUnveil(bool toggle)
    {
        if (toggle)
        {
            atlasRenderer.customBit |= (int)ColorBits.RedChannel;
            atlasRenderer.customBit &= ~(int)ColorBits.GreenChannel;
        }
        else
        {
            atlasRenderer.customBit &= ~(int)ColorBits.RedChannel;
            atlasRenderer.customBit |= (int)ColorBits.GreenChannel;
        }

        revealed = toggle;
    }
    private void SetState(PassengerState newState)
    {
        if (curState == newState) return;
        ExitState(newState);
        PassengerState prevState = curState;
        curState = newState;
        EnterState(prevState);
    }
    private void ChooseStates()
    {
        if (talkingToSpy)
        {
            SetState(PassengerState.TicketCheck);
        }
        else if (Mathf.Abs(targetDist) >= 0.04f)
        {
            SetState(PassengerState.Walking);
        }
        else if (behaving)
        {
            SetState(PassengerState.Behaviour);
        }
        else
        {
            SetState(PassengerState.Idling);
        }
    }
    private void EnterState(PassengerState prevState)
    {
        curFrameIndex = 0;

        switch (curState)
        {
            case PassengerState.Idling:
            {
                stateDuration = UnityEngine.Random.Range(passengerData.idleDurationRange.x, passengerData.idleDurationRange.y);
                EnterIdlePath();
            }
            break;
            case PassengerState.Walking:
            {
                curClip = atlas.clipDict[(int)PassengerMotion.Walking];
            }
            break;
            case PassengerState.TicketCheck:
            {
                if (curPath == PassengerPath.SittingInTrain)
                {
                    PassengerMotion sittingMotion = RandomIdleMotion(PassengerMotion.SittingBlinking, PassengerMotion.SittingBreathing);
                    curClip = atlas.clipDict[(int)sittingMotion];
                }
                else
                {
                    PassengerMotion standingMotion = RandomIdleMotion(PassengerMotion.StandingBlinking, PassengerMotion.StandingBreathing);
                    curClip = atlas.clipDict[(int)standingMotion];
                }
            }
            break;
            case PassengerState.Behaviour:
            {
                if (curBehaviourContext.glyphPrefab != null)
                {
                    curGlyph = PassengerManager.GetGlyph(curBehaviourContext.glyphPrefab, transform);
                    curGlyph.transform.position = transform.position;
                }

                if (curHabit == Habits.Known_vandal)
                {
                    graffiti = PassengerManager.GetGraffitiRenderer(passengersData.graffitiPrefab);
                    int graffitiIndex = UnityEngine.Random.Range(0, graffiti.atlas.simpleSprites.Length - 1);
                    graffiti.SetSprites(graffitiIndex);
                    graffiti.transform.position = new Vector3(transform.position.x, atlasRenderer.bounds.max.y, trainData.depthSections.carriageSeat + 1.5f);
                    graffiti.transform.SetParent(curCarriage.transform);
                    atlasRenderer.SetWorldDepth(trainData.depthSections.backStandingBack);
                }

                if (prevState != PassengerState.TicketCheck)
                {
                    stateDuration = UnityEngine.Random.Range(curBehaviourContext.minTime, curBehaviourContext.maxTime);
                }

                if (curBehaviourContext.sittingPrepMotion != PassengerMotion.None)
                {
                    if (curPath == PassengerPath.SittingInTrain)
                    {
                        curClip = atlas.clipDict[(int)curBehaviourContext.sittingPrepMotion];
                    }
                    else
                    {
                        curClip = atlas.clipDict[(int)curBehaviourContext.standingPrepMotion];
                    }
                    playingPrepBehaviour = true;
                }
                else
                {
                    if (curPath == PassengerPath.SittingInTrain)
                    {
                        curClip = atlas.clipDict[(int)curBehaviourContext.sittingMotion];
                    }
                    else
                    {
                        curClip = atlas.clipDict[(int)curBehaviourContext.standingMotion];
                    }
                }
            }
            break;
        }
    }
    private void UpdateStates()
    {
        switch (curState)
        {
            case PassengerState.Idling:
            {
                UpdateIdlePath();

                atlasRenderer.PlayClip(curClip, audioSource: audioSource, audioData: audioData);

                if (onTrain)
                {
                    behaviourClock += Time.deltaTime;

                    if (behaviourClock > stateDuration)
                    {
                        if (toQueueForSeat && (PassengerMotion)curClip.motionIndex == PassengerMotion.StandingBlinking || (PassengerMotion)curClip.motionIndex == PassengerMotion.StandingBreathing)
                        {
                            QueueForSeat();
                            toQueueForSeat = false;
                        }
                        else
                        {
                            if (role != Role.Accomplice)
                            {
                                PickNextBehaviour();
                            }                    
                            toQueueForSeat = true;
                        }
                        behaviourClock = 0;
                    }
                }
            }
            break;
            case PassengerState.Walking:
            {
                move = Mathf.Sign(targetDist);
                atlasRenderer.PlayClip(curClip, audioSource: audioSource, audioData: audioData);
                atlasRenderer.FlipHSimple(move < 0);

                if (curPath == PassengerPath.ToExitStation)
                {
                    targetDist = targetXPos - transform.localPosition.x;
                }
                else
                {

                    targetDist = targetXPos - transform.position.x;
                }
            }
            break;
            case PassengerState.TicketCheck:
            {
                atlasRenderer.PlayClip(curClip);
            }
            break;

            case PassengerState.Behaviour:
            {
                if (playingPrepBehaviour)
                {
                    if (atlasRenderer.curFrameIndex == curClip.keyframeEndIndex)
                    {
                        if (curPath == PassengerPath.SittingInTrain)
                        {
                            curClip = atlas.clipDict[(int)curBehaviourContext.sittingMotion];
                        }
                        else
                        {
                            curClip = atlas.clipDict[(int)curBehaviourContext.standingMotion];
                        }
                        playingPrepBehaviour = false;
                    }
                }


                if (curGlyph != null)
                {
                    if (atlas.motionSprites[atlasRenderer.sprite.index].markers.Length > 0)
                    {
                        if (!playingGlyph)
                        {
                            curGlyph.gameObject.SetActive(true);
                            curGlyph.Play();
                            playingGlyph = true;
                            if (curHabit == Habits.Smoke_addict)
                            {
                                curGlyph.SetFloat("_Lifetime", stateDuration - behaviourClock);
                            }

                            if (curHabit == Habits.Always_on_call)
                            {

                            }
                        }
                    }
                    else if (playingGlyph)
                    {
                        playingGlyph = false;
                        curGlyph.Stop();
                    }
                    atlasRenderer.PlayClip(curClip, markerTransform: curGlyph.transform, audioSource: audioSource, audioData: audioData);
                }
                else
                {
                    atlasRenderer.PlayClip(curClip, audioSource: audioSource, audioData: audioData);
                }

                behaviourClock += Time.deltaTime;

                if (curHabit == Habits.Known_vandal)
                {
                    graffiti.UpdateAlpha(behaviourClock / stateDuration);
                }

                if (behaviourClock > stateDuration) behaving = false;
            }
            break;
        }
    }
    private void FixedUpdateStates()
    {
        switch (curState)
        {
            case PassengerState.Walking:
            {
                targetXVelocity = passengerData.moveSpeed * move;
                Vector3 localPos = transform.localPosition;
                localPos.x += targetXVelocity * Time.fixedDeltaTime;
                transform.localPosition = localPos;
            }
            break;
        }
    }
    private void ExitState(PassengerState newState)
    {
        switch (curState)
        {
            case PassengerState.Idling:
            {

            }
            break;
            case PassengerState.Walking:
            {
                targetXVelocity = 0;
            }
            break;
            case PassengerState.TicketCheck:
            {
                targetDist = targetXPos - transform.position.x;
            }
            break;

            case PassengerState.Behaviour:
            {
                if (curGlyph != null) PassengerManager.ReturnGlyph(curBehaviourContext.glyphPrefab, curGlyph);


                switch(curHabit)
                {
                    case Habits.Smoke_addict:
                    {
                        curGlyph.Reinit();
                        curGlyph.gameObject.SetActive(false);
                        if (newState != PassengerState.TicketCheck)
                        {
                            curCarriage.smokersRoomData[smokerRoomIndex].npcCount--;
                            smokerRoomIndex = -1;
                            QueueForSeat();
                        }
                    }
                    break;

                    case Habits.Always_on_call:
                    {
                        curGlyph.gameObject.SetActive(false);
                    }
                    break;

                    case Habits.Known_vandal:
                    {
                        graffiti.Dissappear();

                        if (newState != PassengerState.TicketCheck)
                        {
                            QueueForSeat();
                        }
                    }
                    break;
                }

                if (newState != PassengerState.TicketCheck)
                {
                    behaviourClock = 0;
                }

                curGlyph = null;
                playingGlyph = false;
            }
            break;
        }

    }
    private void SetPath(PassengerPath newPath)
    {
        if (curPath == newPath) return;
        ExitPath();
        prevPath = curPath;
        curPath = newPath;
        EnterPath();
    }
    private void EnterPath()
    {
        switch (curPath)
        {
            case PassengerPath.SittingInTrain:
            {
                float seatDepthOffset = (float)seatPosIndex / (float)curCarriage.seatData.xPos.Length;

                atlasRenderer.SetWorldDepth(trainData.depthSections.carriageSeat + seatDepthOffset);
                transform.position = new Vector3(targetXPos, transform.position.y, transform.position.z);
                atlasRenderer.FlipHSimple(false);
            }
            break;
            case PassengerPath.StandingInTrain:
            {
                StopSitting();
                SetStandingDepthInTrain();
            }
            break;
            case PassengerPath.AtSlideDoor:
            {
                StopSitting();

                if (!onTrain)
                {
                    curCarriage = curSlideDoors.carriage;
                    curCarriage.AddPassenger(this);
                    transform.SetParent(curCarriage.transform, worldPositionStays: true);
                }
                queuedForSlideDoor = false;
                behaving = false;
            }
            break;
            case PassengerPath.ToSeatInTrain:
            {
                SetStandingDepthInTrain();
                targetDist = targetXPos - transform.position.x;
            }
            break;
            case PassengerPath.ToStandInTrain:
            {
                StopSitting();

                targetXPos = curCarriage.GetPositionBetweenLargestGap();

                if (curBehaviourContext != null && curBehaviourContext.habit == Habits.Known_vandal)
                {
                    for (int i = 0; i < curCarriage.interiorSlideDoors.Length; i++)
                    {
                        SlideDoors slideDoor = curCarriage.interiorSlideDoors[i];
                        Bounds slideDoorBounds = slideDoor.boxCollider.bounds;
                        float slideDoorWidth = slideDoorBounds.size.x;
                        if (targetXPos > slideDoorBounds.min.x - slideDoorWidth && targetXPos < slideDoorBounds.max.x + slideDoorWidth)
                        {
                            if (targetXPos < slideDoorBounds.center.x)
                            {
                                targetXPos -= slideDoorBounds.size.x;
                            }
                            else
                            {
                                targetXPos += slideDoorBounds.size.x;
                            }
                            break;
                        }
                    }
                }

                targetDist = targetXPos - transform.position.x;
                SetStandingDepthInTrain();
            }
            break;
            case PassengerPath.ToSlideDoor:
            {
                StopSitting();

                if (options.curTrip.stationAhead.isFrontOfTrain)
                {
                    bool foundDoor = false;
                    for (int i = 0; i < trainData.exteriorSlideDoorXBounds.Length; i++)
                    {
                        if (transform.position.x > trainData.exteriorSlideDoorXBounds[i])
                        {
                            curSlideDoors = TrainController.ExteriorSlideDoors[i];

                            foundDoor = true;
                            break;
                        }
                    }

                    if (!foundDoor) curSlideDoors = TrainController.ExteriorSlideDoors[^1];

                }
                else
                {
                    bool foundDoor = false;
                    for (int i = 0; i < trainData.interiorSlideDoorXBounds.Length; i++)
                    {
                        if (transform.position.x > trainData.interiorSlideDoorXBounds[i])
                        {
                            curSlideDoors = TrainController.InteriorSlideDoors[i];

                            foundDoor = true;
                            break;
                        }
                    }

                    if (!foundDoor) curSlideDoors = TrainController.InteriorSlideDoors[^1];
                }

                float extents = passengerData.triggerRadius;
                float slideDoorsXPos = curSlideDoors.transform.position.x;
                float minX =  slideDoorsXPos - extents;
                float maxX = slideDoorsXPos + extents;
                targetXPos = UnityEngine.Random.Range(minX, maxX);
                targetDist = targetXPos - transform.position.x;
                behaving = false;
            }
            break;
            case PassengerPath.ToSmokerRoom:
            {
                StopSitting();
                SetStandingDepthInTrain();

                if (curCarriage.smokersRoomData.Length > 1 && curCarriage.smokersRoomData[1].npcCount < curCarriage.smokersRoomData[0].npcCount)
                {
                    smokerRoomIndex = 1;
                }
                else
                {
                    smokerRoomIndex = 0;
                }

                curCarriage.smokersRoomData[smokerRoomIndex].npcCount++;
                targetXPos = UnityEngine.Random.Range(curCarriage.smokersRoomData[smokerRoomIndex].minXPos, curCarriage.smokersRoomData[smokerRoomIndex].maxXPos);
                targetDist = targetXPos - transform.position.x;
            }
            break;
            case PassengerPath.AtSmokerRoom:
            {
                SetStandingDepthInTrain();
            }
            break; 
            case PassengerPath.ToExitStation:
            {

                targetXPos = options.curTrip.stationsDataArray[profile.disembarkingStationIndex].exitLocalPosX;
                targetDist = targetXPos - transform.localPosition.x;
            }
            break;
        }
    }
    private void EnterIdlePath()
    {
        switch (curPath)
        {
            case PassengerPath.SittingInTrain:
            case PassengerPath.SittingAtStation:
            {
                PassengerMotion sittingMotion = RandomIdleMotion(PassengerMotion.SittingBlinking, PassengerMotion.SittingBreathing);
                curClip = atlas.clipDict[(int)sittingMotion];
            }
            break;

            case PassengerPath.StandingInTrain:
            case PassengerPath.StandingAtStation:
            {
                PassengerMotion standingMotion = RandomIdleMotion(PassengerMotion.StandingBlinking, PassengerMotion.StandingBreathing);
                curClip = atlas.clipDict[(int)standingMotion];
            }
            break;
        }
    }
    private void UpdatePath()
    {
        switch (curPath)
        {
            case PassengerPath.AtSlideDoor:
            {
                if (curSlideDoors.curState == SlideDoors.State.Unlocked)
                {
                    curSlideDoors.OpenDoors();
                }

                if (!queuedForSlideDoor)
                {
                    if (curSlideDoors.curState == SlideDoors.State.Opened)
                    {
                        if (!onTrain)
                        {
                            curSlideDoors.AddToBoardTrainQueue(this);
                        }
                        else
                        {
                            curSlideDoors.AddToDisembarkTrainQueue(this);
                        }
                        queuedForSlideDoor = true;
                    }
                }
            }
            break;
            case PassengerPath.StandingAtStation:
            {
                if (prevPath == PassengerPath.ToSlideDoor && camData.curLocationState != CameraData.LocationState.Station)
                {
                    SetPath(PassengerPath.ToSlideDoor);
                }
            }
            break;
            case PassengerPath.SittingAtStation:
            {
            }
            break;
            case PassengerPath.ToExitStation:
            {
                if (atlasRenderer.bounds.max.x < spawnData.bounds.min.x)
                {
                    stopBehaving = false;
                    ToggleUnveil(false);
                    atlasRenderer.custom.x = 0;
                    atlasRenderer.custom.y = 0;
                    atlasRenderer.custom.z = 0;
                    atlasRenderer.custom.w = 0;
                    atlasRenderer.customBit = 0;
                    PassengerManager.ReturnPassenger(options.curTrip.passengers[profile.npcPrefabIndex].prefab , this);
                }
            }
            break;
            case PassengerPath.ToStandAtStation:
            {
            }
            break;
            case PassengerPath.ToSeatInTrain:
            {

            }
            break;
            case PassengerPath.ToStandInTrain:
            {
            }
            break;
            case PassengerPath.ToSlideDoor:
            {
            }
            break;
            case PassengerPath.ToSmokerRoom:
            {
            }
            break;
        }
    }
    private void UpdateIdlePath()
    {
        switch (curPath)
        {
            case PassengerPath.ToSeatAtStation:
            {
                PassengerMotion sittingMotion = RandomIdleMotion(PassengerMotion.SittingBlinking, PassengerMotion.SittingBreathing);
                SetPath(PassengerPath.SittingAtStation);
                curClip = atlas.clipDict[(int)sittingMotion];
            }
            break;
            case PassengerPath.ToSeatInTrain:
            {
                PassengerMotion sittingMotion = RandomIdleMotion(PassengerMotion.SittingBlinking, PassengerMotion.SittingBreathing);
                SetPath(PassengerPath.SittingInTrain);
                curClip = atlas.clipDict[(int)sittingMotion];
            }
            break;

            case PassengerPath.ToSlideDoor:
            {
                PassengerMotion standingMotion = RandomIdleMotion(PassengerMotion.StandingBlinking, PassengerMotion.StandingBreathing);

                if (camData.curLocationState == CameraData.LocationState.Station && trainData.curStationIndex > 0)
                {
                    SetPath(PassengerPath.StandingAtStation);
                }
                else
                {
                    SetPath(PassengerPath.AtSlideDoor);
                }

                curClip = atlas.clipDict[(int)standingMotion];
            }
            break;

            case PassengerPath.ToStandAtStation:
            {
                PassengerMotion standingMotion = RandomIdleMotion(PassengerMotion.StandingBlinking, PassengerMotion.StandingBreathing);
                SetPath(PassengerPath.StandingAtStation);
                curClip = atlas.clipDict[(int)standingMotion];
            }
            break;

            case PassengerPath.ToSmokerRoom:
            {
                PassengerMotion standingMotion = RandomIdleMotion(PassengerMotion.StandingBlinking, PassengerMotion.StandingBreathing);
                SetPath(PassengerPath.AtSmokerRoom);
                curClip = atlas.clipDict[(int)standingMotion];
            }
            break;
            case PassengerPath.ToStandInTrain:
            {
                PassengerMotion standingMotion = RandomIdleMotion(PassengerMotion.StandingBlinking, PassengerMotion.StandingBreathing);
                SetPath(PassengerPath.StandingInTrain);
                curClip = atlas.clipDict[(int)standingMotion];
            }
            break;
        }
    }
    private void ExitPath()
    {
        switch (curPath)
        {
            case PassengerPath.AtSlideDoor:
            {

            }
            break;
            case PassengerPath.ToSeatInTrain:
            {
            }
            break;

            case PassengerPath.ToSlideDoor:
            {
            }
            break;

            case PassengerPath.ToSmokerRoom:
            {

            }
            break;

        }
    }
    private void PrepareToDisembarkTrain()
    {
        if (!onTrain || options.curTrip.stationAhead.stationIndex != profile.disembarkingStationIndex) return;

        stopBehaving = true;
        StopSitting();

        Callback callback = SetPathToSlideDoorCallback;
        ctsWaitForRandSeconds?.Cancel();
        ctsWaitForRandSeconds = new CancellationTokenSource();
        WaitForRandomSeconds(callback).Forget();
    }
    private void QueueForSeat()
    {
        if (stopBehaving) return;
        curCarriage?.AddToSeatQueue(this);
        queuedForSeat = true;
    }
    private void StopSitting()
    {
        if (queuedForSeat) curCarriage.RemoveFromSeatQueue(this);
        if (seatPosIndex != int.MaxValue && curCarriage.seatData.filled[seatPosIndex])
        {
            curCarriage.seatData.filled[seatPosIndex] = false;
            seatPosIndex = int.MaxValue;
        }
    }
    private void SetStandingDepthInTrain()
    {
        if (!onTrain) return;
        int depth = UnityEngine.Random.Range(trainData.depthSections.frontStandingBack, trainData.depthSections.backStandingFront);
        atlasRenderer.SetWorldDepth(depth);
    }
    private void PickNextBehaviour()
    {

        if (!onTrain || stopBehaving) return;

        int maxBits = 32;

        int currentIndex = (int)Mathf.Log((int)curHabit, 2);

        for (int i = 1; i <= maxBits; i++)
        {
            int nextIndex = (currentIndex + i) % maxBits;
            int nextBit = 1 << nextIndex;

            if ((profile.habits & (Habits)nextBit) != 0)
            {
                curHabit = (Habits)nextBit;
                curBehaviourContext = passengersData.habitDataDict[curHabit];

                if (curBehaviourContext.pathToTake != PassengerPath.None)
                {
                    SetPath(curBehaviourContext.pathToTake);
                }

                behaving = true;
                return;
            }
        }
    }
    private void PrepareToBoardTrain()
    {
        if (onTrain || options.curTrip.stationAhead.stationIndex != profile.boardingStationIndex) return;
        Callback callback = SetPathToSlideDoorCallback;
        ctsWaitForRandSeconds?.Cancel();
        ctsWaitForRandSeconds = new CancellationTokenSource();
        WaitForRandomSeconds(callback).Forget();
    }
    private void SetPathToSlideDoorCallback()
    {
        SetPath(PassengerPath.ToSlideDoor);
    }
    private void HandleKeyframeChange(MotionSprite curSprite)
    {
        switch (curState)
        {
            case PassengerState.Walking:
            {
                if (curSprite.audioIndex == 0)
                {
                    AudioClip[] footStepSounds = onTrain ? audioData.footStepTrain : audioData.footStepsConcrete;

                    int randFootstepIndex = UnityEngine.Random.Range(0, footStepSounds.Length);
                    curClip.audioClips[0] = footStepSounds[randFootstepIndex];
                }
            }
            break;
        }
    }
    private async UniTask WaitForRandomSeconds(Callback callback)
    {
        try
        {
            await UniTask.Yield();
            float randTime = UnityEngine.Random.Range(1, 3);
            await UniTask.WaitForSeconds(randTime, cancellationToken: ctsWaitForRandSeconds.Token);
            callback();
        }
        catch(OperationCanceledException)
        {

        }
    }
    private PassengerMotion RandomIdleMotion(PassengerMotion motion1, PassengerMotion motion2)
    {
        return UnityEngine.Random.Range(0, 2) == 0 ? motion1 : motion2;
    }
    public Habits GetRandomBehaviour()
    {
        Habits[] allBehaviours = (Habits[])Enum.GetValues(typeof(Habits));

        List<Habits> allowedBehaviours = new List<Habits>();
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            Habits b = allBehaviours[i];
            if (b != 0 && profile.habits.HasFlag(b))
            {
                allowedBehaviours.Add(b);
            }
        }

        Habits selectedBehaviour = allowedBehaviours[UnityEngine.Random.Range(0, allowedBehaviours.Count)];
        return selectedBehaviour;
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector3 typeLabel = transform.position + atlasRenderer.sprite.worldSize + Vector3.up * 0.2f;
        Vector3 stateLabel = typeLabel + Vector3.up;

        GUIStyle typeStyle = new GUIStyle();

        switch(role)
        {
            case Role.Accomplice:
            {
                typeStyle.normal.textColor = Color.yellow;
            }
            break;

            case Role.Traitor:
            {
                typeStyle.normal.textColor = Color.red;
            }
            break;

            case Role.Bystander:
            {
                typeStyle.normal.textColor = Color.green;
            }
            break;
        }
        typeStyle.alignment = TextAnchor.UpperCenter;
        typeStyle.fontSize = 10;

        Handles.Label(typeLabel, role.ToString(), typeStyle);
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 a = new Vector2();
        a.x = transform.position.x - passengerData.triggerRadius;
        a.y = atlasRenderer.bounds.center.y;
        Vector2 b = new Vector2();
        b.x = transform.position.x + passengerData.triggerRadius;
        b.y = atlasRenderer.bounds.center.y;
        Handles.color = Color.red;
        Handles.DrawLine(a, b);
    }
#endif
}
