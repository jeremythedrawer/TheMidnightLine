using Proselyte.Sigils;
using System;
using UnityEngine;

public class NPCBrain : MonoBehaviour
{
    public enum State
    {
        Idle,
        Walk,
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
        internal RuntimeAnimatorController animController;
        internal SlideDoors slideDoors;
        internal CarriageChairs carriageChairs;
    }
    [SerializeField] ComponentData componentData;

    [Serializable] public struct SOData
    {
        public NPCSettingsSO settings;
        public LayerSettingsSO layerSettings;
        public TrainSettingsSO trainSettings;
        public TrainStatsSO trainStats;
        public NPCsDataSO npcData;
        public GameEventDataSO gameEventData;
        internal StationSO startStation;
        internal StationSO endStation;
    }
    [SerializeField] public SOData soData;

    [Serializable] public struct StatData
    {
        internal State curState;
        internal Type type;
        internal Behaviours behaviours;
        internal bool canBoardTrain;
        internal float targetXVelocity;
        internal float curRunSpeed;
        internal float targetXPos;
        internal float targetDist;
    }
    [SerializeField] public StatData stats;

    [Serializable] public struct InputData
    {
        internal bool jump;
        internal float move;
    }
    [SerializeField] InputData inputData;

    [Serializable] public struct AnimHashData
    {
        internal int calmBreathingHash;
        internal int calmBlinkingHash;

        internal int sitBreathingHash;
        internal int sitBlinkingHash;
        
        internal int walkHash;
    }
    [SerializeField] AnimHashData animHashData;

    [Serializable] public struct AnimClipData
    {
        public AnimationClip[] standingCalmClips;
        public AnimationClip[] sittingCalmClips;

    }
    [SerializeField] AnimClipData animClipData;

