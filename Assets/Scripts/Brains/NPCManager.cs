using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using static NPC;

public class NPCManager : MonoBehaviour
{
    const float QUEUE_TICK_RATE = 0.3f;
    const int MAX_QUEUE_SIZE = 128;

    public TripSO trip;
    public GameEventDataSO gameEventData;
    public NPCsDataSO npcsData;

    [Header("Generated")]
    public bool npcFindingChair;
    public int totalAgentCount;

    public static NPCQueue seatQueue;
    public static NPCQueue boardTrainQueue;
    public static NPCQueue disembarkTrainQueue;

    public static Dictionary<VisualEffect, Queue<VisualEffect>> glyphPool;
    private void Awake()
    {
        seatQueue = new NPCQueue();
        boardTrainQueue = new NPCQueue();
        disembarkTrainQueue = new NPCQueue();

        seatQueue.npcs = new NPCBrain[MAX_QUEUE_SIZE];
        boardTrainQueue.npcs = new NPCBrain[MAX_QUEUE_SIZE];
        disembarkTrainQueue.npcs = new NPCBrain[MAX_QUEUE_SIZE];

        glyphPool = new Dictionary<VisualEffect, Queue<VisualEffect>>();

    }

    private void Start()
    {
        npcsData.behaviourContextDict = SetBehaviourContextDictionary();
    }
    private void OnEnable()
    {
        gameEventData.OnStationArrival.RegisterListener(ProcessTrainQueues);
    }
    private void OnDisable()
    {
        gameEventData.OnStationArrival.UnregisterListener(ProcessTrainQueues);
    }
    private void Update()
    {
        ProcessSeatQueue();
    }
    private void ProcessSeatQueue()
    {
        if (seatQueue.npcsCount == 0) return;

        seatQueue.timer -= Time.deltaTime;
        if (seatQueue.timer > 0f) return;

        seatQueue.timer = QUEUE_TICK_RATE;

        NPCBrain npc = seatQueue.npcs[seatQueue.npcsCount - 1];
        seatQueue.npcsCount--;
        AssignNextNPCPosition(npc);
    }
    public static void AddToSeatQueue(NPCBrain npc)
    {
        npc.seatQueueIndex = seatQueue.npcsCount;
        seatQueue.npcs[seatQueue.npcsCount] = npc;
        seatQueue.npcsCount++;
    }
    public static void RemoveFromSeatQueue(NPCBrain npc)
    {
        if (seatQueue.npcsCount == 0) return;
        int lastIndex = seatQueue.npcsCount - 1;

        seatQueue.npcs[npc.seatQueueIndex] = seatQueue.npcs[lastIndex];
        seatQueue.npcs[lastIndex] = npc;
        seatQueue.npcsCount--;
    }
    public static void AddToBoardTrainQueue(NPCBrain npc)
    {
        npc.boardTrainIndex = boardTrainQueue.npcsCount;
        boardTrainQueue.npcs[boardTrainQueue.npcsCount] = npc;
        boardTrainQueue.npcsCount++;
    }
    public static void AddToDisembarkTrainQueue(NPCBrain npc)
    {
        npc.disembarkTrainIndex = disembarkTrainQueue.npcsCount;
        disembarkTrainQueue.npcs[disembarkTrainQueue.npcsCount] = npc;
        disembarkTrainQueue.npcsCount++;
    }
    public static VisualEffect GetGlyph(VisualEffect glyphPrefab, Transform parent)
    {
        if (!glyphPool.TryGetValue(glyphPrefab, out Queue<VisualEffect> queue))
        {
            queue = new Queue<VisualEffect>();
            glyphPool[glyphPrefab] = queue;
        }

        if (queue.Count > 0)
        {
            VisualEffect gylphInstance = queue.Dequeue();
            gylphInstance.gameObject.SetActive(true);
            gylphInstance.Reinit();
            gylphInstance.gameObject.transform.position = parent.position;
            gylphInstance.gameObject.transform.parent = parent;
            return gylphInstance;
        }

        return Instantiate(glyphPrefab, parent.transform.position, parent.transform.rotation, parent);
    }
    public static void ReturnGlyph(VisualEffect glyphPrefab, VisualEffect glyphInstance)
    {
        glyphInstance.Stop();
        glyphInstance.gameObject.transform.parent = null;
        if(!glyphPool.TryGetValue(glyphPrefab, out Queue<VisualEffect> queue))
        {
            queue = new Queue<VisualEffect>();
            glyphPool[glyphPrefab] = queue;
        }

        queue.Enqueue(glyphInstance);
    }
    private void ProcessTrainQueues()
    {
        ProcessingTrainQueues().Forget();
    }
    private async UniTask ProcessingTrainQueues()
    {
        await UniTask.Yield();

        while(disembarkTrainQueue.npcsCount > 0)
        {
            NPCBrain npc = disembarkTrainQueue.npcs[disembarkTrainQueue.npcsCount - 1];
            disembarkTrainQueue.npcsCount--;
            npc.DisembarkTrain();
            await UniTask.Yield();
            await UniTask.Delay((int)QUEUE_TICK_RATE * 1000);
        }

        while(boardTrainQueue.npcsCount > 0)
        {
            NPCBrain npc = boardTrainQueue.npcs[boardTrainQueue.npcsCount - 1];
            boardTrainQueue.npcsCount--;
            npc.BoardTrain();
            await UniTask.Yield();
            await UniTask.Delay((int)QUEUE_TICK_RATE * 1000);
        }
    }
    private void AssignNextNPCPosition(NPCBrain npc)
    {
        float npcX = npc.transform.position.x;
        float closestDist = float.PositiveInfinity;
        int bestIndex = int.MaxValue;

        for (int i = 0; i < npc.curCarriage.seatAmount; i++)
        {
            if (npc.curCarriage.seatData.filled[i]) continue;

            float dist = Mathf.Abs(npcX - npc.curCarriage.seatData.xPos[i]);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestIndex = i;
            }
        }

        if (bestIndex == int.MaxValue)
        {
            npc.FindStandingPosition();
        }
        else
        {
            npc.AssignSeat(bestIndex);
        }
    }
    private Dictionary<Behaviours, NPCBehaviourContextSO> SetBehaviourContextDictionary()
    {
        Dictionary<Behaviours, NPCBehaviourContextSO> dict = new Dictionary<Behaviours, NPCBehaviourContextSO>();

        foreach (NPCBehaviourContextSO context in npcsData.behaviourContexts)
        {
            dict[context.behaviour] = context;
        }

        return dict;
    }
}

