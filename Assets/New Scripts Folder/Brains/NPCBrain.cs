using Proselyte.Sigils;
using System;
using UnityEngine;
using static SpyBrain;

public class NPCBrain : MonoBehaviour
{
    public enum State
    {
        Idle,
        Walk,
        Run,
        Dead,
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
        internal RuntimeAnimatorController animController;
        internal SlideDoors slideDoors;
    }
    [SerializeField] ComponentData componentData;

    [Serializable] public struct SOData
    {
        public NPCSettingsSO settings;
        public LayerSettingsSO layerSettings;
        public TrainSettingsSO trainSettings;
    }
    [SerializeField] SOData soData;
    [Serializable] public struct StateData
    {
        internal State curState;
        internal Type type;
    }
    [SerializeField] StateData stateData;

    [Serializable] public struct StatData
    {
        internal bool canBoardTrain;
        internal float targetXVelocity;
        internal float curRunSpeed;
        internal Vector2 targetPos;
    }
    [SerializeField] StatData statData;

    [Serializable] public struct InputData
    {
        internal bool jump;
        internal float move;
        internal bool run;
    }
    [SerializeField] InputData inputData;

    [Serializable] public struct GameEventData
    {
        public GameEvent OnUnlockSlideDoors;
    }
    [SerializeField] GameEventData gameEventData;

    [Serializable] public struct AnimClipData
    {
        public AnimationClip startRunClip;

        internal int calmBreathingHash;
        internal int calmBlinkingHash;
        internal int sitBreathingHash;
        internal int sitBlinkingHash;
        internal int walkHash;
        internal int startRunHash;
        internal int runHash;
        internal int deadHash;
        internal int panicBreathingHash;
        internal int panicBlinkingHash;
    }
    [SerializeField] AnimClipData animClipData;

