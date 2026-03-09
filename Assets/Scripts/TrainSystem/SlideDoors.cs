using Cysharp.Threading.Tasks;
using UnityEngine;

public class SlideDoors : MonoBehaviour
{
    const float UNLOCK_MOVE_AMOUNT_PERCENT = 0.01f;
    const float OPEN_MOVE_AMOUNT_PERCENT = 0.99f;
    const float UNLOCK_MOVE_TIME = 0.3f;
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
    public BoxCollider2D boxCollider;

    public AtlasRenderer rightSlideDoorRenderer;
    public AtlasRenderer leftSlideDoorRenderer;
    [Header("Generated")]
    public State curState;
    public Transform rightSlideDoor_transform;
    public Transform leftSlideDoor_transform;

    private float openMoveAmount;
    private float unlockMoveAmount;
    private void Start()
    {
        ResetDoors();
        curState = State.Locked;
        boxCollider.enabled = false;
        rightSlideDoor_transform = rightSlideDoorRenderer.transform;
        leftSlideDoor_transform = leftSlideDoorRenderer.transform;
        openMoveAmount = rightSlideDoorRenderer.sprite.worldSize.x * OPEN_MOVE_AMOUNT_PERCENT;
        unlockMoveAmount = rightSlideDoorRenderer.sprite.worldSize.x * UNLOCK_MOVE_AMOUNT_PERCENT;
    }

    private void OnDisable()
    {
        ResetDoors();
    }
    private void OnApplicationQuit()
    {
        ResetDoors();
    }
    public void UnlockDoors()
    {
        boxCollider.enabled = true;
        OpeningDoors(unlockMoveAmount, UNLOCK_MOVE_TIME, State.Unlocked).Forget();
    }

    public void OpenDoors()
    {
        curState = State.Opening;
        OpeningDoors(openMoveAmount, trainSettings.doorMoveTime, State.Opened).Forget();
    }

    public void CloseDoors()
    {
        boxCollider.enabled = false;
        if (curState == State.Opened)
        {
            ClosingDoors(rightSlideDoorRenderer.sprite.worldSize.x, trainSettings.doorMoveTime, State.Locked).Forget();
        }
        else
        {
            ClosingDoors(unlockMoveAmount, trainSettings.doorMoveTime, State.Locked).Forget();
        }
        curState = State.Closing;
    }

    private async UniTaskVoid OpeningDoors(float moveAmount, float moveTime, State newState)
    {
        float elapsedTime = 0;
        Vector3 rightSlideDoorPos = rightSlideDoor_transform.localPosition;
        Vector3 leftSlideDoorPos = leftSlideDoor_transform.localPosition;

        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveTime;

            rightSlideDoorPos.x = moveAmount * t;
            leftSlideDoorPos.x = moveAmount * -t;
            rightSlideDoor_transform.localPosition = rightSlideDoorPos;
            leftSlideDoor_transform.localPosition = leftSlideDoorPos;
            await UniTask.Yield();
        }

        rightSlideDoorPos.x = moveAmount;
        leftSlideDoorPos.x = -moveAmount;
        rightSlideDoor_transform.localPosition = rightSlideDoorPos;
        leftSlideDoor_transform.localPosition = leftSlideDoorPos;

        curState = newState;
    }


    private async UniTaskVoid ClosingDoors(float moveAmount, float moveTime, State newState)
    {
        float elapsedTime = 0;
        Vector3 rightSlideDoorPos = rightSlideDoor_transform.localPosition;
        Vector3 leftSlideDoorPos = leftSlideDoor_transform.localPosition;

        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;
            float t = 1 - (elapsedTime / moveTime);

            rightSlideDoorPos.x = moveAmount * t;
            leftSlideDoorPos.x = moveAmount * -t;
            rightSlideDoor_transform.localPosition = rightSlideDoorPos;
            leftSlideDoor_transform.localPosition = leftSlideDoorPos;
            await UniTask.Yield();
        }

        rightSlideDoorPos.x = 0;
        leftSlideDoorPos.x = 0;
        rightSlideDoor_transform.localPosition = rightSlideDoorPos;
        leftSlideDoor_transform.localPosition = leftSlideDoorPos;

        curState = newState;
    }
    public void ResetDoors()
    {
        curState = State.Locked;
        rightSlideDoor_transform.localPosition = Vector3.zero;
        leftSlideDoor_transform.localPosition = Vector3.zero;
    }
}
