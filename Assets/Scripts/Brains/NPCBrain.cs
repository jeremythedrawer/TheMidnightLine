using Cysharp.Threading.Tasks;
using System;
using System.Threading;
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
    public SlideDoors curSlideDoors;

    public CancellationTokenSource ctsFade;
    public CancellationTokenSource ctsBoardTrain;
    public UniTaskCompletionSource tcsGetToSlideDoor;
    
    public Carriage curCarriage;

    public StationSO startStation;
    public StationSO endStation;

    public Vector2 curSpriteMarkerLocalPosition;
    public float targetXVelocity;
    public float targetXPos;
    public float targetDist;
    public float behaviourClock;
    public float stateDuration;
    public float targetAlpha;
    public float atlasIndexClock;
    public float move;

    public Behaviours curBehaviour;

    public NPCProfile profile;
        
    public NPCState curState;
    public Path curPath;
    public Role role;
    public int smokerRoomIndex;
    public int seatPosIndex;
    public int selectedProfileIndex;

    public int curFrameIndex;
    public int prevAtlasIndex;

    public bool startFade;
    public bool ticketIsBeingChecked;

    public bool queuedForSeat;

    public bool ticketChecked;

    public int seatQueueIndex;
    public int enterTrainIndex;
    public int exitTrainIndex;

    public AtlasClip curClip;

    private VisualEffect sleepingZs;
    private VisualEffect smoke;
    private VisualEffect speechBubble;
    private VisualEffect musicNotes;

    private void Awake()
    {
        ctsFade = new CancellationTokenSource();
        seatPosIndex = int.MaxValue;
        selectedProfileIndex = int.MaxValue;
        atlas = atlasRenderer.atlas;
        rigidBody.includeLayers = layerSettings.stationMask;
    }
    private void Start()
    {
        Vector3 markerPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z - 0.5f);
        if (((profile.behaviours & Behaviours.Takes_naps) != 0) && sleepingZs == null)
        {
            sleepingZs = Instantiate(npcData.sleepingZs_prefab, markerPosition, transform.rotation, transform);
            sleepingZs.Stop();
        }
        if (((profile.behaviours & Behaviours.smoke_Addict) != 0) && smoke == null)
        {
            smoke = Instantiate(npcData.smoke_prefab, markerPosition, transform.rotation, transform);
            smoke.Stop();
        }
        if (((profile.behaviours & Behaviours.Listens_to_music) != 0) && musicNotes == null)
        {
            musicNotes = Instantiate(npcData.musicNotes_prefab, markerPosition, transform.rotation, transform);
            musicNotes.Stop();
        }
        if (((profile.behaviours & Behaviours.Always_on_call) != 0) && speechBubble == null)
        {
            speechBubble = Instantiate(npcData.speechBubble_prefab, markerPosition, transform.rotation, transform);
            speechBubble.Stop();
        }

        atlasRenderer.UpdateDepthRealtime((int)transform.position.z);
        QueueToEnterTrain();
    }
    private void Update()
    {
        ChooseStates();
        UpdateStates();

        UpdatePath();

        Fade();


        if (rigidBody.includeLayers == layerSettings.trainMask)
        {
            behaviourClock += Time.deltaTime;
        }

        move = Mathf.Sign(targetDist);
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
        else if (curBehaviour == 0)
        {
            SetState(NPCState.Idling);
        }
        else if ((curBehaviour & Behaviours.smoke_Addict) != 0)
        {
            SetState(NPCState.Smoking);
        }
        else if ((curBehaviour & Behaviours.Takes_naps) != 0)
        {
            SetState(NPCState.Sleeping);
        }
        else if ((curBehaviour & Behaviours.Always_hungry) != 0)
        {
            SetState(NPCState.Eating);
        }
        else if ((curBehaviour & Behaviours.Listens_to_music) != 0)
        {
            SetState(NPCState.Music);
        }
        else if ((curBehaviour & Behaviours.Always_on_call) != 0)
        {
            SetState(NPCState.Calling);
        }
        else if ((curBehaviour & Behaviours.Enjoys_reading) != 0)
        {
            SetState(NPCState.Reading);
        }
    }
    private void UpdateStates()
    {
        switch (curState)
        {
            case NPCState.Idling:
            {
                atlasRenderer.PlayClip(ref curClip);
                if (behaviourClock > stateDuration)
                {
                    curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Walking:
            {
                atlasRenderer.PlayClip(ref curClip);
                atlasRenderer.FlipH(move < 0, atlasRenderer.sprite);
            }
            break;
            case NPCState.TicketCheck:
            {
                atlasRenderer.PlayClip(ref curClip);
                if (!ticketIsBeingChecked)
                {
                    curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Smoking:
            {
                atlasRenderer.PlayClip(ref curClip, smoke.transform);
                if (behaviourClock > stateDuration)
                {
                    curBehaviour = PickBehaviour();
                }

            }
            break;
            case NPCState.Sleeping:
            {
                atlasRenderer.PlayClip(ref curClip, sleepingZs.transform);
                if (behaviourClock > stateDuration)
                {
                    curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Eating:
            {
                atlasRenderer.PlayClip(ref curClip);
                if (behaviourClock  > stateDuration)
                {
                    curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Music:
            {
                atlasRenderer.PlayClip(ref curClip, musicNotes.transform);
                if (behaviourClock > stateDuration)
                {
                    curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Calling:
            {
                atlasRenderer.PlayClip(ref curClip, speechBubble.transform);
                if (behaviourClock > stateDuration)
                {
                    curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Reading:
            {
                atlasRenderer.PlayClip(ref curClip);
                if (behaviourClock > stateDuration)
                {
                    curBehaviour = PickBehaviour();
                }
            }
            break;
        }
    }
    private void FixedUpdateStates()
    {
        switch (curState)
        {
            case NPCState.Idling:
            {

            }
            break;
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
        ExitState();
        curState = newState;
        behaviourClock = 0;
        EnterState();
    }
    private void EnterState()
    {
        atlasIndexClock = 0;
        curFrameIndex = 0;

        switch (curState)
        {
            case NPCState.Idling:
            {
                stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);

                NPCMotion sittingMotion = RandomIdleMotion(NPCMotion.SittingBlinking, NPCMotion.SittingBreathing);
                NPCMotion standingMotion = RandomIdleMotion(NPCMotion.StandingBlinking, NPCMotion.StandingBreathing);
                SetPathMotion(sittingMotion, standingMotion);
            }
            break;
            case NPCState.Walking:
            {
                curClip = atlasRenderer.atlas.clipDict[(int)NPCMotion.Walking];
            }
            break;
            case NPCState.TicketCheck:
            {
                NPCMotion sittingMotion = RandomIdleMotion(NPCMotion.SittingBlinking, NPCMotion.SittingBreathing);
                NPCMotion standingMotion = RandomIdleMotion(NPCMotion.StandingBlinking, NPCMotion.StandingBreathing);
                SetPathMotion(sittingMotion, standingMotion);

                atlasRenderer.custom.y = 1;

                if (!ticketChecked)
                {
                    ticketChecked = true;
                    trip.ticketsCheckedSinceStart++;
                    trip.ticketsCheckedSinceLastStation++;
                }
            }
            break;
            case NPCState.Smoking:
            {
                stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                curClip = atlasRenderer.atlas.clipDict[(int)NPCMotion.Smoking];
                smoke.SetFloat("_Lifetime", stateDuration);
                smoke.Play();

                SetStandingDepthInTrain();
            }
            break;
            case NPCState.Sleeping:
            {
                stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                sleepingZs.Play();
                SetPathMotion(NPCMotion.SittingSleeping, NPCMotion.StandingSleeping);
            }
            break;
            case NPCState.Eating:
            {
                stateDuration = UnityEngine.Random.Range(
                npc.pickBehaviourDurationRange.x, 
                npc.pickBehaviourDurationRange.y);
                SetPathMotion(NPCMotion.SittingEating, NPCMotion.StandingEating);
            }
            break;
            case NPCState.Music:
            {
                stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                musicNotes.Play();
                SetPathMotion(NPCMotion.SittingMusic, NPCMotion.StandingMusic);
            }
            break;
            case NPCState.Calling:
            {
                stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                speechBubble.Play();
                SetPathMotion(NPCMotion.SittingCalling, NPCMotion.StandingCalling);
            }
            break;
            case NPCState.Reading:
            {
                stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                SetPathMotion(NPCMotion.SittingReading, NPCMotion.StandingReading);
            }
            break;
        }
    }
    private void ExitState()
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

                switch (curPath)
                {
                    case Path.ToSlideDoor:
                    case Path.ToSmokerRoom:
                    {
                        SetPath(Path.Standing);
                    }
                    break;

                    case Path.ToSeat:
                    {
                        SetPath(Path.Sitting);
                    }
                    break;
                }

            }
            break;
            case NPCState.Smoking:
            {
                smoke.Stop();
                curCarriage.smokersRoomData[smokerRoomIndex].npcCount--;
                QueueForChair();
            }
            break;
            case NPCState.Sleeping:
            {
                sleepingZs.Stop();
            }
            break;
            case NPCState.Music:
            {
                musicNotes.Stop();
            }
            break;
            case NPCState.Calling:
            {
                speechBubble.Stop();
            }
            break;
        }

    }
    private void SetPath(Path newPath)
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
            case Path.Sitting:
            {

            }
            break;

            case Path.Standing:
            {

            }
            break;

            case Path.ToSeat:
            {
                targetDist = targetXPos - transform.position.x;
            }
            break;

            case Path.ToSlideDoor:
            {
                targetDist = targetXPos - transform.position.x;
            }
            break;

            case Path.ToSmokerRoom:
            {
                targetDist = targetXPos - transform.position.x;
            }
            break;  

        }

    }
    private void EnterPath()
    {
        switch (curPath)
        {
            case Path.Sitting:
            {
                targetDist = 0;
            }
            break;

            case Path.Standing:
            {
                targetDist = 0;
            }
            break;

            case Path.ToSeat:
            {
            }
            break;

            case Path.ToSlideDoor:
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
            }
            break;

            case Path.ToSmokerRoom:
            {
            }
            break;
        }
    }
    private void ExitPath()
    {
        switch (curPath)
        {
            case Path.Sitting:
            {

            }
            break;

            case Path.Standing:
            {

            }
            break;

            case Path.ToSeat:
            {
            }
            break;

            case Path.ToSlideDoor:
            {
                tcsGetToSlideDoor?.TrySetResult();
                tcsGetToSlideDoor = null;
            }
            break;

            case Path.ToSmokerRoom:
            {
            }
            break;

        }
    }
    private void SetPathMotion(NPCMotion sittingMotion, NPCMotion standingMotion)
    {
        switch (curPath)
        {
            case Path.Sitting:
            case Path.ToSeat:
            {
                curClip = atlas.clipDict[(int)sittingMotion];
                atlasRenderer.UpdateDepthRealtime(trainStats.depthSections.carriageSeat);
            }
            break;

            case Path.ToStand:
            case Path.Standing:
            {
                curClip = atlas.clipDict[(int)standingMotion];
                SetStandingDepthInTrain();
            }
            break;

        }
    }
    private void Fade()
    {
        bool shouldFadeOut =
            rigidBody.includeLayers == layerSettings.stationMask &&
            spyStats.curGroundLayer == layerSettings.trainLayers.ground &&
            spyStats.curWorldPos.z > transform.position.z &&
            transform.position.x > spyStats.curLocationBounds.min.x &&
            transform.position.x < spyStats.curLocationBounds.max.x;

        float targAlpha = shouldFadeOut ? 0f : 1f;

        if (targetAlpha == targAlpha) return;
        
        targetAlpha = targAlpha;
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();
        FadeTo().Forget();
    }
    private async UniTask FadeTo()
    {
        float startAlpha = atlasRenderer.custom.x;
        float elapsed = 0f;
        try
        {
            while (elapsed < npcData.fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / npcData.fadeTime;

                atlasRenderer.custom.x = Mathf.Lerp(startAlpha, targetAlpha, t);

                await UniTask.Yield(PlayerLoopTiming.Update, ctsFade.Token);
            }

            atlasRenderer.custom.x = targetAlpha;
        }
        catch (OperationCanceledException)
        {
        }
    }
    public void QueueToEnterTrain()
    {
        NPCManager.AddToEnterTrainQueue(this);
    }
    public void BoardTrain()
    {
        ctsBoardTrain = new CancellationTokenSource();
        BoardingTrain().Forget();
    }
    private async UniTask BoardingTrain()
    {
        float randomStartMoveTime = UnityEngine.Random.Range(0.3f, 1f);
        await UniTask.WaitForSeconds(randomStartMoveTime);

        SetPath(Path.ToSlideDoor);
        while(curPath != Path.ToSlideDoor) await UniTask.Yield();

        await WaitUntilCloseToSlideDoor();

        RaycastHit2D slideDoorHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0.0f, transform.right, 0.0f, layerSettings.trainLayers.slideDoors);
        RaycastHit2D carriageHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0.0f, transform.right, 0.0f, layerSettings.trainLayers.insideCarriageBounds);

#if UNITY_EDITOR
        if (slideDoorHit.collider == null) { Debug.LogError($"{profile.fullName} did not find a slide door to go to"); return; }
        if (carriageHit.collider == null) { Debug.LogError($"{profile.fullName} did not find a slide door to go to"); return; }
#endif
        curSlideDoors = slideDoorHit.collider.GetComponent<SlideDoors>();

        await curSlideDoors.WaitUntilUnlocked();
        
        if (curSlideDoors.curState == SlideDoors.State.Unlocked) curSlideDoors.OpenDoors();

        await curSlideDoors.WaitUntilOpened();

        transform.SetParent(null, true);
        trainStats.curPassengersBoarded++;
        QueueForChair();

        curCarriage = trainStats.carriageDict[carriageHit.collider];
        rigidBody.includeLayers = layerSettings.trainMask;
        SetStandingDepthInTrain();
    }
    private UniTask WaitUntilCloseToSlideDoor()
    {
        if (curPath == Path.Standing || Mathf.Abs(targetDist) <= CLOSE_TO_TARGET_BUFFER)
        {
            return UniTask.CompletedTask;
        }

        if (tcsGetToSlideDoor == null) tcsGetToSlideDoor = new UniTaskCompletionSource();

        return tcsGetToSlideDoor.Task;
    }
    private void FindSlideDoor()
    {


    }
    private void QueueForChair()
    {
        NPCManager.AddToSeatQueue(this);
        queuedForSeat = true;
    }
    public void AssignSeat(int chairIndex)
    {
        queuedForSeat = false;
        seatPosIndex = chairIndex;
        curCarriage.seatData.filled[seatPosIndex] = true;
        SetPath(Path.ToSeat);
        targetXPos = curCarriage.seatData.xPos[seatPosIndex];
    }
    public void FindStandingPosition()
    {
        targetXPos = UnityEngine.Random.Range(curCarriage.insideBoundsCollider.bounds.min.x, curCarriage.insideBoundsCollider.bounds.max.x);
        SetPath(Path.ToStand);
    }
    private void FindSmokersRoom()
    {
        if (seatPosIndex != int.MaxValue && curCarriage.seatData.filled[seatPosIndex])
        {
            curCarriage.seatData.filled[seatPosIndex] = false;
            seatPosIndex = int.MaxValue;
        }
        if (queuedForSeat) NPCManager.RemoveFromSeatQueue(this); // To prevent them from going back to the chair if they are queued

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
        SetPath(Path.ToSmokerRoom);
    }
    private void SetStandingDepthInTrain()
    {
        if (rigidBody.includeLayers != layerSettings.trainMask) return;
        int depth = UnityEngine.Random.Range(trainStats.depthSections.frontMin, trainStats.depthSections.backMax);
        atlasRenderer.UpdateDepthRealtime(depth);
    }
    private NPCMotion RandomIdleMotion(NPCMotion motion1, NPCMotion motion2)
    {
        return UnityEngine.Random.Range(0, 2) == 0 ? motion1 : motion2;
    }
    private Behaviours PickBehaviour()
    {
        int behaviourValue = (int)profile.behaviours;
        int[] flags = new int[32];
        int flagCount = 1;

        for (int i = 0; i < flags.Length; i++)
        {
            int flag = 1 << i;
            if ((behaviourValue & flag) != 0)
            {
                flags[flagCount] = flag;
                flagCount++;
            }
        }
        int chosenFlag = flags[UnityEngine.Random.Range(0, flagCount)];

        Behaviours chosenBehaviour = (Behaviours)chosenFlag;

        if ((chosenBehaviour & Behaviours.smoke_Addict) != 0)
        {
            FindSmokersRoom();
        }

        return (Behaviours)chosenFlag;
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
    public bool IsOnTrain()
    {
        return rigidBody.includeLayers == layerSettings.trainMask;
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (curState == NPCState.Walking)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(boxCollider.bounds.center, new Vector2(targetXPos, boxCollider.bounds.center.y));
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
