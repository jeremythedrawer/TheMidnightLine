using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
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
    public static NPCQueue enterTrainQueue;
    public static NPCQueue exitTrainQueue;

    public NPCQueue seatQ;
    public NPCQueue enterTrainQ;
    public NPCQueue exitTrainQ;
    private void Awake()
    {
        seatQueue = new NPCQueue();
        enterTrainQueue = new NPCQueue();
        exitTrainQueue = new NPCQueue();

        seatQueue.npcs = new NPCBrain[MAX_QUEUE_SIZE];
        enterTrainQueue.npcs = new NPCBrain[MAX_QUEUE_SIZE];
        exitTrainQueue.npcs = new NPCBrain[MAX_QUEUE_SIZE];
    }
    private void OnEnable()
    {
        gameEventData.OnStationArrival.RegisterListener(ProcessEnterTrainQueue);
    }

    private void OnDisable()
    {
        gameEventData.OnStationArrival.UnregisterListener(ProcessEnterTrainQueue);
    }
    private void Update()
    {
        ProcessSeatQueue();
        seatQ = seatQueue;
        enterTrainQ = enterTrainQueue;
        exitTrainQ = exitTrainQueue;
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
        int lastIndex = seatQueue.npcsCount - 1;

        seatQueue.npcs[npc.seatQueueIndex] = seatQueue.npcs[lastIndex];
        seatQueue.npcs[lastIndex] = npc;
        seatQueue.npcsCount--;
    }
    private void ProcessEnterTrainQueue()
    {
        ProcessingEnterTrainQueue().Forget();
    }
    private async UniTask ProcessingEnterTrainQueue()
    {
        await UniTask.Yield();
        while(enterTrainQueue.npcsCount > 0)
        {

            NPCBrain npc = enterTrainQueue.npcs[enterTrainQueue.npcsCount - 1];
            enterTrainQueue.npcsCount--;
            npc.BoardTrain();
            await UniTask.Yield();
            await UniTask.Delay((int)QUEUE_TICK_RATE * 1000);
        }
    }
    public static void AddToEnterTrainQueue(NPCBrain npc)
    {
        npc.enterTrainIndex = enterTrainQueue.npcsCount;
        enterTrainQueue.npcs[enterTrainQueue.npcsCount] = npc;
        enterTrainQueue.npcsCount++;
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
}

