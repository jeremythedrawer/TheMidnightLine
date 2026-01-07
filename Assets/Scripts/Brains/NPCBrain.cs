using Cysharp.Threading.Tasks;
using System;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.VFX;

public class NPCBrain : MonoBehaviour
{
    public enum State
    {
        Idling,
        Walking,
        Smoking,
        Sleeping,
        Eating,
    }
    
    public enum Type
    {
        Agent,
        Bystander
    }

    public enum Path
    {
        ToSmokerRoom,
        ToChair,
        ToSlideDoor,
    }
    public Rigidbody2D rigidBody;
    public BoxCollider2D boxCollider;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    internal SlideDoors curSlideDoors;
    internal MaterialPropertyBlock mpb;
    internal CancellationTokenSource ctsFade;
        
    internal Carriage curCarriage;
    internal VisualEffect sleepingZs;
    internal GameObject smoke;

    [Serializable] public struct SOData
    {
        public NPCSO npc;
        public NPCsDataSO npcData;
        public LayerSettingsSO layerSettings;
        public TrainSettingsSO trainSettings;
        public TrainStatsSO trainStats;
        public GameEventDataSO gameEventData;
        public PlayerInputsSO spyInputs;
        public SpyStatsSO spyStats;
        public ClipboardStatsSO clipboardStats;

        internal StationSO startStation;
        internal StationSO endStation;
    }
    [SerializeField] public SOData soData;

    [Serializable] public struct StatData
    {
        internal Color selectedColor;

        internal float targetXVelocity;
        internal float targetXPos;
        internal float targetDist;
        internal float stateTimer;
        internal float stateDuration;
        internal float curAlpha;
        internal float targetAlpha;

        internal NPCTraits.Behaviours curBehaviour;
        internal NPCTraits.Behaviours behaviours;
        
        internal State curState;
        internal Path curPath;
        internal Type type;

        internal int smokerRoomIndex;
        internal int chairPosIndex;
        internal int selectedProfileIndex;

        internal bool canBoardTrain;
        internal bool startFade;
    }
    [SerializeField] public StatData stats;

    [Serializable] public struct InputData
    {
        internal float move;
    }
    [SerializeField] InputData inputData;
    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        ctsFade = new CancellationTokenSource();

        mpb.SetFloat(soData.npcData.materialData.zPosID, soData.trainSettings.maxMinWorldZPos.min);
        stats.curState = State.Idling;
        stats.chairPosIndex = -1;
        stats.curAlpha = 1;
        stats.selectedProfileIndex = -1;
        stats.targetXPos = transform.position.x;

