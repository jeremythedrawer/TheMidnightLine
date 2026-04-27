using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using static Atlas;
using static NPC;
using UnityEngine.VFX;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class NPCBrain : MonoBehaviour
{
    public NPCSO npc;
    public AtlasRenderer atlasRenderer;
    public NPCsDataSO npcData;
    public LayerSettingsSO layerSettings;
    public TrainSettingsSO trainSettings;
    public TrainStatsSO trainStats;
    public GameEventDataSO gameEventData;
    public PlayerInputsSO playerInputs;
    public SpyStatsSO spyStats;
    public TripSO trip;

    public Rigidbody2D rigidBody;
    public BoxCollider2D boxCollider;

    [Header("Generated")]
    public AtlasSO atlas;
    public StationSO boardingStation;
    public StationSO disembarkingStation;
    public NPCBehaviourContextSO curBehaviourContext;

    public SlideDoors curSlideDoors;
    public Carriage curCarriage;
    public VisualEffect curGlyph;

    public Vector2 curSpriteMarkerLocalPosition;
    public float targetXVelocity;
    public float targetXPos; 
    public float targetDist;
    public float behaviourClock;
    public float stateDuration;
    public float targetAlpha;
    public float atlasIndexClock;
    public float move;

    public float alphaClock;
    public float hoverClock;

    public Behaviours[] behaviourFlags;
    public Behaviours curBehaviour;
    public NPCProfile profile;
    public AtlasClip curClip;
        
    public NPCState curState;
    public NPCPath curPath;
    public Role role;

    public int smokerRoomIndex;
    public int seatPosIndex;
    public int selectedProfileIndex;

    public int curFrameIndex;
    public int prevAtlasIndex;

    public bool startFade;
    public bool ticketIsBeingChecked;
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
    public int behaviourFlagCount;


    public bool isSuspected;
    public bool isClicked;
    private void OnEnable()
    {
        gameEventData.OnStationArrival.RegisterListener(PrepareToBoardTrain);

        gameEventData.OnTrainDeceleration.RegisterListener(PrepareToDisembarkTrain);
    }
    private void OnDisable()
    {
        gameEventData.OnTrainDeceleration.UnregisterListener(PrepareToDisembarkTrain);
        gameEventData.OnStationArrival.UnregisterListener(PrepareToBoardTrain);
    }
    private void Start()
    {
        seatPosIndex = int.MaxValue;
        selectedProfileIndex = int.MaxValue;
        atlas = atlasRenderer.atlas;

        rigidBody.includeLayers = layerSettings.stationMask;
        curPath = NPCPath.ToStandAtStation;

        atlasRenderer.UpdateDepthRealtime((int)transform.position.z);
        SetBehaviourFlags();
        smokerRoomIndex = -1; //NOTE: -1 is used as a condition to find a smokers room in the smoker state
        targetAlpha = 1;
    }
    private void Update()
    {
        ChooseStates();
        UpdateStates();
        UpdatePath();
        AdjustAlpha();
        AdjustHover();
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
    }
    private void ChooseStates()
    {
        if (ticketIsBeingChecked)
        {
            SetState(NPCState.TicketCheck);
        }
        else if (Mathf.Abs(targetDist) > CLOSE_TO_TARGET_BUFFER)
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
    private void UpdateStates()
    {
        switch (curState)
        {
            case NPCState.Idling:
            {
                SetIdlePath();
                atlasRenderer.PlayClip(ref curClip);
                behaviourClock += Time.deltaTime;
                if (behaviourClock > stateDuration)
                {
                    PickRandomBehaviour();
                }
            }
            break;
            case NPCState.Walking:
            {
                move = Mathf.Sign(targetDist);
                atlasRenderer.PlayClip(ref curClip);
                atlasRenderer.FlipH(move < 0, atlasRenderer.sprite);

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
                    if (!playingGlyph && curClip.keyFrames[atlasRenderer.curFrameIndex].motionSprite.markers.Length > 0)
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
                rigidBody.linearVelocityX = targetXVelocity;
            }
            break;
        }
    }
    private void SetState(NPCState newState)
    {
        if (curState == newState) return;
        ExitState(newState);
        NPCState prevState = curState;
        curState = newState;
        EnterState(prevState);
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
                SetIdlePath();
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
                AdjustColor(NPCMark.TicketCheck);
                ticketHasBeenChecked = true;

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
                    curBehaviour = 0;
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
    private void EnterPath()
    {
        switch (curPath)
        {
            case NPCPath.SittingInTrain:
            {
                atlasRenderer.UpdateDepthRealtime(trainStats.depthSections.carriageSeat);

            }
            break;
            case NPCPath.StandingInTrain:
            {
                SetStandingDepthInTrain();
            }
            break;
            case NPCPath.AtSlideDoor:
            {
                LayerMask slideDoorLayer = trip.nextStation.isFrontOfTrain ? layerSettings.trainLayers.exteriorSlideDoors : layerSettings.trainLayers.interiorSlideDoors;
                RaycastHit2D slideDoorHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0.0f, transform.right, 0.0f, slideDoorLayer);

                curSlideDoors = slideDoorHit.collider.GetComponent<SlideDoors>();

                if (!onTrain)
                {
                    RaycastHit2D carriageHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0.0f, transform.right, 0.0f, layerSettings.trainLayers.insideCarriageBounds);
                    curCarriage = trainStats.carriageDict[carriageHit.collider];
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
                for (int i = 0; i < trainStats.slideDoorPositions.Length; i++)
                {
                    float dist = Mathf.Abs(trainStats.slideDoorPositions[i] - transform.position.x);

                    if (dist < shortestDist)
                    {
                        shortestDist = dist;
                        selectedSlideDoorPos = trainStats.slideDoorPositions[i];
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

                if (curCarriage.smokersRoomData.Length > 1 && curCarriage.smokersRoomData[1].npcCount < curCarriage.smokersRoomData[0].npcCount) // selected smoker room is based on which room has less npcs
                {
                    smokerRoomIndex = 1;
                }
                else
                {
                    smokerRoomIndex = 0;
                }

                curCarriage.smokersRoomData[smokerRoomIndex].npcCount++;
                SetStandingDepthInTrain();
                targetXPos = UnityEngine.Random.Range(curCarriage.smokersRoomData[smokerRoomIndex].minXPos, curCarriage.smokersRoomData[smokerRoomIndex].maxXPos);
                targetDist = targetXPos - transform.position.x;
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
    private void AdjustAlpha()
    {
        if ((spyStats.curGroundLayer != layerSettings.trainLayers.ground || !boardingStation.isFrontOfTrain) && alphaClock >= ADJUST_COLOR_TIME) return;
        
        if (transform.position.z < spyStats.curWorldPos.z && transform.position.x > spyStats.curLocationBounds.min.x && transform.position.x < spyStats.curLocationBounds.max.x)
        {
            if (alphaClock < ADJUST_COLOR_TIME)
            {
                alphaClock += Time.deltaTime;
                float t = alphaClock / ADJUST_COLOR_TIME;
                atlasRenderer.custom.w = t;
            }
        }
        else
        {
            if (alphaClock > 0)
            {
                alphaClock -= Time.deltaTime;
                float t = alphaClock / ADJUST_COLOR_TIME;
                atlasRenderer.custom.w = t;
            }
        }
    }
    private void AdjustHover()
    {
        if (!onTrain) return;

        if (CursorController.IsInsideBounds(atlasRenderer.bounds))
        {
            if (hoverClock < ADJUST_COLOR_TIME)
            {
                hoverClock += Time.deltaTime;
                float t = hoverClock / ADJUST_COLOR_TIME;

                if (ticketHasBeenChecked && !NPCManager.stationNameTag.text_renderer.hasText)
                {
                    int charCount = Mathf.FloorToInt(t * disembarkingStation.stationName.Length);
                    charCount = Mathf.Clamp(charCount, 0, disembarkingStation.stationName.Length);
                    string curStationNameText = disembarkingStation.stationName.Substring(0, charCount);
                    NPCManager.stationNameTag.SetText(curStationNameText);
                }

                float suspicionT = isSuspected ? (1 - (t * 0.5f)) : t * 0.5f;
                atlasRenderer.custom.y = suspicionT;
            }

            if (!isClicked)
            {
                if (playerInputs.mouseLeftDown)
                {
                    isClicked = true;

                    if (!isSuspected)
                    {
                        isSuspected = true;
                        atlasRenderer.custom.y = 1;
                    }
                    else
                    {
                        isSuspected = false;
                        atlasRenderer.custom.y = 0;
                    }
                }
            }

            NPCManager.stationNameTag.transform.position = new Vector3(atlasRenderer.bounds.center.x, atlasRenderer.bounds.max.y, 0);
        }
        else if (hoverClock > 0)
        {
            hoverClock -= Time.deltaTime;
            float t = hoverClock / ADJUST_COLOR_TIME;

            if (ticketHasBeenChecked)
            {
                int charCount = Mathf.FloorToInt(t * disembarkingStation.stationName.Length);
                charCount = Mathf.Clamp(charCount, 0, disembarkingStation.stationName.Length);

                string curStationNameText = disembarkingStation.stationName.Substring(0, charCount);

                NPCManager.stationNameTag.SetText(curStationNameText);
                NPCManager.stationNameTag.transform.position = new Vector3(atlasRenderer.bounds.center.x, atlasRenderer.bounds.max.y, 0);
            }


            if (!isClicked)
            {
                float suspicionT = isSuspected ? (1 - (t * 0.5f)) : t * 0.5f;
                atlasRenderer.custom.y = suspicionT;
            }
            else
            {
                if (!isSuspected)
                {
                    atlasRenderer.custom.y = 0;
                }
                else
                {
                    atlasRenderer.custom.y = 1;
                }
            }
        }
        else if (isClicked)
        {
            isClicked = false;
        }
    }
    private void AdjustColor(NPCMark mark)
    {
        switch(mark)
        { 
            case NPCMark.TicketCheck:
            {
                AdjustingTicketCheckColor().Forget();
            }
            break;

            case NPCMark.Suspicion:
            {
                AdjustingSuspicionColor().Forget();
            }
            break;
        }
    }
    private async UniTask AdjustingTicketCheckColor()
    {
        float elapsed = 0;

        while (elapsed < ADJUST_COLOR_TIME)
        {
            elapsed += Time.deltaTime;
            await UniTask.Yield();
            atlasRenderer.custom.x = elapsed / ADJUST_COLOR_TIME;
        }

        atlasRenderer.custom.x = 1;
    }
    private async UniTask AdjustingSuspicionColor()
    {
        float elapsed = 0;

        while (elapsed < ADJUST_COLOR_TIME)
        {
            elapsed += Time.deltaTime;
            await UniTask.Yield();
            atlasRenderer.custom.y = elapsed / ADJUST_COLOR_TIME;
        }

        atlasRenderer.custom.y = 1;
    }
    public void BoardTrain()
    {
        transform.SetParent(null, true);
        trainStats.totalPassengersBoarded++;
        QueueForSeat();
        rigidBody.includeLayers = layerSettings.trainMask;
        onTrain = true;
        SetStandingDepthInTrain();
    }
    public void DisembarkTrain()
    {
        Station station = TrainController.nextStation;
        AtlasRenderer stationPlatform = station.station.isFrontOfTrain ? station.frontPlatformRenderer : station.backPlatformRenderer;
        transform.SetParent(stationPlatform.transform, true);
        atlasRenderer.UpdateDepthRealtime(stationPlatform.batchKey.depthOrder);
        rigidBody.includeLayers = layerSettings.stationMask;
        atlasRenderer.UpdateDepthRealtime(stationPlatform.batchKey.depthOrder);
        onTrain = false;
        SetPath(NPCPath.ToExitStation);
    }
    private void PrepareToDisembarkTrain()
    {
        if (!onTrain || trip.nextStation.stationIndex != profile.disembarkingStationIndex) return;
        disembarking = true;
        SetPath(NPCPath.ToSlideDoor);
    }
    public void PrepareToBoardTrain()
    {
        if (onTrain || trip.nextStation.stationIndex != profile.boardingStationIndex) return;
        SetPath(NPCPath.ToSlideDoor);
    }
    private void QueueForSeat()
    {
        curCarriage.AddToSeatQueue(this);
        queuedForSeat = true;
    }
    public void AssignSeat(int seatIndex)
    {
        queuedForSeat = false;
        seatPosIndex = seatIndex;
        targetXPos = curCarriage.seatData.xPos[seatPosIndex];
        SetPath(NPCPath.ToSeatInTrain);
    }
    public void FindStandingPosition()
    {
        SetPath(NPCPath.ToStandInTrain);
    }
    private void SetStandingDepthInTrain()
    {
        if (!onTrain) return;
        int depth = UnityEngine.Random.Range(trainStats.depthSections.frontMin, trainStats.depthSections.backMax);
        atlasRenderer.UpdateDepthRealtime(depth);
    }
    private NPCMotion RandomIdleMotion(NPCMotion motion1, NPCMotion motion2)
    {
        return UnityEngine.Random.Range(0, 2) == 0 ? motion1 : motion2;
    }
    private void SetIdlePath()
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
                SetPath(NPCPath.AtSlideDoor);
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
            case NPCPath.ToStandInTrain:
            {
                NPCMotion standingMotion = RandomIdleMotion(NPCMotion.StandingBlinking, NPCMotion.StandingBreathing);
                SetPath(NPCPath.StandingInTrain);
                curClip = atlas.clipDict[(int)standingMotion];
            }
            break;
        }
    }
    private void PickRandomBehaviour()
    {
        behaviourClock = 0;
        if (!onTrain || disembarking) return;

        curBehaviour = behaviourFlags[UnityEngine.Random.Range(0, behaviourFlagCount)];
        curBehaviourContext = npcData.behaviourContextDict[curBehaviour];

        if (curBehaviourContext.pathToTake != NPCPath.None)
        {
            SetPath(curBehaviourContext.pathToTake);
        }
        behaving = true;
    }
    private void SetBehaviourFlags()
    {
        int behaviourValue = (int)profile.behaviours;
        behaviourFlags = new Behaviours[32];
        behaviourFlagCount = 0;

        for (int i = 0; i < behaviourFlags.Length; i++)
        {
            int flag = 1 << i;
            if ((behaviourValue & flag) != 0)
            {
                behaviourFlags[behaviourFlagCount] = (Behaviours)flag;
                behaviourFlagCount++;
            }
        }
    }
    public Behaviours GetRandomBehaviours(Behaviours npcBehaviours)
    {
        Behaviours[] allBehaviours = (Behaviours[])Enum.GetValues(typeof(Behaviours));

        List<Behaviours> allowedBehaviours = new List<Behaviours>();
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            Behaviours b = allBehaviours[i];
            if (b != 0 && npcBehaviours.HasFlag(b))
            {
                allowedBehaviours.Add(b);
            }
        }

        Behaviours firstBehave = allowedBehaviours[UnityEngine.Random.Range(0, allowedBehaviours.Count)];
        if (allowedBehaviours.Count == 1)
        {
            return firstBehave;
        }

        Behaviours secondBehave;
        do
        {
            secondBehave = allowedBehaviours[UnityEngine.Random.Range(0, allowedBehaviours.Count)];
        }
        while (secondBehave == firstBehave);

        Behaviours behaviours = firstBehave | secondBehave;
        return behaviours;
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (curState == NPCState.Walking)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(boxCollider.bounds.center, new Vector3(targetXPos, boxCollider.bounds.center.y, transform.position.z));
        }

        Vector3 typeLabel = transform.position + atlasRenderer.sprite.worldSize + Vector3.up * 0.2f;
        Vector3 stateLabel = typeLabel + Vector3.up;

        GUIStyle typeStyle = new GUIStyle();
        typeStyle.normal.textColor = role == Role.Traitor ? Color.red : Color.green;
        typeStyle.alignment = TextAnchor.UpperCenter;
        typeStyle.fontSize = 10;

        GUIStyle stateStyle = new GUIStyle();
        stateStyle.normal.textColor = role == Role.Traitor ? Color.red : Color.green;
        stateStyle.alignment = TextAnchor.UpperCenter;
        stateStyle.fontSize = 10;

        // Draw the label in Scene view
        Handles.Label(typeLabel, role.ToString(), typeStyle);
        Handles.Label(stateLabel, curClip.ToString(), stateStyle);

    }
#endif
}
