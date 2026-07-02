using System;
using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;

using static Atlas;
using static AtlasUI;
using static NPC;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class NPCBrain : MonoBehaviour
{
    public AtlasRenderer atlasRenderer;
    public Rigidbody2D rigidBody;
    public BoxCollider2D boxCollider;
    
    public NPCSO npc;
    public NPCsDataSO npcData;
    public LayerSettingsSO layerSettings;
    public TrainSettingsSO trainSettings;
    public TrainStatsSO trainStats;
    public GameEventDataSO gameEventData;
    public PlayerInputsSO playerInputs;
    public SpyStatsSO spyStats;
    public TripSO trip;
    public SpawnData spawnData;

    [Header("Generated")]
    public AtlasSO atlas;
    public StationSO boardingStation;
    public StationSO disembarkingStation;
    
    public NPCBehaviourContextSO curBehaviourContext;

    public SlideDoors curSlideDoors;
    
    public Carriage curCarriage;

    public VisualEffect curGlyph;
    
    public float targetXVelocity;
    public float targetXPos; 
    public float targetDist;
    public float behaviourClock;
    public float stateDuration;
    public float targetAlpha;
    public float atlasIndexClock;
    public float move;
    public float alphaClock;

    public Vector2 curSpriteMarkerLocalPosition;

    public NPCProfile profile;
    public AtlasClip curClip;
        
    public Behaviours curBehaviour;
    public NPCState curState;
    public NPCPath curPath;
    public Role role;

    public int smokerRoomIndex;
    public int seatPosIndex;

    public int curFrameIndex;
    public int prevAtlasIndex;

    public bool startFade;
    public bool talkingToSpy;
    public bool ticketHasBeenChecked;
    public bool playingGlyph;
    public bool behaving;
    public bool disembarking;
    public bool onTrain;

    public bool queuedForSeat;
    public bool queuedForSlideDoor;

    public int seatQueueIndex;
    public int boardTrainQueueIndex;
    public int disembarkTrainQueueIndex;

    private void OnEnable()
    {
        gameEventData.OnStationArrival.RegisterListener(PrepareToBoardTrain);
        gameEventData.OnStationSpawn.RegisterListener(PrepareToDisembarkTrain);
        CursorController.OnMouseDisabled += DisableHover;
    }
    private void OnDisable()
    {
        gameEventData.OnStationSpawn.UnregisterListener(PrepareToDisembarkTrain);
        gameEventData.OnStationArrival.UnregisterListener(PrepareToBoardTrain);
        CursorController.OnMouseDisabled -= DisableHover;
    }
    private void Start()
    {
        seatPosIndex = int.MaxValue;
        atlas = atlasRenderer.atlas;

        rigidBody.includeLayers = layerSettings.stationMask;
        curPath = NPCPath.ToStandAtStation;

        atlasRenderer.UpdateDepthRealtime((int)transform.position.z);
        smokerRoomIndex = -1; //NOTE: -1 is used as a condition to find a smokers room in the smoker state
        targetAlpha = 1;


        if (role == Role.Accomplice)
        {
            atlasRenderer.customBit |= 1 << MERIDIA_COLOR_BIT;
        }
        else
        {
            curBehaviour = GetRandomBehaviour();
        }
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
    public void BoardTrain()
    {
        transform.SetParent(curCarriage.transform, true);
        trainStats.totalNPCsBoarded++;
        QueueForSeat();
        rigidBody.includeLayers = layerSettings.trainMask;
        onTrain = true;
        SetStandingDepthInTrain();
    }
    public void DisembarkTrain()
    {
        Station station = TrainController.NextStationInstance;
        AtlasRenderer stationPlatform = station.station.isFrontOfTrain ? station.frontPlatformRenderer : station.backPlatformRenderer;
        transform.SetParent(stationPlatform.transform, true);
        atlasRenderer.UpdateDepthRealtime((int)stationPlatform.transform.position.z);
        rigidBody.includeLayers = layerSettings.stationMask;
        onTrain = false;
        curCarriage.RemoveNPC(this);
        SetPath(NPCPath.ToExitStation);
    }
    public void AssignSeat(int seatIndex)
    {
        queuedForSeat = false;
        seatPosIndex = seatIndex;
        targetXPos = curCarriage.seatData.xPos[seatPosIndex];
        if (disembarking) return;
        SetPath(NPCPath.ToSeatInTrain);
    }
    public void FindStandingPosition()
    {
        if (disembarking) return;
        SetPath(NPCPath.ToStandInTrain);
    }
    private void DisableHover()
    {
        atlasRenderer.custom.y = 0;
    }
    public void ToggleHover(bool toggle)
    {
        if (trip.unlockedRuleOutMarker)
        {
            atlasRenderer.custom.y = toggle ? 1 : 0;
        }
    }
    public void ToggleTicketCheckHover(bool toggle)
    {
        atlasRenderer.custom.w = toggle ? 1 : 0; 
    }
    public void ToggleUnveil(bool toggle)
    {
        atlasRenderer.custom.z = toggle ? 1 : 0;
        ticketHasBeenChecked = toggle;
    }
    private void SetState(NPCState newState)
    {
        if (curState == newState) return;
        ExitState(newState);
        NPCState prevState = curState;
        curState = newState;
        EnterState(prevState);
    }
    private void ChooseStates()
    {
        if (talkingToSpy)
        {
            SetState(NPCState.TicketCheck);
        }
        else if (Mathf.Abs(targetDist) >= boxCollider.bounds.extents.x)
        {
            SetState(NPCState.Walking);
        }
        else if (behaving)
        {
            SetState(NPCState.Behaviour);
        }
        else
        {
            SetState(NPCState.Idling);
        }
    }
    private void EnterState(NPCState prevState)
    {
        atlasIndexClock = 0;
        curFrameIndex = 0;

        switch (curState)
        {
            case NPCState.Idling:
            {
                stateDuration = UnityEngine.Random.Range(npc.idleDurationRange.x, npc.idleDurationRange.y);
                EnterIdlePath();
            }
            break;
            case NPCState.Walking:
            {
                curClip = atlas.clipDict[(int)NPCMotion.Walking];
            }
            break;
            case NPCState.TicketCheck:
            {
                if (curPath == NPCPath.SittingInTrain)
                {
                    NPCMotion sittingMotion = RandomIdleMotion(NPCMotion.SittingBlinking, NPCMotion.SittingBreathing);
                    curClip = atlas.clipDict[(int)sittingMotion];
                }
                else
                {
                    NPCMotion standingMotion = RandomIdleMotion(NPCMotion.StandingBlinking, NPCMotion.StandingBreathing);
                    curClip = atlas.clipDict[(int)standingMotion];
                }
            }
            break;
            case NPCState.Behaviour:
            {
                if (curBehaviourContext.glyphPrefab != null)
                {
                    curGlyph = NPCManager.GetGlyph(curBehaviourContext.glyphPrefab, transform);
                }

                if (prevState != NPCState.TicketCheck)
                {
                    stateDuration = UnityEngine.Random.Range(curBehaviourContext.minTime, curBehaviourContext.maxTime);
                }

                if (curPath == NPCPath.SittingInTrain)
                {
                    curClip = atlas.clipDict[(int)curBehaviourContext.sittingMotion];
                }
                else
                {
                    curClip = atlas.clipDict[(int)curBehaviourContext.standingMotion];
                }
            }
            break;
        }
    }
    private void UpdateStates()
    {
        switch (curState)
        {
            case NPCState.Idling:
            {
                UpdateIdlePath();

                atlasRenderer.PlayClip(ref curClip);

                if (role != Role.Accomplice)
                {
                    behaviourClock += Time.deltaTime;
                    if (behaviourClock > stateDuration)
                    {
                        PickNextBehaviour();
                    }
                }

            }
            break;
            case NPCState.Walking:
            {
                move = Mathf.Sign(targetDist);
                atlasRenderer.PlayClip(ref curClip);
                atlasRenderer.FlipHSimple(move < 0);

                if (curPath == NPCPath.ToExitStation)
                {
                    targetDist = targetXPos - transform.localPosition.x;
                }
                else
                {

                    targetDist = targetXPos - transform.position.x;
                }
            }
            break;
            case NPCState.TicketCheck:
            {
                atlasRenderer.PlayClip(ref curClip);
            }
            break;

            case NPCState.Behaviour:
            {
                if (curGlyph != null)
                {
                    atlasRenderer.PlayClip(ref curClip, curGlyph.transform);
                    if (!playingGlyph &&  atlas.motionSprites[atlasRenderer.sprite.index].markers.Length > 0)
                    {
                        curGlyph.Play();
                        playingGlyph = true;
                        if ((curBehaviour & Behaviours.Smoke_addict) != 0)
                        {
                            curGlyph.SetFloat("_Lifetime", stateDuration - behaviourClock);
                        }
                    }
                }
                else
                {
                    atlasRenderer.PlayClip(ref curClip);
                }
                behaviourClock += Time.deltaTime;

                if (behaviourClock > stateDuration) behaving = false;
            }
            break;
        }
    }
    private void FixedUpdateStates()
    {
        switch (curState)
        {
            case NPCState.Walking:
            {
                targetXVelocity = npc.moveSpeed * move;
                Vector3 localPos = transform.localPosition;
                localPos.x += targetXVelocity * Time.fixedDeltaTime;
                transform.localPosition = localPos;
            }
            break;
        }
    }
    private void ExitState(NPCState newState)
    {
        switch (curState)
        {
            case NPCState.Idling:
            {

            }
            break;
            case NPCState.Walking:
            {
                targetXVelocity = 0;
                rigidBody.linearVelocityX = 0;
            }
            break;
            case NPCState.TicketCheck:
            {
                targetDist = targetXPos - transform.position.x;
            }
            break;

            case NPCState.Behaviour:
            {
                if (curGlyph != null) NPCManager.ReturnGlyph(curBehaviourContext.glyphPrefab, curGlyph);

                if ((curBehaviour & Behaviours.Smoke_addict) != 0)
                {
                    curGlyph.Reinit();
                    curGlyph.gameObject.SetActive(false);
                    if (newState != NPCState.TicketCheck)
                    {
                        curCarriage.smokersRoomData[smokerRoomIndex].npcCount--;
                        smokerRoomIndex = -1;
                        QueueForSeat();
                    }
                }
                else if ((curBehaviour & Behaviours.Always_on_call) != 0)
                {
                    curGlyph.gameObject.SetActive(false);
                }

                if (newState != NPCState.TicketCheck)
                {
                    behaviourClock = 0;
                }

                curGlyph = null;
                playingGlyph = false;
            }
            break;
        }

    }
    private void SetPath(NPCPath newPath)
    {
        if (curPath == newPath) return;
        ExitPath();
        curPath = newPath;
        EnterPath();
    }
    private void EnterPath()
    {
        switch (curPath)
        {
            case NPCPath.SittingInTrain:
            {
                atlasRenderer.UpdateDepthRealtime(trainStats.depthSections.carriageSeat - 1);
                transform.position = new Vector3(targetXPos, transform.position.y, transform.position.z);
                atlasRenderer.FlipHSimple(false);


            }
            break;
            case NPCPath.StandingInTrain:
            {
                SetStandingDepthInTrain();
            }
            break;
            case NPCPath.AtSlideDoor:
            {
                LayerMask slideDoorLayer = trip.stationAhead.isFrontOfTrain ? layerSettings.trainLayers.exteriorSlideDoors : layerSettings.trainLayers.interiorSlideDoors;
                RaycastHit2D slideDoorHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0.0f, transform.right, 0.0f, slideDoorLayer);

                curSlideDoors = slideDoorHit.collider.GetComponent<SlideDoors>();

                if (!onTrain)
                {
                    RaycastHit2D carriageHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0.0f, transform.right, 0.0f, layerSettings.trainLayers.insideCarriageBounds);
                    curCarriage = TrainController.GetCarriage(carriageHit.collider);
                    curCarriage.AddNPC(this);
                }
                queuedForSlideDoor = false;
                behaving = false;
            }
            break;
            case NPCPath.ToSeatInTrain:
            {
                SetStandingDepthInTrain();
                targetDist = targetXPos - transform.position.x;
            }
            break;
            case NPCPath.ToStandInTrain:
            {
                targetXPos = UnityEngine.Random.Range(curCarriage.insideBoundsCollider.bounds.min.x, curCarriage.insideBoundsCollider.bounds.max.x);
                targetDist = targetXPos - transform.position.x;

                SetStandingDepthInTrain();
            }
            break;
            case NPCPath.ToSlideDoor:
            {
                float shortestDist = float.MaxValue;
                float selectedSlideDoorPos = float.MaxValue;

                if (trip.stationAhead.isFrontOfTrain)
                {
                    for (int i = 0; i < trainStats.exteriorSlideDoorPositions.Length; i++)
                    {
                        float dist = Mathf.Abs(trainStats.exteriorSlideDoorPositions[i] - transform.position.x);

                        if (dist > shortestDist) continue;
                        shortestDist = dist;
                        selectedSlideDoorPos = trainStats.exteriorSlideDoorPositions[i];

                    }
                }
                else
                {
                    for (int i = 0; i < trainStats.interiorSlideDoorPositions.Length; i++)
                    {
                        float dist = Mathf.Abs(trainStats.interiorSlideDoorPositions[i] - transform.position.x);

                        if (dist > shortestDist) continue;
                        shortestDist = dist;
                        selectedSlideDoorPos = trainStats.interiorSlideDoorPositions[i];
                    }
                }

                targetXPos = selectedSlideDoorPos;
                targetDist = targetXPos - transform.position.x;
                behaving = false;
            }
            break;
            case NPCPath.ToSmokerRoom:
            {
                if (seatPosIndex != int.MaxValue && curCarriage.seatData.filled[seatPosIndex])
                {
                    curCarriage.seatData.filled[seatPosIndex] = false;
                    seatPosIndex = int.MaxValue;
                }
                if (queuedForSeat) curCarriage.RemoveFromSeatQueue(this); // To prevent them from going back to the chair if they are queued
                SetStandingDepthInTrain();
                if (curCarriage.smokersRoomData.Length > 1 && curCarriage.smokersRoomData[1].npcCount < curCarriage.smokersRoomData[0].npcCount) // selected smoker room is based on which room has less npcs
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

            case NPCPath.AtSmokerRoom:
            {
                SetStandingDepthInTrain();
            }
            break;

            case NPCPath.ToExitStation:
            {
                targetXPos = trip.stationsDataArray[profile.disembarkingStationIndex].exitLocalPosX;
                targetDist = targetXPos - transform.localPosition.x;
            }
            break;
        }
    }
    private void EnterIdlePath()
    {
        switch (curPath)
        {
            case NPCPath.SittingInTrain:
            case NPCPath.SittingAtStation:
            {
                NPCMotion sittingMotion = RandomIdleMotion(NPCMotion.SittingBlinking, NPCMotion.SittingBreathing);
                curClip = atlas.clipDict[(int)sittingMotion];
            }
            break;

            case NPCPath.StandingInTrain:
            case NPCPath.StandingAtStation:
            {
                NPCMotion standingMotion = RandomIdleMotion(NPCMotion.StandingBlinking, NPCMotion.StandingBreathing);
                curClip = atlas.clipDict[(int)standingMotion];
            }
            break;
        }
    }
    private void UpdatePath()
    {
        switch (curPath)
        {
            case NPCPath.AtSlideDoor:
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
            case NPCPath.StandingAtStation:
            {
            }
            break;
            case NPCPath.SittingAtStation:
            {
            }
            break;
            case NPCPath.ToExitStation:
            {
                if (atlasRenderer.bounds.max.x < spawnData.bounds.min.x)
                {
                    disembarking = false;
                    ToggleUnveil(false);
                    NPCManager.ReturnNPC(trip.npcDataArray[profile.npcPrefabIndex].prefab , this);
                }
            }
            break;
            case NPCPath.ToStandAtStation:
            {
            }
            break;
            case NPCPath.ToSeatInTrain:
            {

            }
            break;
            case NPCPath.ToStandInTrain:
            {
            }
            break;
            case NPCPath.ToSlideDoor:
            {
            }
            break;
            case NPCPath.ToSmokerRoom:
            {
            }
            break;
        }
    }
    private void UpdateIdlePath()
    {
        switch (curPath)
        {
            case NPCPath.ToSeatAtStation:
            {
                NPCMotion sittingMotion = RandomIdleMotion(NPCMotion.SittingBlinking, NPCMotion.SittingBreathing);
                SetPath(NPCPath.SittingAtStation);
                curClip = atlas.clipDict[(int)sittingMotion];
            }
            break;
            case NPCPath.ToSeatInTrain:
            {
                NPCMotion sittingMotion = RandomIdleMotion(NPCMotion.SittingBlinking, NPCMotion.SittingBreathing);
                SetPath(NPCPath.SittingInTrain);
                curClip = atlas.clipDict[(int)sittingMotion];
            }
            break;

            case NPCPath.ToSlideDoor:
            {
                NPCMotion standingMotion = RandomIdleMotion(NPCMotion.StandingBlinking, NPCMotion.StandingBreathing);

                if (spyStats.curLocationState == Spy.LocationState.Station && trainStats.curStationIndex > 0)
                {
                    SetPath(NPCPath.StandingAtStation);
                }
                else
                {
                    SetPath(NPCPath.AtSlideDoor);
                }

                curClip = atlas.clipDict[(int)standingMotion];
            }
            break;

            case NPCPath.ToStandAtStation:
            {
                NPCMotion standingMotion = RandomIdleMotion(NPCMotion.StandingBlinking, NPCMotion.StandingBreathing);
                SetPath(NPCPath.StandingAtStation);
                curClip = atlas.clipDict[(int)standingMotion];
            }
            break;

            case NPCPath.ToSmokerRoom:
            {
                NPCMotion standingMotion = RandomIdleMotion(NPCMotion.StandingBlinking, NPCMotion.StandingBreathing);
                SetPath(NPCPath.AtSmokerRoom);
                curClip = atlas.clipDict[(int)standingMotion];
            }
            break;
            case NPCPath.ToStandInTrain:
            {
                NPCMotion standingMotion = RandomIdleMotion(NPCMotion.StandingBlinking, NPCMotion.StandingBreathing);
                SetPath(NPCPath.StandingInTrain);
                curClip = atlas.clipDict[(int)standingMotion];
            }
            break;
        }
    }
    private void ExitPath()
    {
        switch (curPath)
        {
            case NPCPath.AtSlideDoor:
            {

            }
            break;
            case NPCPath.ToSeatInTrain:
            {
            }
            break;

            case NPCPath.ToSlideDoor:
            {
            }
            break;

            case NPCPath.ToSmokerRoom:
            {

            }
            break;

        }
    }
    private void PrepareToDisembarkTrain()
    {
        if (!onTrain || trip.stationAhead.stationIndex != profile.disembarkingStationIndex) return;
        disembarking = true;
        if (queuedForSeat) curCarriage.RemoveFromSeatQueue(this);
        if (seatPosIndex != int.MaxValue && curCarriage.seatData.filled[seatPosIndex])
        {
            curCarriage.seatData.filled[seatPosIndex] = false;
        }
        SetPath(NPCPath.ToSlideDoor);
    }
    private void QueueForSeat()
    {
        if (disembarking) return;
        curCarriage?.AddToSeatQueue(this);
        queuedForSeat = true;
    }
    private void SetStandingDepthInTrain()
    {
        if (!onTrain) return;
        int depth = UnityEngine.Random.Range(trainStats.depthSections.frontMin, trainStats.depthSections.backMax);
        atlasRenderer.UpdateDepthRealtime(depth);
    }
    private void PickNextBehaviour()
    {
        behaviourClock = 0;
        if (!onTrain || disembarking) return;

        int maxBits = 32;

        int currentIndex = (int)Mathf.Log((int)curBehaviour, 2);

        for (int i = 1; i <= maxBits; i++)
        {
            int nextIndex = (currentIndex + i) % maxBits;
            int nextBit = 1 << nextIndex;

            if ((profile.behaviours & (Behaviours)nextBit) != 0)
            {
                curBehaviour = (Behaviours)nextBit;
                curBehaviourContext = npcData.behaviourContextDict[curBehaviour];

                if (curBehaviourContext.pathToTake != NPCPath.None)
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
        if (onTrain || trip.stationAhead.stationIndex != profile.boardingStationIndex) return;
        SetPath(NPCPath.ToSlideDoor);
    }
    private NPCMotion RandomIdleMotion(NPCMotion motion1, NPCMotion motion2)
    {
        return UnityEngine.Random.Range(0, 2) == 0 ? motion1 : motion2;
    }
    public Behaviours GetRandomBehaviour()
    {
        Behaviours[] allBehaviours = (Behaviours[])Enum.GetValues(typeof(Behaviours));

        List<Behaviours> allowedBehaviours = new List<Behaviours>();
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            Behaviours b = allBehaviours[i];
            if (b != 0 && profile.behaviours.HasFlag(b))
            {
                allowedBehaviours.Add(b);
            }
        }

        Behaviours selectedBehaviour = allowedBehaviours[UnityEngine.Random.Range(0, allowedBehaviours.Count)];
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
#endif
}