        SetLayer().Forget();
        if (stats.behaviours == 0)
        {
            stats.behaviours = NPCTraits.GetBehaviours(soData.npc.behaviours);
        }
    }
    private void OnEnable()
    {
        soData.gameEventData.OnStationArrival.RegisterListener(() => stats.canBoardTrain = true);
        SetAnimationEvents();
    }

    private void Start()
    {
        if (((stats.behaviours & NPCTraits.Behaviours.Takes_naps) != 0) && sleepingZs == null)
        {
            sleepingZs = Instantiate(soData.npcData.sleepingZs, transform);
            sleepingZs.Stop();
        }
        if (((stats.behaviours & NPCTraits.Behaviours.Frequent_smoker) != 0) && smoke == null)
        {
            smoke = Instantiate(soData.npcData.smoke, transform);
            smoke.SetActive(false);
        }
    }
    private async UniTask SetLayer()
    {
        rigidBody.gravityScale = 0;
        while (soData.layerSettings.stationMask == 0) await UniTask.Yield();
        rigidBody.includeLayers = soData.layerSettings.stationMask;
        rigidBody.gravityScale = 1;

    }
    private void OnDisable()
    {
        soData.gameEventData.OnStationArrival.UnregisterListener(() => stats.canBoardTrain = true);
    }
    private void Update()
    {
        SelectingStates();
        UpdateStates();
        Fade();
        mpb.SetTexture(soData.npcData.materialData.mainTexID, spriteRenderer.sprite.texture);
        spriteRenderer.SetPropertyBlock(mpb);



        if (rigidBody.includeLayers == soData.layerSettings.trainMask)
        {
            stats.stateTimer += Time.deltaTime;
        }

        if ((stats.curBehaviour & NPCTraits.Behaviours.Frequent_smoker) != 0 && stats.curPath != Path.ToSmokerRoom)
        {
            FindSmokersRoom();
        }

    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        BoardTrain();
        stats.targetDist = stats.targetXPos - transform.position.x;
        inputData.move = Mathf.Abs(stats.targetDist) > 0.1f ? Mathf.Sign(stats.targetDist) : 0;
        stats.targetXVelocity = soData.npc.moveSpeed  * inputData.move;
        rigidBody.linearVelocityX = soData.npc.moveSpeed  * inputData.move;
    }
    private void OnApplicationQuit()
    {
        ResetData();
    }
    private void SelectingStates()
    {
        if (inputData.move != 0)
        {
            SetState(State.Walking);
        }
        else if (stats.curBehaviour == 0)
        {
            SetState(State.Idling);
        }
        else if ((stats.curBehaviour & NPCTraits.Behaviours.Frequent_smoker) != 0)
        {
            SetState(State.Smoking);
        }
        else if ((stats.curBehaviour & NPCTraits.Behaviours.Takes_naps) != 0)
        {
            SetState(State.Sleeping);
        }
        else if ((stats.curBehaviour & NPCTraits.Behaviours.Always_hungry) != 0)
        {
            SetState(State.Eating);
        }
    }
    private void UpdateStates()
    {
        switch (stats.curState)
        {
            case State.Idling:
            {
                if (stats.stateTimer > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
            case State.Walking:
            {
                spriteRenderer.flipX = inputData.move < 0;
            }
            break;
            case State.Smoking:
            {
                if (stats.stateTimer > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
            case State.Sleeping:
            {
                
                if (stats.stateTimer > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
            case State.Eating:
            {
                if (stats.stateTimer > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;

        }
    }
    private void FixedUpdateStates()
    {
        switch (stats.curState)
        {
            case State.Idling:
            {

            }
            break;
            case State.Walking:
            {
            }
            break;
        }
    }
    private void SetState(State newState)
    {
        if (stats.curState == newState) return;
        ExitState();
        stats.curState = newState;
        stats.stateTimer = 0;
        EnterState();

    }
    private void EnterState()
    {
        switch (stats.curState)
        {
            case State.Idling:
            {
                stats.stateDuration = UnityEngine.Random.Range(soData.npc.pickBehaviourDurationRange.x, soData.npc.pickBehaviourDurationRange.y);

                if (stats.chairPosIndex != -1)
                {
                    animator.Play(soData.npcData.animHashData.sittingBreathing);
                    transform.position = new Vector3(transform.position.x, transform.position.y, curCarriage.chairZPos);
                    mpb.SetFloat(soData.npcData.materialData.zPosID, curCarriage.chairZPos);
                }
                else
                {
                    animator.Play(soData.npcData.animHashData.standingBreathing);
                }
            }
            break;
            case State.Walking:
            {
                animator.Play(soData.npcData.animHashData.walking);
            }
            break;
            case State.Smoking:
            {
                stats.stateDuration = UnityEngine.Random.Range(soData.npc.pickBehaviourDurationRange.x, soData.npc.pickBehaviourDurationRange.y);
                animator.Play(soData.npcData.animHashData.smoking);
                smoke.SetActive(true);
            }
            break;
            case State.Sleeping:
            {
                stats.stateDuration = UnityEngine.Random.Range(soData.npc.pickBehaviourDurationRange.x, soData.npc.pickBehaviourDurationRange.y);
                sleepingZs.transform.position = new Vector3(spriteRenderer.bounds.center.x, spriteRenderer.bounds.max.y, transform.position.z - 0.5f);
                sleepingZs.Reinit();
                sleepingZs.Play();
                if (stats.chairPosIndex != -1)
                {
                    animator.Play(soData.npcData.animHashData.sittingSleeping);
                    transform.position = new Vector3(transform.position.x, transform.position.y, curCarriage.chairZPos);
                    mpb.SetFloat(soData.npcData.materialData.zPosID, curCarriage.chairZPos);
                }
                else
                {
                    animator.Play(soData.npcData.animHashData.standingSleeping);
                }
            }
            break;
            case State.Eating:
            {
                stats.stateDuration = UnityEngine.Random.Range(soData.npc.pickBehaviourDurationRange.x, soData.npc.pickBehaviourDurationRange.y);
                if (stats.chairPosIndex != -1)
                {
                    animator.Play(soData.npcData.animHashData.sittingEating);
                    transform.position = new Vector3(transform.position.x, transform.position.y, curCarriage.chairZPos);
                    mpb.SetFloat(soData.npcData.materialData.zPosID, curCarriage.chairZPos);
                }
                else
                {
                    animator.Play(soData.npcData.animHashData.standingEating);
                }
            }
            break;


        }
    }
    private void ExitState()
    {
        switch (stats.curState)
        {
            case State.Idling:
            {

            }
            break;
            case State.Walking:
            {
            }
            break;
            case State.Smoking:
            {
                smoke.SetActive(false);
                curCarriage.smokersRoomData[stats.smokerRoomIndex].npcCount--;
                QueueForChair();
            }
            break;
            case State.Sleeping:
            {
                sleepingZs.Stop();
            }
            break;
        }

    }
    public void HoverColor()
    {
        if (rigidBody.includeLayers != soData.layerSettings.trainMask) return;

        if (!soData.clipboardStats.tempStats.active || soData.clipboardStats.tempStats.curPageIndex >= soData.clipboardStats.profilePageArray.Length)
        {
            mpb.SetColor(soData.npcData.materialData.colorID, Color.black + new Color(soData.npcData.hoverColorOffet, soData.npcData.hoverColorOffet, soData.npcData.hoverColorOffet, 0f));
        }
        else if (stats.selectedColor != soData.clipboardStats.profilePageArray[soData.clipboardStats.tempStats.curPageIndex].color)
        {
            mpb.SetColor(soData.npcData.materialData.colorID, soData.clipboardStats.profilePageArray[soData.clipboardStats.tempStats.curPageIndex].color * soData.npcData.hoverColorOffet);
        }
        spriteRenderer.SetPropertyBlock(mpb);
    }
    public void SelectColor()
    {
        if (rigidBody.includeLayers != soData.layerSettings.trainMask) return;

        if (!soData.clipboardStats.tempStats.active || soData.clipboardStats.tempStats.curPageIndex >= soData.clipboardStats.profilePageArray.Length)
        {
            stats.selectedColor = Color.black;

            if (stats.selectedProfileIndex == -1) return;
            soData.clipboardStats.profilePageArray[stats.selectedProfileIndex].spySelected = false;
            stats.selectedProfileIndex = -1;
        }
        else if (stats.selectedColor != soData.clipboardStats.profilePageArray[soData.clipboardStats.tempStats.curPageIndex].color)
        {
            stats.selectedProfileIndex = soData.clipboardStats.tempStats.curPageIndex;

            stats.selectedColor = soData.clipboardStats.profilePageArray[stats.selectedProfileIndex].color;
            soData.clipboardStats.profilePageArray[stats.selectedProfileIndex].spySelected = true;
        }
        mpb.SetColor(soData.npcData.materialData.colorID, stats.selectedColor);
        spriteRenderer.SetPropertyBlock(mpb);
    }
    public void ExitColor()
    {
        if (rigidBody.includeLayers != soData.layerSettings.trainMask) return;
        mpb.SetColor(soData.npcData.materialData.colorID, stats.selectedColor);
        spriteRenderer.SetPropertyBlock(mpb);
    }
    private void Fade()
    {
        bool shouldFadeOut =
            rigidBody.includeLayers == soData.layerSettings.stationMask &&
            soData.spyStats.curGroundLayer == soData.layerSettings.trainLayerStruct.ground &&
            soData.spyStats.curWorldPos.z > transform.position.z &&
            transform.position.x > soData.spyStats.curCarriageMinXPos &&
            transform.position.x < soData.spyStats.curCarriageMaxXPos;

        float targetAlpha = shouldFadeOut ? 0f : 1f;

        if (stats.targetAlpha == targetAlpha) return;
        stats.targetAlpha = targetAlpha;;
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();
        FadeTo(ctsFade.Token).Forget();
    }
    private async UniTask FadeTo(CancellationToken token)
    {
        float startAlpha = stats.curAlpha;
        float elapsed = 0f;
        try
        {
            while (elapsed < soData.npcData.fadeTime)
            {
                token.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = elapsed / soData.npcData.fadeTime;

                stats.curAlpha = Mathf.Lerp(startAlpha, stats.targetAlpha, t);
                mpb.SetFloat( soData.npcData.materialData.alphaID, stats.curAlpha);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            stats.curAlpha = stats.targetAlpha;
            mpb.SetFloat(soData.npcData.materialData.alphaID, stats.curAlpha);
        }
        catch (OperationCanceledException)
        {
        }
    }
    private void BoardTrain()
    {
        if (!stats.canBoardTrain || rigidBody.includeLayers == soData.layerSettings.trainMask) return; // only board train when you can board and npc is on the station ground

        if (curCarriage == null) // find slide door in one frame
        {
            RaycastHit2D carriageHit = Physics2D.BoxCast(boxCollider.bounds.center, new Vector2(soData.npc.maxDistanceDetection, boxCollider.size.y), 0.0f, transform.right, soData.npc.maxDistanceDetection, soData.layerSettings.trainLayerStruct.carriage);

            if (carriageHit.collider == null) { Debug.LogError($"{name} did not find a carriage to go to"); return; }

            curCarriage = carriageHit.collider.GetComponent<Carriage>();
        }
        else if (curSlideDoors == null)
        {
            float clostestSlideDoorDists = float.MaxValue;
            float npcXPos = transform.position.x;
            int bestIndex = -1;
            if (soData.startStation.isFrontOfTrain)
            {
                for (int i = 0; i < curCarriage.exteriorSlideDoors.Length; i++)
                {
                    float dist = Mathf.Abs(npcXPos - curCarriage.exteriorSlideDoors[i].transform.position.x);
                    if (dist < clostestSlideDoorDists)
                    {
                        clostestSlideDoorDists = dist;
                        bestIndex = i;
                    }
                }
                if (bestIndex !=  -1)
                {
                    curSlideDoors = curCarriage.exteriorSlideDoors[bestIndex];
                }
            }
            else
            {
                for (int i = 0; i < curCarriage.interiorSlideDoors.Length; i++)
                {
                    float dist = Mathf.Abs(npcXPos - curCarriage.interiorSlideDoors[i].transform.position.x);
                    if (dist < clostestSlideDoorDists)
                    {
                        clostestSlideDoorDists = dist;
                        bestIndex = i;
                    }
                }
                if (bestIndex != -1)
                {
                    curSlideDoors = curCarriage.interiorSlideDoors[bestIndex];
                }
            }

            if (curSlideDoors == null)
            {
                Debug.LogError("No slide door found");
                return;
            }

            stats.curPath = Path.ToSlideDoor;
            stats.targetXPos = curSlideDoors.transform.position.x;
        }
        else if (inputData.move == 0.0f)
        {

            if (curSlideDoors.stats.curState == SlideDoors.State.Unlocked)
            {
                curSlideDoors.OpenDoors();
            }
            else if (curSlideDoors.stats.curState == SlideDoors.State.Opened) // enter train when slide door is opened
            {
                SetStandingDepthAndPosition();
                transform.SetParent(null, true);
                soData.trainStats.curPassengerCount++;
                QueueForChair();
                rigidBody.includeLayers = soData.layerSettings.trainMask;
            }
        }
    }
    private void QueueForChair()
    {
        if (!NPCManager.npcChairList.Contains(this))
        {
            NPCManager.npcChairList.Add(this);
        }
    }
    public void AssignChair(int chairIndex)
    {
        stats.chairPosIndex = chairIndex;
        curCarriage.chairData[stats.chairPosIndex].filled = true;
        stats.curPath = Path.ToChair;
        stats.targetXPos = curCarriage.chairData[stats.chairPosIndex].xPos;


    }
    public void FindStandingPosition()
    {
        stats.targetXPos = UnityEngine.Random.Range(curCarriage.insideBoundsCollider.bounds.min.x, curCarriage.insideBoundsCollider.bounds.max.x);
    }
    private void FindSmokersRoom()
    {
        if (curCarriage.chairData[stats.chairPosIndex].filled)
        {
            curCarriage.chairData[stats.chairPosIndex].filled = false;
            stats.chairPosIndex = -1;

        }
        if (NPCManager.npcChairList.Contains(this)) NPCManager.npcChairList.Remove(this); // To prevent them from going back to the chair if they are queued

        if (curCarriage.smokersRoomData.Length > 1 && curCarriage.smokersRoomData[1].npcCount < curCarriage.smokersRoomData[0].npcCount) // selected smoker room is based on which room has less npcs
        {
            stats.smokerRoomIndex = 1;
        }
        else
        {
            stats.smokerRoomIndex = 0;
        }
        curCarriage.smokersRoomData[stats.smokerRoomIndex].npcCount++;
        SetStandingDepthAndPosition();
        stats.curPath = Path.ToSmokerRoom;
        stats.targetXPos = UnityEngine.Random.Range(curCarriage.smokersRoomData[stats.smokerRoomIndex].minXPos, curCarriage.smokersRoomData[stats.smokerRoomIndex].maxXPos);

    }
    private void SetStandingDepthAndPosition()
    {
        float zPos = UnityEngine.Random.Range(soData.trainSettings.maxMinWorldZPos.min, soData.trainSettings.maxMinWorldZPos.max);
        mpb.SetFloat(soData.npcData.materialData.zPosID, zPos);
        transform.position = new Vector3(transform.position.x, transform.position.y, zPos);
    }

    //public void SetStationDepth()
    //{
    //    componentData.mpb.SetFloat(soData.npcData.materialData.zPosID, transform.position.z); //TODO Lighten NPC when they are on a station that is behind the train
    //}
    private void SetAnimationEvents()
    {
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.sittingAboutToEat], nameof(PlaySittingEating));
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.standingAboutToEat], nameof(PlayStandingEatingAnimation));
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.standingBreathing], nameof(PlayRandomStandingIdleAnimations));
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.standingBlinking], nameof(PlayRandomStandingIdleAnimations));
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.sittingBreathing], nameof(PlayRandomSittingIdleAnimations));
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.sittingBlinking], nameof(PlayRandomSittingIdleAnimations));

        for (int i = 0; i < soData.npc.smokeAnimPosData.Length; i++)
        {
            Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.smoking], nameof(SetSmokePosition), soData.npc.smokeAnimPosData[i].time, intParam: i);
        }
    }
    private void PlaySittingEating()
    {
        animator.Play(soData.npcData.animHashData.sittingEating);
    }
    private void PlayStandingEatingAnimation()
    {
        animator.Play(soData.npcData.animHashData.standingEating);
    }
    private void PlayRandomStandingIdleAnimations()
    {
        if(UnityEngine.Random.Range(0, 2) == 0)
        {
            animator.Play(soData.npcData.animHashData.standingBlinking);
        }
        else
        {
            animator.Play(soData.npcData.animHashData.standingBreathing);
        }
    }
    private void PlayRandomSittingIdleAnimations()
    {
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            animator.Play(soData.npcData.animHashData.sittingBlinking);
        }
        else
        {
            animator.Play(soData.npcData.animHashData.sittingBreathing);
        }
    }
    private void SetSmokePosition(int index)
    {
        float xPos = spriteRenderer.flipX ? -soData.npc.smokeAnimPosData[index].position.x : soData.npc.smokeAnimPosData[index].position.x;
        smoke.transform.localPosition = new Vector3(xPos, soData.npc.smokeAnimPosData[index].position.y, 0);
    }
    private NPCTraits.Behaviours PickBehaviour()
    {
        int behaviourValue = (int)stats.behaviours;
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

        return (NPCTraits.Behaviours)chosenFlag;
    }
    private void ResetData()
    {
        mpb.SetFloat(soData.npcData.materialData.zPosID, 0f);
        spriteRenderer.SetPropertyBlock(mpb);
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(boxCollider.bounds.center, new Vector2(stats.targetXPos, boxCollider.bounds.center.y));

        Vector3 typeLabel = spriteRenderer.bounds.max + Vector3.up * 0.2f;
        Vector3 stateLabel = typeLabel + Vector3.up;

        GUIStyle typeStyle = new GUIStyle();
        typeStyle.normal.textColor = stats.type == Type.Agent ? Color.red : Color.green;
        typeStyle.alignment = TextAnchor.UpperCenter;
        typeStyle.fontSize = 10;

        GUIStyle stateStyle = new GUIStyle();
        stateStyle.normal.textColor = stats.type == Type.Agent ? Color.red : Color.green;
        stateStyle.alignment = TextAnchor.UpperCenter;
        stateStyle.fontSize = 10;

        // Draw the label in Scene view
        Handles.Label(typeLabel, stats.type.ToString(), typeStyle);
        Handles.Label(stateLabel, animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.ToString(), stateStyle);

    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(boxCollider.bounds.center, new Vector2(soData.npc.maxDistanceDetection, boxCollider.bounds.extents.y));
    }
#endif

}
