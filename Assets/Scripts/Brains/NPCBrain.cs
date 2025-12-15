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
        Eating
    }
    
    public enum Type
    {
        Agent,
        Bystander
    }
    [Serializable] public struct ComponentData
    {
        public Rigidbody2D rigidBody;
        public BoxCollider2D boxCollider;
        public Animator animator;
        public SpriteRenderer spriteRenderer;

        internal MaterialPropertyBlock mpb;
        internal CancellationTokenSource ctsFade;
        internal SlideDoors slideDoors;
        internal CarriageChairs carriageChairs;
        internal Collider2D carriageSizeCollider;
        internal Collider2D smokerRoom;
        internal VisualEffect sleepingZs;
        internal GameObject smoke;
    }
    [SerializeField] ComponentData componentData;

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
        internal State curState;
        internal Type type;
        internal NPCTraits.Behaviours behaviours;
        internal NPCTraits.Behaviours curBehaviour;
        internal Color selectedColor;

        internal bool canBoardTrain;
        internal float targetXVelocity;
        internal float curRunSpeed;
        internal float targetXPos;
        internal float targetDist;

        internal float stateTimer;
        internal float stateDuration;

        internal float alpha;
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
        SetAnimationEvents();
        componentData.mpb = new MaterialPropertyBlock();
        componentData.ctsFade = new CancellationTokenSource();
    }
    private void OnEnable()
    {
        soData.gameEventData.OnUnlockSlideDoors.RegisterListener(() => stats.canBoardTrain = true);


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
        soData.gameEventData.OnUnlockSlideDoors.UnregisterListener(() => stats.canBoardTrain = true);
    }
    private void Start()
    {
        stats.curState = State.Idling;
        componentData.rigidBody.includeLayers = soData.layerSettings.stationLayers.ground;
        stats.curRunSpeed = 1.0f;

        componentData.mpb.SetFloat(soData.npcData.materialData.zPosID, soData.trainSettings.entityDepthRange.x);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

        stats.targetXPos = transform.position.x;        
    }
    private void Update()
    {
        SelectingStates();
        UpdateStates();
        QueueForChair();
        Fade();
        componentData.mpb.SetTexture(soData.npcData.materialData.mainTexID, componentData.spriteRenderer.sprite.texture);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

        stats.targetDist = stats.targetXPos - transform.position.x;
        inputData.move = Mathf.Abs(stats.targetDist) > 0.1f ? Mathf.Sign(stats.targetDist) : 0;

        if (componentData.rigidBody.includeLayers == soData.layerSettings.trainMask)
        {
            stats.stateTimer += Time.deltaTime;
        }
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        BoardTrain();

        if ((stats.curBehaviour & NPCTraits.Behaviours.Frequent_smoker) != 0 && componentData.smokerRoom == null)
        {
            FindSmokersRoom();
        }

        stats.targetXVelocity = soData.npc.moveSpeed * stats.curRunSpeed * inputData.move;
        componentData.rigidBody.linearVelocityX = Mathf.Lerp(componentData.rigidBody.linearVelocityX, stats.targetXVelocity, soData.npc.groundAccelation * Time.fixedDeltaTime);
    }
    private void OnApplicationQuit()
    {
        ResetData();
    }
    #region State Methods
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
        else if ((stats.curBehaviour & NPCTraits.Behaviours.Frequent_smoker) != 0 && inputData.move == 0 && componentData.smokerRoom != null && transform.position.x > componentData.smokerRoom.bounds.min.x && transform.position.x < componentData.smokerRoom.bounds.max.x)
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
                if (componentData.carriageChairs != null)
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
                componentData.carriageChairs = null;
            }
            break;
            case State.Smoking:
            {
                stats.stateDuration = UnityEngine.Random.Range(soData.npc.pickBehaviourDurationRange.x, soData.npc.pickBehaviourDurationRange.y);
                componentData.animator.Play(soData.npcData.animHashData.smoking);
            }
            break;
            case State.Sleeping:
            {
                stats.stateDuration = UnityEngine.Random.Range(soData.npc.pickBehaviourDurationRange.x, soData.npc.pickBehaviourDurationRange.y);
                componentData.sleepingZs.Play();
                if (componentData.carriageChairs != null)
                {
                    componentData.animator.Play(soData.npcData.animHashData.sittingSleeping);
                }
                else
                {
                    componentData.animator.Play(soData.npcData.animHashData.standingBreathing);
                }
            }
            break;
            case State.Eating:
            {
                stats.stateDuration = UnityEngine.Random.Range(soData.npc.pickBehaviourDurationRange.x, soData.npc.pickBehaviourDurationRange.y);
                if (componentData.carriageChairs != null)
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
    #endregion
    #region Material Methods
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
        if (!stats.startFade)
        {
            if (soData.trainStats.slideDoorsToUnlock == SlideDoors.Type.Exterior && componentData.rigidBody.includeLayers == soData.layerSettings.stationMask) // while on station, bystanders do not recognise train bounds, but they need to to fade out
            {
                componentData.ctsFade?.Cancel();
                componentData.ctsFade?.Dispose();
                componentData.ctsFade = new CancellationTokenSource();
                Debug.Log("fading");
                Fading(fadeIn: false, componentData.ctsFade.Token).Forget();
            }
            else if (stats.alpha != 1)
            {
                componentData.ctsFade?.Cancel();
                componentData.ctsFade?.Dispose();
                componentData.ctsFade = new CancellationTokenSource();
                Fading(fadeIn: true, componentData.ctsFade.Token).Forget();
            }
        }
    }
    private async UniTask Fading(bool fadeIn, CancellationToken token)
    {
        stats.startFade = true;
        try
        {
            float elaspedTime = stats.alpha * soData.npcData.fadeTime;
            while (fadeIn ? elaspedTime < soData.npcData.fadeTime : elaspedTime > 0f)
            {
                token.ThrowIfCancellationRequested();

                elaspedTime += (fadeIn ? Time.deltaTime : -Time.deltaTime);

                stats.alpha = elaspedTime / soData.npcData.fadeTime;
                componentData.mpb.SetFloat(soData.npcData.materialData.alphaID, stats.alpha);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            stats.alpha = fadeIn ? 1 : 0;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            stats.startFade = false;
        }
    }
    #endregion

    private void BoardTrain()
    {
        if (!stats.canBoardTrain || componentData.rigidBody.includeLayers == soData.layerSettings.trainMask) return; // only board train when you can board and npc is on the station ground

        if (componentData.slideDoors == null) // find slide door in one frame
        {
            RaycastHit2D slideDoorHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, new Vector2(soData.npc.maxDistanceDetection, componentData.boxCollider.size.y), 0.0f, transform.right, soData.npc.maxDistanceDetection, soData.layerSettings.trainLayers.slideDoors);

            if (slideDoorHit.collider == null) { Debug.LogError($"{name} did not find a slide door to go to"); return; }

            SlideDoors selectedDoors = slideDoorHit.collider.GetComponent<SlideDoors>();
            stats.targetXPos = selectedDoors.transform.position.x;
            componentData.slideDoors = selectedDoors;

        }
        else if (inputData.move == 0.0f)
        {
            if (componentData.slideDoors.stats.curState == SlideDoors.State.Unlocked)
            {
                componentData.slideDoors.OpenDoors();
            }
            else if (componentData.slideDoors.stats.curState == SlideDoors.State.Opened) // enter train when slide door is opened
            {
                float zPos = UnityEngine.Random.Range(soData.trainSettings.entityDepthRange.x, soData.trainSettings.entityDepthRange.y);

                RaycastHit2D carriageSizeHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, componentData.boxCollider.bounds.size, 0.0f, transform.right, soData.npc.maxDistanceDetection, soData.layerSettings.trainLayers.carriagExteriorWalls);

                if (carriageSizeHit.collider == null) { Debug.LogError($"{name} did not get the carriage size collider"); return; }

                componentData.carriageSizeCollider = carriageSizeHit.collider;

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
        if (componentData.rigidBody.includeLayers == soData.layerSettings.trainMask) // now on train
        {
            if (!soData.npcData.boardingNPCQueue.Contains(this) && componentData.carriageChairs == null)
            {
                soData.npcData.boardingNPCQueue.Enqueue(this);
            }
        }
    }
    public void FindCarriageChair()
    {
        RaycastHit2D carriageChairsHit = Physics2D.BoxCast(componentData.carriageSizeCollider.bounds.center, componentData.carriageSizeCollider.bounds.size, 0.0f, transform.right, componentData.carriageSizeCollider.bounds.size.x, soData.layerSettings.trainLayers.carriageChairs);

        CarriageChairs selectedChairs = carriageChairsHit.collider.GetComponent<CarriageChairs>();

        for (int i = 0; i < selectedChairs.chairData.Length; i++)
        {
            if(!selectedChairs.chairData[i].filled)
            {
                stats.targetXPos = selectedChairs.chairData[i].chairXPos;
                selectedChairs.chairData[i].filled = true;

                float zPos = selectedChairs.transform.position.z - 1;
                transform.position = new Vector3(transform.position.x, transform.position.y, zPos);
                componentData.mpb.SetFloat(soData.npcData.materialData.zPosID, zPos);
                componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

                componentData.carriageChairs = selectedChairs;
                break;
            }
        }
    }
    private void FindSmokersRoom()
    {
        RaycastHit2D smokersRoomHit = Physics2D.BoxCast(componentData.carriageSizeCollider.bounds.center, componentData.carriageSizeCollider.bounds.size, 0.0f, transform.right, componentData.carriageSizeCollider.bounds.size.x, soData.layerSettings.trainLayers.smokingRoom);
        componentData.carriageChairs = null;
        componentData.smokerRoom = smokersRoomHit.collider;
        stats.targetXPos = UnityEngine.Random.Range(componentData.smokerRoom.bounds.min.x, componentData.smokerRoom.bounds.max.x);
    }
    #region Animation Events
    private void SetAnimationEvents()
    {
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.sittingAboutToEat], nameof(PlaySittingEating));
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.standingAboutToEat],nameof(PlayStandingEatingAnimation));
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.standingBreathing],nameof(PlayRandomStandingIdleAnimations));
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.standingBlinking], nameof(PlayRandomStandingIdleAnimations));
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.sittingBreathing], nameof(PlayRandomSittingIdleAnimations));
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.sittingBlinking], nameof(PlayRandomSittingIdleAnimations));
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
    #endregion
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
