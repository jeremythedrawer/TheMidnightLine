using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;
using static Atlas;

public class SlideDoors : MonoBehaviour
{
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
    private void Start()
    {
        curState = State.Locked;
        boxCollider.enabled = false;
        rightSlideDoor_transform = rightSlideDoorRenderer.transform;
        leftSlideDoor_transform = leftSlideDoorRenderer.transform;
    }

    private void OnDisable()
    {
        ResetDoors();
    }
    public void UnlockDoors()
    {
        boxCollider.enabled = true;
        MovingDoors(moveAmount: rightSlideDoorRenderer.sprite.worldSize.x * 0.01f, moveTime: 0.3f, State.Unlocked).Forget();
    }

    public void OpenDoors()
    {
        curState = State.Opening;
        MovingDoors(moveAmount: rightSlideDoorRenderer.sprite.worldSize.x * 0.99f, moveTime: trainSettings.doorMoveTime, State.Opened).Forget();
    }

    public void CloseDoors()
    {
        curState = State.Closing;
        MovingDoors(moveAmount: rightSlideDoorRenderer.sprite.worldSize.x, moveTime: trainSettings.doorMoveTime, State.Locked).Forget();
    }
    private async UniTaskVoid MovingDoors(float moveAmount, float moveTime, State newState)
    {
        float elapsedTime = 0;
        Vector3 rightSlideDoorPos = rightSlideDoor_transform.position;
        Vector3 leftSlideDoorPos = leftSlideDoor_transform.position;
        
        float moveSign = newState == State.Opened ? 1 : -1;

        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveTime;

            rightSlideDoorPos.x = moveAmount * t * moveSign;
            leftSlideDoorPos.x = -moveAmount * t * moveSign;
            rightSlideDoor_transform.localPosition = rightSlideDoorPos;
            leftSlideDoor_transform.localPosition = leftSlideDoorPos;
            await UniTask.Yield();
        }

        curState = newState;
    }
    public void ResetDoors()
    {
        curState = State.Locked;
        rightSlideDoor_transform.localPosition = Vector3.zero;
        leftSlideDoor_transform.localPosition = Vector3.zero;
    }
}
