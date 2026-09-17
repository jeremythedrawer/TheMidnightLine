using Cysharp.Threading.Tasks;
using UnityEngine;
using static Passenger;

public class SlideDoors : MonoBehaviour
{
    const float UNLOCK_MOVE_AMOUNT_PERCENT = 0.01f;
    const float OPEN_MOVE_AMOUNT_PERCENT = 0.99f;
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

    public TrainData trainData;
    public AudioData audioData;

    public AtlasRenderer rightSlideDoorRenderer;
    public AtlasRenderer leftSlideDoorRenderer;

    public BoxCollider2D boxCollider;
    public AudioSource audioSource;

    [Header("Generated")]
    public State curState;
    public Transform rightSlideDoorTransform;
    public Transform leftSlideDoorTransform;
    public Carriage carriage;

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
        curState = State.Locked;
        rightSlideDoorTransform = rightSlideDoorRenderer.transform;
        leftSlideDoorTransform = leftSlideDoorRenderer.transform;
        activeMoveAmount = rightSlideDoorRenderer.sprite.worldSize.x * OPEN_MOVE_AMOUNT_PERCENT;
        unlockMoveAmount = rightSlideDoorRenderer.sprite.worldSize.x * UNLOCK_MOVE_AMOUNT_PERCENT;

        boardTrainQueue = new NPCQueue();
        disembarkTrainQueue = new NPCQueue();
        boardTrainQueue.npcs = new PassengerBrain[MAX_QUEUE_SIZE];
        disembarkTrainQueue.npcs = new PassengerBrain[MAX_QUEUE_SIZE];
        
        ResetDoors();
    }
    private void Update()
    {
        UpdateState();
    }
    private void SetState(State newState)
    {
        if (newState == curState) return;
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
                float t = moveTimer / audioData.slideDoorsOpening.length;
                t = Curves.EaseOutT(t, 2);
                rightSlideDoorPos.x = activeMoveAmount * t;
                leftSlideDoorPos.x = activeMoveAmount * -t;
                rightSlideDoorTransform.localPosition = rightSlideDoorPos;
                leftSlideDoorTransform.localPosition = leftSlideDoorPos;

                if (moveTimer >= audioData.slideDoorsOpening.length)
                {
                    SetState(State.Opened);
                }
            }
            break;

            case State.Opened:
            {
                if (disembarkTrainQueue.passengerCount > 0)
                {
                    disembarkTrainQueue.timer += Time.deltaTime;
                    if (disembarkTrainQueue.timer > QUEUE_TICK_RATE)
                    {
                        PassengerBrain npc = disembarkTrainQueue.npcs[disembarkTrainQueue.passengerCount - 1];
                        npc.DisembarkTrain();
                        disembarkTrainQueue.passengerCount--;
                        disembarkTrainQueue.timer = 0;
                    }
                }
                else if (boardTrainQueue.passengerCount > 0)
                {
                    boardTrainQueue.timer += Time.deltaTime;

                    if (boardTrainQueue.timer > QUEUE_TICK_RATE)
                    {
                        PassengerBrain npc = boardTrainQueue.npcs[boardTrainQueue.passengerCount - 1];
                        npc.BoardTrain();

                        boardTrainQueue.passengerCount--;
                        boardTrainQueue.timer = 0;
                    }
                }
            }
            break;

            case State.Closing:
            {
                moveTimer -= Time.deltaTime;
                float t = moveTimer/ audioData.slideDoorsClosing.length;
                t = Curves.EaseOutT(t, 2);

                rightSlideDoorPos.x = activeMoveAmount * t;
                leftSlideDoorPos.x = activeMoveAmount * -t;

                rightSlideDoorTransform.localPosition = rightSlideDoorPos;
                leftSlideDoorTransform.localPosition = leftSlideDoorPos;

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
                audioSource.volume = audioData.soundEffectsVolume;
                audioSource.PlayOneShot(audioData.slideDoorsReadyToOpen);
            }
            break;

            case State.Opening:
            {
                rightSlideDoorPos = rightSlideDoorTransform.localPosition;
                leftSlideDoorPos = leftSlideDoorTransform.localPosition;
                moveTimer = 0;

                audioSource.volume = audioData.soundEffectsVolume;
                audioSource.PlayOneShot(audioData.slideDoorsOpening);
            }
            break;

            case State.Opened:
            {
                disembarkTrainQueue.timer = 0;
                boardTrainQueue.timer = 0;

                rightSlideDoorPos.x = activeMoveAmount;
                leftSlideDoorPos.x = -activeMoveAmount;
                rightSlideDoorTransform.localPosition = rightSlideDoorPos;
                leftSlideDoorTransform.localPosition = leftSlideDoorPos;

                trainData.slideDoorsAmountOpened++;
            }
            break;

            case State.Closing:
            {
                rightSlideDoorPos = rightSlideDoorTransform.localPosition;
                leftSlideDoorPos = leftSlideDoorTransform.localPosition;
                moveTimer = audioData.slideDoorsClosing.length;


                audioSource.volume = audioData.soundEffectsVolume;
                audioSource.PlayOneShot(audioData.slideDoorsClosing);
            }
            break;

            case State.Locked:
            {

                rightSlideDoorPos.x = 0;
                leftSlideDoorPos.x = 0;
                rightSlideDoorTransform.localPosition = rightSlideDoorPos;
                leftSlideDoorTransform.localPosition = leftSlideDoorPos;
                if (prevState == State.Closing)
                {
                    trainData.slideDoorsAmountOpened--;
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
    public void AddToBoardTrainQueue(PassengerBrain passenger)
    {
        passenger.boardTrainQueueIndex = boardTrainQueue.passengerCount;
        boardTrainQueue.npcs[boardTrainQueue.passengerCount] = passenger;
        boardTrainQueue.passengerCount++;
    }
    public void AddToDisembarkTrainQueue(PassengerBrain passenger)
    {
        passenger.disembarkTrainQueueIndex = disembarkTrainQueue.passengerCount;
        disembarkTrainQueue.npcs[disembarkTrainQueue.passengerCount] = passenger;
        disembarkTrainQueue.passengerCount++;
    }
    public void ResetDoors()
    {
        curState = State.Locked;
        rightSlideDoorTransform.localPosition = new Vector3(0, 0, rightSlideDoorTransform.localPosition.z);
        leftSlideDoorTransform.localPosition = new Vector3(0, 0, leftSlideDoorTransform.localPosition.z);
    }
}