    [Serializable] public struct MaterialData
    {
        public SpriteRenderer renderer;
        internal MaterialPropertyBlock mpb;
        internal int colorID;
        internal int zPosID;
        internal int mainTexID;
    }
    [SerializeField] MaterialData materialData;
    private void Awake()
    {
        componentData.animController = componentData.animator.runtimeAnimatorController;

        animClipData.calmBreathingHash = Animator.StringToHash("CalmBreathing");
        animClipData.calmBlinkingHash = Animator.StringToHash("CalmBlinking");
        animClipData.sitBreathingHash = Animator.StringToHash("SitBreathing");
        animClipData.sitBlinkingHash = Animator.StringToHash("SitBlinking");
        animClipData.walkHash = Animator.StringToHash("Walk");
        animClipData.startRunHash = Animator.StringToHash("StartRun");
        animClipData.runHash = Animator.StringToHash("Run");
        animClipData.deadHash = Animator.StringToHash("Dead");
        animClipData.panicBreathingHash = Animator.StringToHash("PanicBreathing");
        animClipData.panicBlinkingHash = Animator.StringToHash("PanicBlinking");

        materialData.mpb = new MaterialPropertyBlock();
        materialData.colorID = Shader.PropertyToID("_Color");
        materialData.zPosID = Shader.PropertyToID("_ZPos");
        materialData.mainTexID = Shader.PropertyToID("_MainTex");

        //SetAnimationEvent(animClipData.startRunClip, nameof(PlayRunningClip));  // TODO: uncomment when run clip is ready
    }
    private void OnEnable()
    {
        gameEventData.OnUnlockSlideDoors.RegisterListener(() => statData.canBoardTrain = true);
    }
    private void OnDisable()
    {
        gameEventData.OnUnlockSlideDoors.UnregisterListener(() => statData.canBoardTrain = true);
    }
    private void Start()
    {
        stateData.curState = State.Idle;
        componentData.rigidBody.includeLayers = soData.layerSettings.stationGround;
        statData.curRunSpeed = 1.0f;

        materialData.mpb.SetFloat(materialData.zPosID, soData.trainSettings.entityDepthRange.x);
        materialData.renderer.SetPropertyBlock(materialData.mpb);

    }
    private void Update()
    {
        SelectingStates();
        UpdateStates();

        materialData.mpb.SetTexture(materialData.mainTexID, componentData.spriteRenderer.sprite.texture);
        materialData.renderer.SetPropertyBlock(materialData.mpb);
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        BoardTrain();

        statData.targetXVelocity = soData.settings.moveSpeed * statData.curRunSpeed * inputData.move;
        componentData.rigidBody.linearVelocityX = Mathf.Lerp(componentData.rigidBody.linearVelocityX, statData.targetXVelocity, soData.settings.groundAccelation * Time.fixedDeltaTime);
    }
    private void SelectingStates()
    {
        if (inputData.move != 0 && !inputData.run)
        {
            SetState(State.Walk);
        }
        else if (inputData.move != 0 && inputData.run) 
        {
            SetState(State.Run);
        }
        else
        {
            SetState(State.Idle);
        }
    }
    private void UpdateStates()
    {
        switch (stateData.curState)
        {
            case State.Idle:
            {
            }
            break;
            case State.Walk:
            {
                componentData.spriteRenderer.flipX = inputData.move < 0;
            }
            break;
            case State.Run:
            {
                componentData.spriteRenderer.flipX = inputData.move < 0;
            }
            break;
        }
    }
    private void FixedUpdateStates()
    {
        switch (stateData.curState)
        {
            case State.Idle:
            {

            }
            break;
            case State.Walk:
            {
            }
            break;
        }
    }
    private void SetState(State newState)
    {
        if (stateData.curState == newState) return;
        ExitState();
        stateData.curState = newState;
        EnterState();
    }
    private void EnterState()
    {
        switch (stateData.curState)
        {
            case State.Idle:
            {
                componentData.animator.Play(animClipData.calmBreathingHash);
            }
            break;
            case State.Walk:
            {
                componentData.animator.Play(animClipData.walkHash);
            }
            break;
            case State.Run:
            {
                componentData.animator.Play(animClipData.startRunHash);
                statData.curRunSpeed = soData.settings.runSpeedMultiplier;
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (stateData.curState)
        {
            case State.Idle:
            {

            }
            break;
            case State.Walk:
            {
            }
            break;
            case State.Run:
            {
                statData.curRunSpeed = 1.0f;
            }
            break;
        }

    }
    private void PlayRunningClip()
    {
        componentData.animator.Play(animClipData.runHash);
    }
    private void BoardTrain()
    {
        if (!statData.canBoardTrain || componentData.rigidBody.includeLayers != soData.layerSettings.stationGround) return; // only board train when you can board and npc is on the station ground

        if (componentData.slideDoors == null) // find slide door in one frame
        { 
            Vector2 startLinecast = new Vector2(componentData.boxCollider.bounds.center.x - soData.settings.maxSlideDoorDistanceDetection, componentData.boxCollider.bounds.center.y);
            Vector2 endLinecast = new Vector2(componentData.boxCollider.bounds.center.x + soData.settings.maxSlideDoorDistanceDetection, componentData.boxCollider.bounds.center.y);
            RaycastHit2D slideDoorHit = Physics2D.Linecast(startLinecast, endLinecast, soData.layerSettings.slideDoors);

            if (slideDoorHit.collider == null) { Debug.LogWarning($"{name} did not find a slide door to go to"); return; }

            componentData.slideDoors = slideDoorHit.collider.GetComponent<SlideDoors>(); 
        }
        else
        {
            float distToSlideDoor = componentData.slideDoors.transform.position.x - transform.position.x;

            if (Mathf.Abs(distToSlideDoor) > 0.1f)
            {
                inputData.move = Mathf.Sign(distToSlideDoor);
            }
            else
            {
                inputData.move = 0;
                if (componentData.slideDoors.stateData.curState == SlideDoors.State.Unlocked)
                {
                    componentData.slideDoors.OpenDoors();
                }
                else if (componentData.slideDoors.stateData.curState == SlideDoors.State.Opened) // enter train when slide door is opened
                {
                    float zPos = UnityEngine.Random.Range(soData.trainSettings.entityDepthRange.x, soData.trainSettings.entityDepthRange.y);

                    materialData.mpb.SetFloat(materialData.zPosID, zPos);
                    materialData.renderer.SetPropertyBlock(materialData.mpb);

                    transform.position = new Vector3(transform.position.x, transform.position.y, zPos);
                    componentData.rigidBody.includeLayers = soData.layerSettings.trainGround;                   
                }

            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 startLinecast = new Vector2(componentData.boxCollider.bounds.center.x - soData.settings.maxSlideDoorDistanceDetection, componentData.boxCollider.bounds.center.y);
        Vector2 endLinecast = new Vector2(componentData.boxCollider.bounds.center.x + soData.settings.maxSlideDoorDistanceDetection, componentData.boxCollider.bounds.center.y);
        Gizmos.DrawLine(startLinecast, endLinecast);
    }
}
