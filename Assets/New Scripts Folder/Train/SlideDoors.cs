using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;

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

    [Serializable] public struct SOData
    {
        public TrainStatsSO trainStats;
        public TrainSettingsSO trainSettings;
    }
    [SerializeField] SOData soData;
    [Serializable] public struct GameEventData
    {
        public GameEvent OnReset;
        public GameEvent OnUnlockSlideDoors;
    }
    [SerializeField] GameEventData gameEventData;

    [Serializable] public struct ComponentData
    {
        public SpriteRenderer[] sliderDoors;
    }
    [SerializeField] ComponentData componentData;

    [Serializable] public struct StateData
    {
       public State curState;
    }
    public StateData stateData;

    private void Awake()
    {
        ResetDoors();
    }
    private void OnEnable()
    {
        gameEventData.OnUnlockSlideDoors.RegisterListener(UnlockDoors);
        gameEventData.OnReset.RegisterListener(ResetDoors);
    }

    private void OnDisable()
    {
        gameEventData.OnUnlockSlideDoors.UnregisterListener(UnlockDoors);
        gameEventData.OnReset.UnregisterListener(ResetDoors);
    }

    private void UnlockDoors()
    {
        MoveDoors(moveAmount: soData.trainSettings.slideDoorSprite.bounds.size.x * 0.01f, moveTime: 0.3f, State.Unlocked).Forget();
    }

    public void OpenDoors()
    {
        stateData.curState = State.Opening;
        MoveDoors(moveAmount: soData.trainSettings.slideDoorSprite.bounds.size.x * 0.99f, moveTime: soData.trainStats.doorMovingTime, State.Opened).Forget();
    }
    private async UniTaskVoid MoveDoors(float moveAmount, float moveTime, State newState)
    {
        float elapsedTime = 0.0f;

        Vector3[] doorStartPos = new Vector3[componentData.sliderDoors.Length];
        for (int i = 0; i < doorStartPos.Length; i++)
        {
            doorStartPos[i] = componentData.sliderDoors[i].transform.localPosition;
        }

        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveTime;

            for (int i = 0; i < componentData.sliderDoors.Length; i++)
            {
                float xPos = doorStartPos[i].x + (moveAmount * -componentData.sliderDoors[i].transform.localScale.x * t);
                componentData.sliderDoors[i].transform.localPosition = new Vector3(xPos, doorStartPos[i].y, doorStartPos[i].z);
            }
            await UniTask.Yield();
        }

        stateData.curState = newState;
    }

    private void ResetDoors()
    {
        stateData.curState = State.Locked;
        for (int i = 0; i < componentData.sliderDoors.Length; i++)
        {
            float xPos = soData.trainSettings.slideDoorSprite.bounds.size.x * -componentData.sliderDoors[i].transform.localScale.x;
            componentData.sliderDoors[i].transform.localPosition = new Vector3(xPos, 0, 0);
        }
    }
}