    [Serializable] public struct MaterialData
    {
        internal int colorID;
        internal int zPosID;
        internal int mainTexID;
    }
    [SerializeField] MaterialData materialData;
    private void Awake()
    {
        componentData.animController = componentData.animator.runtimeAnimatorController;
        componentData.mpb = new MaterialPropertyBlock();

        animHashData.calmBreathingHash = Animator.StringToHash("CalmBreathing");
        animHashData.calmBlinkingHash = Animator.StringToHash("CalmBlinking");
        animHashData.sitBreathingHash = Animator.StringToHash("SitBreathing");
        animHashData.sitBlinkingHash = Animator.StringToHash("SitBlinking");
        animHashData.walkHash = Animator.StringToHash("Walk");

        materialData.colorID = Shader.PropertyToID("_Color");
        materialData.zPosID = Shader.PropertyToID("_ZPos");
        materialData.mainTexID = Shader.PropertyToID("_MainTex");
    }
    private void OnEnable()
    {
        soData.gameEventData.OnUnlockSlideDoors.RegisterListener(() => stats.canBoardTrain = true);
    }
    private void OnDisable()
    {
        soData.gameEventData.OnUnlockSlideDoors.UnregisterListener(() => stats.canBoardTrain = true);
    }
    private void Start()
    {
        stats.curState = State.Idle;
        componentData.rigidBody.includeLayers = soData.layerSettings.stationLayers.ground;
        stats.curRunSpeed = 1.0f;

        componentData.mpb.SetFloat(materialData.zPosID, soData.trainSettings.entityDepthRange.x);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

        stats.targetXPos = transform.position.x;
        stats.behaviours = Utils.GetBehaviours();

    }
    private void Update()
    {
        SelectingStates();
        UpdateStates();
        componentData.mpb.SetTexture(materialData.mainTexID, componentData.spriteRenderer.sprite.texture);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

        stats.targetDist = stats.targetXPos - transform.position.x;
        inputData.move = Mathf.Abs(stats.targetDist) > 0.1f ? Mathf.Sign(stats.targetDist) : 0.0f;
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        BoardTrain();

        stats.targetXVelocity = soData.settings.moveSpeed * stats.curRunSpeed * inputData.move;
        componentData.rigidBody.linearVelocityX = Mathf.Lerp(componentData.rigidBody.linearVelocityX, stats.targetXVelocity, soData.settings.groundAccelation * Time.fixedDeltaTime);
    }
    private void OnApplicationQuit()
    {
        ResetData();
    }
    private void SelectingStates()
    {
        if (inputData.move != 0)
        {
            SetState(State.Walk);
        }
        else
        {
            SetState(State.Idle);
        }
    }
    private void UpdateStates()
    {
        switch (stats.curState)
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
        }
    }
    private void FixedUpdateStates()
    {
        switch (stats.curState)
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
        if (stats.curState == newState) return;
        ExitState();
        stats.curState = newState;
        EnterState();

    }
    private void EnterState()
    {
        switch (stats.curState)
        {
            case State.Idle:
            {
                if (componentData.carriageChairs != null)
                {
                    componentData.animator.Play(animHashData.sitBreathingHash);
                }
                else
                {
                    componentData.animator.Play(animHashData.calmBreathingHash);
                }
            }
            break;
            case State.Walk:
            {
                componentData.animator.Play(animHashData.walkHash);
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (stats.curState)
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
    private void BoardTrain()
    {
        if (!stats.canBoardTrain) return; // only board train when you can board and npc is on the station ground

        if (componentData.rigidBody.includeLayers == soData.layerSettings.stationMask)
        {
            if (componentData.slideDoors == null) // find slide door in one frame
            {
                RaycastHit2D slideDoorHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, new Vector2(soData.settings.maxDistanceDetection, componentData.boxCollider.size.y), 0.0f, transform.right, soData.settings.maxDistanceDetection, soData.layerSettings.trainLayers.slideDoors);

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

                    componentData.mpb.SetFloat(materialData.zPosID, zPos);
                    componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

                    transform.position = new Vector3(transform.position.x, transform.position.y, zPos);
                    transform.SetParent(null, true);
                    soData.trainStats.curPassengerCount++;
                    componentData.rigidBody.includeLayers = soData.layerSettings.trainMask;                   
                }
            }
        }
        else if (componentData.rigidBody.includeLayers == soData.layerSettings.trainMask)
        {
            if (!soData.npcData.boardingNPCQueue.Contains(this) && componentData.carriageChairs == null)
            {
                soData.npcData.boardingNPCQueue.Enqueue(this);
            }
        }
    }
    public void FindCarriageChair()
    {
        Vector2 boxCastSize = new Vector2(soData.settings.maxDistanceDetection * 2, componentData.boxCollider.bounds.size.y);
        RaycastHit2D carriageChairsHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, boxCastSize, 0.0f, transform.right, soData.settings.maxDistanceDetection, soData.layerSettings.trainLayers.carriageChairs);

        CarriageChairs selectedChairs = carriageChairsHit.collider.GetComponent<CarriageChairs>();

        for (int i = 0; i < selectedChairs.chairData.Length; i++)
        {
            if(!selectedChairs.chairData[i].filled)
            {
                stats.targetXPos = selectedChairs.chairData[i].chairXPos;
                selectedChairs.chairData[i].filled = true;

                float zPos = selectedChairs.transform.position.z - 1;
                transform.position = new Vector3(transform.position.x, transform.position.y, zPos);
                componentData.mpb.SetFloat(materialData.zPosID, zPos);
                componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

                break;
            }
        }
        componentData.carriageChairs = selectedChairs;
    }
    private void ResetData()
    {
        componentData.mpb.SetFloat(materialData.zPosID, 0f);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);
    }
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(componentData.boxCollider.bounds.center, new Vector2(stats.targetXPos, componentData.boxCollider.bounds.center.y));
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(componentData.boxCollider.bounds.center, new Vector2(soData.settings.maxDistanceDetection, componentData.boxCollider.bounds.extents.y));
    }
}
