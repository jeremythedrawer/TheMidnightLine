using Cysharp.Threading.Tasks;
using UnityEngine;
using static NPC;

public class SlideDoors : MonoBehaviour
{
    const float UNLOCK_MOVE_AMOUNT_PERCENT = 0.01f;
    const float OPEN_MOVE_AMOUNT_PERCENT = 0.99f;
    const float UNLOCK_MOVE_TIME = 0.3f;
    const int MAX_QUEUE_SIZE = 128;
    const float QUEUE_TICK_RATE = 0.3f;
    public enum State
    { 
        Locked,
        Unlocked,
        Opening,
        Opened,
        Closing,
    }

    public TrainStatsSO trainStats;
    public TrainSettingsSO trainSettings;

    public AtlasRenderer rightSlideDoorRenderer;
    public AtlasRenderer leftSlideDoorRenderer;
    public BoxCollider2D boxCollider;
    [Header("Generated")]
    public State curState;
    public Transform rightSlideDoor_transform;
    public Transform leftSlideDoor_transform;

    public Vector3 rightSlideDoorPos;
    public Vector3 leftSlideDoorPos;

    public float activeMoveAmount;
    public float unlockMoveAmount;
    public float moveTimer;
    public NPCQueue boardTrainQueue;
    public NPCQueue disembarkTrainQueue;
    private void OnDisable()
    {
        ResetDoors();
    }
    private void Start()
    {
        ResetDoors();
        curState = State.Locked;
        rightSlideDoor_transform = rightSlideDoorRenderer.transform;
        leftSlideDoor_transform = leftSlideDoorRenderer.transform;
        activeMoveAmount = rightSlideDoorRenderer.sprite.worldSize.x * OPEN_MOVE_AMOUNT_PERCENT;
        unlockMoveAmount = rightSlideDoorRenderer.sprite.worldSize.x * UNLOCK_MOVE_AMOUNT_PERCENT;

        boardTrainQueue = new NPCQueue();
        disembarkTrainQueue = new NPCQueue();
        boardTrainQueue.npcs = new NPCBrain[MAX_QUEUE_SIZE];
        disembarkTrainQueue.npcs = new NPCBrain[MAX_QUEUE_SIZE];
    }
    private void Update()
    {
        UpdateState();
    }
    private void SetState(State newState)
    {
        if(newState == curState) return;
        ExitState();
        State prevState = curState;
        curState = newState;
        EnterState(prevState);
    }
    private void UpdateState()
    {
        switch(curState)
        {
            case State.Opening:
            {
                moveTimer += Time.deltaTime;
                float t = moveTimer / trainSettings.doorMoveTime;
                rightSlideDoorPos.x = activeMoveAmount * t;
                leftSlideDoorPos.x = activeMoveAmount * -t;
                rightSlideDoor_transform.localPosition = rightSlideDoorPos;
                leftSlideDoor_transform.localPosition = leftSlideDoorPos;

                if (t >= 1)
                {
                    SetState(State.Opened);
                }
            }
            break;

            case State.Opened:
            {
                if (disembarkTrainQueue.npcsCount > 0)
                {
                    disembarkTrainQueue.timer += Time.deltaTime;
                    if (disembarkTrainQueue.timer > QUEUE_TICK_RATE)
                    {
                        NPCBrain npc = disembarkTrainQueue.npcs[disembarkTrainQueue.npcsCount - 1];
                        npc.DisembarkTrain();
                        disembarkTrainQueue.npcsCount--;
                        disembarkTrainQueue.timer = 0;
                    }
                }
                else if (boardTrainQueue.npcsCount > 0)
                {
                    boardTrainQueue.timer += Time.deltaTime;

                    if (boardTrainQueue.timer > QUEUE_TICK_RATE)
                    {
                        NPCBrain npc = boardTrainQueue.npcs[boardTrainQueue.npcsCount - 1];
                        npc.BoardTrain();

                        boardTrainQueue.npcsCount--;
                        boardTrainQueue.timer = 0;
                    }
                }
            }
            break;

            case State.Closing:
            {
                moveTimer -= Time.deltaTime;
                float t = moveTimer/ trainSettings.doorMoveTime;

                rightSlideDoorPos.x = activeMoveAmount * t;
                leftSlideDoorPos.x = activeMoveAmount * -t;

                rightSlideDoor_transform.localPosition = rightSlideDoorPos;
                leftSlideDoor_transform.localPosition = leftSlideDoorPos;

                if (t <= 0)
                {
                    SetState(State.Locked);
                }
            }
            break;
        }
    }
    private void ExitState()
    {
        switch(curState)
        {
            case State.Opened:
            {

            }
            break;
        }
    }
    private void EnterState(State prevState)
    {
        switch(curState)
        {
            case State.Unlocked:
            {
            }
            break;

            case State.Opening:
            {
                rightSlideDoorPos = rightSlideDoor_transform.localPosition;
                leftSlideDoorPos = leftSlideDoor_transform.localPosition;
                moveTimer = 0;
            }
            break;

            case State.Opened:
            {
                disembarkTrainQueue.timer = 0;
                boardTrainQueue.timer = 0;

                rightSlideDoorPos.x = activeMoveAmount;
                leftSlideDoorPos.x = -activeMoveAmount;
                rightSlideDoor_transform.localPosition = rightSlideDoorPos;
                leftSlideDoor_transform.localPosition = leftSlideDoorPos;

                trainStats.slideDoorsAmountOpened++;
            }
            break;

            case State.Closing:
            {
                rightSlideDoorPos = rightSlideDoor_transform.localPosition;
                leftSlideDoorPos = leftSlideDoor_transform.localPosition;
                moveTimer = trainSettings.doorMoveTime;

            }
            break;

            case State.Locked:
            {

                rightSlideDoorPos.x = 0;
                leftSlideDoorPos.x = 0;
                rightSlideDoor_transform.localPosition = rightSlideDoorPos;
                leftSlideDoor_transform.localPosition = leftSlideDoorPos;
                if (prevState == State.Closing)
                {
                    trainStats.slideDoorsAmountOpened--;
                }
            }
            break;
        }
    }
    public void UnlockDoors()
    {
        SetState(State.Unlocked);
    }
    public void OpenDoors()
    {
        SetState(State.Opening);
    }
    public void CloseDoors()
    {
        if (curState == State.Opened)
        {
            SetState(State.Closing);
        }
        else
        {
            SetState(State.Locked);
        }
    }
    public void AddToBoardTrainQueue(NPCBrain npc)
    {
        npc.boardTrainQueueIndex = boardTrainQueue.npcsCount;
        boardTrainQueue.npcs[boardTrainQueue.npcsCount] = npc;
        boardTrainQueue.npcsCount++;
    }
    public void AddToDisembarkTrainQueue(NPCBrain npc)
    {
        npc.disembarkTrainQueueIndex = disembarkTrainQueue.npcsCount;
        disembarkTrainQueue.npcs[disembarkTrainQueue.npcsCount] = npc;
        disembarkTrainQueue.npcsCount++;
        Debug.Log(npc.profile.fullName + " is disembarking");
    }
    public void ResetDoors()
    {
        curState = State.Locked;
        rightSlideDoor_transform.localPosition = new Vector3(0, 0, rightSlideDoor_transform.localPosition.z);
        leftSlideDoor_transform.localPosition = new Vector3(0, 0, leftSlideDoor_transform.localPosition.z);
    }
}
