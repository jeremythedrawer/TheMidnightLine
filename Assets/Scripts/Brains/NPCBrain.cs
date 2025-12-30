using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEditor;
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
    [Serializable] public struct ComponentData
    {
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
    }
    public ComponentData componentData;

    [Serializable] public struct SOData
    {
        public NPCSO npc;
        public NPCsDataSO npcData;
        public LayerSettingsSO layerSettings;
        public TrainSettingsSO trainSettings;
        public TrainStatsSO trainStats;
        public GameEventDataSO gameEventData;
        public SpyInputsSO spyInputs;
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
        internal float curRunSpeed;
        internal float targetXPos;
        internal float targetDist;
        internal float stateTimer;
        internal float stateDuration;
        internal float alpha;

        internal NPCTraits.Behaviours curBehaviour;
        internal NPCTraits.Behaviours behaviours;
        
        internal State curState;
        internal Path curPath;
        internal Type type;

        internal int smokerRoomIndex;
        internal int chairPosIndex;
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
        componentData.mpb = new MaterialPropertyBlock();
        componentData.ctsFade = new CancellationTokenSource();
    }
    private void OnEnable()
    {
        soData.gameEventData.OnStationArrival.RegisterListener(() => stats.canBoardTrain = true);

        if (stats.behaviours == 0)
        {
            stats.behaviours = NPCTraits.GetBehaviours();
        }
        if (((stats.behaviours & NPCTraits.Behaviours.Takes_naps) != 0) && componentData.sleepingZs == null)
        {
            componentData.sleepingZs = Instantiate(soData.npcData.sleepingZs, transform);
            componentData.sleepingZs.Stop();
        }
        if (((stats.behaviours & NPCTraits.Behaviours.Frequent_smoker) != 0) && componentData.smoke == null)
        {
            componentData.smoke = Instantiate(soData.npcData.smoke, transform);
            componentData.smoke.SetActive(false);
        }

    }
    private void OnDisable()
    {
        soData.gameEventData.OnStationArrival.UnregisterListener(() => stats.canBoardTrain = true);
    }
    private void Start()
    {
        SetAnimationEvents();
        stats.curState = State.Idling;
        componentData.rigidBody.includeLayers = soData.layerSettings.stationLayersStruct.ground;
        stats.curRunSpeed = 1.0f;

        componentData.mpb.SetFloat(soData.npcData.materialData.zPosID, soData.trainSettings.maxMinWorldZPos.min);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

        stats.chairPosIndex = -1;
        stats.targetXPos = transform.position.x;
        stats.alpha = 1;
    }
    private void Update()
    {
        SelectingStates();
        UpdateStates();
        Fade();
        componentData.mpb.SetTexture(soData.npcData.materialData.mainTexID, componentData.spriteRenderer.sprite.texture);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

        stats.targetDist = stats.targetXPos - transform.position.x;
        inputData.move = Mathf.Abs(stats.targetDist) > 0.1f ? Mathf.Sign(stats.targetDist) : 0;

        if (componentData.rigidBody.includeLayers == soData.layerSettings.trainMask)
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

        stats.targetXVelocity = soData.npc.moveSpeed * stats.curRunSpeed * inputData.move;
        componentData.rigidBody.linearVelocityX = Mathf.Lerp(componentData.rigidBody.linearVelocityX, stats.targetXVelocity, soData.npc.groundAccelation * Time.fixedDeltaTime);
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
                componentData.spriteRenderer.flipX = inputData.move < 0;
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
                componentData.sleepingZs.transform.position = new Vector3(componentData.spriteRenderer.bounds.center.x, componentData.spriteRenderer.bounds.max.y, transform.position.z);
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
                    componentData.animator.Play(soData.npcData.animHashData.sittingBreathing);
                }
                else
                {
                    componentData.animator.Play(soData.npcData.animHashData.standingBreathing);
                }
            }
            break;
            case State.Walking:
            {
                componentData.animator.Play(soData.npcData.animHashData.walking);
            }
            break;
            case State.Smoking:
            {
                stats.stateDuration = UnityEngine.Random.Range(soData.npc.pickBehaviourDurationRange.x, soData.npc.pickBehaviourDurationRange.y);
                componentData.animator.Play(soData.npcData.animHashData.smoking);
                componentData.smoke.SetActive(true);
            }
            break;
            case State.Sleeping:
            {
                stats.stateDuration = UnityEngine.Random.Range(soData.npc.pickBehaviourDurationRange.x, soData.npc.pickBehaviourDurationRange.y);
                componentData.sleepingZs.Play();
                if (stats.chairPosIndex != -1)
                {
                    componentData.animator.Play(soData.npcData.animHashData.sittingSleeping);
                }
                else
                {
                    componentData.animator.Play(soData.npcData.animHashData.standingSleeping);
                }
            }
            break;
            case State.Eating:
            {
                stats.stateDuration = UnityEngine.Random.Range(soData.npc.pickBehaviourDurationRange.x, soData.npc.pickBehaviourDurationRange.y);
                if (stats.chairPosIndex != -1)
                {
                    componentData.animator.Play(soData.npcData.animHashData.sittingEating);
                }
                else
                {
                    componentData.animator.Play(soData.npcData.animHashData.standingEating);
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
                componentData.smoke.SetActive(false);
                componentData.curCarriage.smokersRoomData[stats.smokerRoomIndex].npcCount--;
                QueueForChair();
            }
            break;
            case State.Sleeping:
            {
                componentData.sleepingZs.Stop();
            }
            break;
        }

    }
    public void HoverColor()
    {
        if (componentData.rigidBody.includeLayers != soData.layerSettings.trainMask) return;

        if (!soData.clipboardStats.active || soData.clipboardStats.activePageIndex >= soData.clipboardStats.profilePageArray.Length)
        {
            componentData.mpb.SetColor(soData.npcData.materialData.colorID, Color.black + new Color(soData.npcData.hoverColorOffet, soData.npcData.hoverColorOffet, soData.npcData.hoverColorOffet, 0f));
        }
        else if (stats.selectedColor != soData.clipboardStats.profilePageArray[soData.clipboardStats.activePageIndex].color)
        {
            componentData.mpb.SetColor(soData.npcData.materialData.colorID, soData.clipboardStats.profilePageArray[soData.clipboardStats.activePageIndex].color * soData.npcData.hoverColorOffet);
        }
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);
    }
    public void SelectColor()
    {
        if (componentData.rigidBody.includeLayers != soData.layerSettings.trainMask) return;

        if (!soData.clipboardStats.active || soData.clipboardStats.activePageIndex >= soData.clipboardStats.profilePageArray.Length)
        {
            stats.selectedColor = Color.black;
        }
        else if (stats.selectedColor != soData.clipboardStats.profilePageArray[soData.clipboardStats.activePageIndex].color)
        {
            stats.selectedColor = soData.clipboardStats.profilePageArray[soData.clipboardStats.activePageIndex].color;
        }
        componentData.mpb.SetColor(soData.npcData.materialData.colorID, stats.selectedColor);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);
    }
    public void ExitColor()
    {
        if (componentData.rigidBody.includeLayers != soData.layerSettings.trainMask) return;
        componentData.mpb.SetColor(soData.npcData.materialData.colorID, stats.selectedColor);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);
    }
    private void Fade()
    {
        bool shouldFadeOut =
            componentData.rigidBody.includeLayers == soData.layerSettings.stationMask &&
            soData.spyStats.curGroundLayer == soData.layerSettings.trainLayerStruct.ground &&
            soData.spyStats.curWorldPos.z > transform.position.z &&
            transform.position.x > soData.spyStats.curCarriageMinXPos &&
            transform.position.x < soData.spyStats.curCarriageMaxXPos;

        float targetAlpha = shouldFadeOut ? 0f : 1f;

        if (Mathf.Approximately(stats.alpha, targetAlpha)) return;

        componentData.ctsFade?.Cancel();
        componentData.ctsFade?.Dispose();

        componentData.ctsFade = new CancellationTokenSource();
        FadeTo(targetAlpha, componentData.ctsFade.Token).Forget();
    }
    private async UniTask FadeTo(float targetAlpha, CancellationToken token)
    {
        float startAlpha = stats.alpha;
        float elapsed = 0f;
        try
        {
            while (elapsed < soData.npcData.fadeTime)
            {
                token.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = elapsed / soData.npcData.fadeTime;

                stats.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                componentData.mpb.SetFloat( soData.npcData.materialData.alphaID, stats.alpha);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            stats.alpha = targetAlpha;
            componentData.mpb.SetFloat(soData.npcData.materialData.alphaID, stats.alpha);
        }
        catch (OperationCanceledException)
        {
        }
    }
    private void BoardTrain()
    {
        if (!stats.canBoardTrain || componentData.rigidBody.includeLayers == soData.layerSettings.trainMask) return; // only board train when you can board and npc is on the station ground

        if (componentData.curCarriage == null) // find slide door in one frame
        {
            RaycastHit2D carriageHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, new Vector2(soData.npc.maxDistanceDetection, componentData.boxCollider.size.y), 0.0f, transform.right, soData.npc.maxDistanceDetection, soData.layerSettings.trainLayerStruct.carriage);

            if (carriageHit.collider == null) { Debug.LogError($"{name} did not find a carriage to go to"); return; }

            componentData.curCarriage = carriageHit.collider.GetComponent<Carriage>();
        }
        else if (componentData.curSlideDoors == null)
        {
            float clostestSlideDoorDists = float.MaxValue;
            float npcXPos = transform.position.x;
            int bestIndex = -1;
            if (soData.startStation.isFrontOfTrain)
            {
                for (int i = 0; i < componentData.curCarriage.exteriorSlideDoors.Length; i++)
                {
                    float dist = Mathf.Abs(npcXPos - componentData.curCarriage.exteriorSlideDoors[i].transform.position.x);
                    if (dist < clostestSlideDoorDists)
                    {
                        clostestSlideDoorDists = dist;
                        bestIndex = i;
                    }
                }
                if (bestIndex !=  -1)
                {
                    componentData.curSlideDoors = componentData.curCarriage.exteriorSlideDoors[bestIndex];
                }
            }
            else
            {
                for (int i = 0; i < componentData.curCarriage.interiorSlideDoors.Length; i++)
                {
                    float dist = Mathf.Abs(npcXPos - componentData.curCarriage.interiorSlideDoors[i].transform.position.x);
                    if (dist < clostestSlideDoorDists)
                    {
                        clostestSlideDoorDists = dist;
                        bestIndex = i;
                    }
                }
                if (bestIndex != -1)
                {
                    componentData.curSlideDoors = componentData.curCarriage.interiorSlideDoors[bestIndex];
                }
            }

            if (componentData.curSlideDoors == null)
            {
                Debug.LogError("No slide door found");
                return;
            }

            stats.curPath = Path.ToSlideDoor;
            stats.targetXPos = componentData.curSlideDoors.transform.position.x;
        }
        else if (inputData.move == 0.0f)
        {

            if (componentData.curSlideDoors.stats.curState == SlideDoors.State.Unlocked)
            {
                componentData.curSlideDoors.OpenDoors();
            }
            else if (componentData.curSlideDoors.stats.curState == SlideDoors.State.Opened) // enter train when slide door is opened
            {
                float zPos = UnityEngine.Random.Range(soData.trainSettings.maxMinWorldZPos.min, soData.trainSettings.maxMinWorldZPos.max);

                componentData.mpb.SetFloat(soData.npcData.materialData.zPosID, zPos);
                componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);


                transform.position = new Vector3(transform.position.x, transform.position.y, zPos);
                transform.SetParent(null, true);
                soData.trainStats.curPassengerCount++;
                QueueForChair();
                componentData.rigidBody.includeLayers = soData.layerSettings.trainMask;
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
        componentData.curCarriage.chairData[stats.chairPosIndex].filled = true;
        stats.curPath = Path.ToChair;
        stats.targetXPos = componentData.curCarriage.chairData[stats.chairPosIndex].xPos;

        transform.position = new Vector3(transform.position.x, transform.position.y, componentData.curCarriage.chairZPos);
        componentData.mpb.SetFloat(soData.npcData.materialData.zPosID, componentData.curCarriage.chairZPos);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);
    }
    public void FindStandingPosition()
    {
        stats.targetXPos = UnityEngine.Random.Range(componentData.curCarriage.insideBoundsCollider.bounds.min.x, componentData.curCarriage.insideBoundsCollider.bounds.max.x);
    }
    private void FindSmokersRoom()
    {
        if (componentData.curCarriage.chairData[stats.chairPosIndex].filled)
        {
            componentData.curCarriage.chairData[stats.chairPosIndex].filled = false;
            stats.chairPosIndex = -1;
        }
        if (NPCManager.npcChairList.Contains(this)) NPCManager.npcChairList.Remove(this); // To prevent them from going back to the chair if they are queued

        if (componentData.curCarriage.smokersRoomData.Length > 1 && componentData.curCarriage.smokersRoomData[1].npcCount < componentData.curCarriage.smokersRoomData[0].npcCount) // selected smoker room is based on which room has less npcs
        {
            stats.smokerRoomIndex = 1;
        }
        else
        {
            stats.smokerRoomIndex = 0;
        }
        componentData.curCarriage.smokersRoomData[stats.smokerRoomIndex].npcCount++;
        stats.curPath = Path.ToSmokerRoom;
        stats.targetXPos = UnityEngine.Random.Range(componentData.curCarriage.smokersRoomData[stats.smokerRoomIndex].minXPos, componentData.curCarriage.smokersRoomData[stats.smokerRoomIndex].maxXPos);

    }
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
        componentData.animator.Play(soData.npcData.animHashData.sittingEating);
    }
    private void PlayStandingEatingAnimation()
    {
        componentData.animator.Play(soData.npcData.animHashData.standingEating);
    }
    private void PlayRandomStandingIdleAnimations()
    {
        if(UnityEngine.Random.Range(0, 2) == 0)
        {
            componentData.animator.Play(soData.npcData.animHashData.standingBlinking);
        }
        else
        {
            componentData.animator.Play(soData.npcData.animHashData.standingBreathing);
        }
    }
    private void PlayRandomSittingIdleAnimations()
    {
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            componentData.animator.Play(soData.npcData.animHashData.sittingBlinking);
        }
        else
        {
            componentData.animator.Play(soData.npcData.animHashData.sittingBreathing);
        }
    }
    private void SetSmokePosition(int index)
    {
        float xPos = componentData.spriteRenderer.flipX ? -soData.npc.smokeAnimPosData[index].position.x : soData.npc.smokeAnimPosData[index].position.x;
        componentData.smoke.transform.localPosition = new Vector3(xPos, soData.npc.smokeAnimPosData[index].position.y, 0);
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
        componentData.mpb.SetFloat(soData.npcData.materialData.zPosID, 0f);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);
    }
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(componentData.boxCollider.bounds.center, new Vector2(stats.targetXPos, componentData.boxCollider.bounds.center.y));

        Vector3 typeLabel = componentData.spriteRenderer.bounds.max + Vector3.up * 0.2f;
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
        Handles.Label(stateLabel, componentData.animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.ToString(), stateStyle);

    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(componentData.boxCollider.bounds.center, new Vector2(soData.npc.maxDistanceDetection, componentData.boxCollider.bounds.extents.y));
    }

}
