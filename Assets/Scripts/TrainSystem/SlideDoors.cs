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
    [Serializable] public struct Stats
    {
        internal State curState;
    }
    public Stats stats;
    [Serializable] public struct SOData
    {
        public TrainStatsSO trainStats;
        public TrainSettingsSO trainSettings;
    }
    [SerializeField] SOData soData;

    [Serializable] public struct ComponentData
    {
        public SpriteRenderer[] sliderDoors;
        public BoxCollider2D collider;
    }
    [SerializeField] ComponentData componentData;


    private void Awake()
    {
        ResetDoors();
    }
    private void Start()
    {
        stats.curState = State.Locked;
        componentData.collider.enabled = false;
    }
    public void UnlockDoors()
    {
        componentData.collider.enabled = true;
        MoveDoors(moveAmount: soData.trainSettings.slideDoorSprite.bounds.size.x * 0.01f, moveTime: 0.3f, State.Unlocked).Forget();
    }

    public void OpenDoors()
    {
        stats.curState = State.Opening;
        MoveDoors(moveAmount: soData.trainSettings.slideDoorSprite.bounds.size.x * 0.99f, moveTime: soData.trainSettings.doorMoveTime, State.Opened).Forget();
    }

    public void CloseDoors()
    {
        if (stats.curState == State.Unlocked) return;
        stats.curState = State.Closing;
        MoveDoors(moveAmount: -soData.trainSettings.slideDoorSprite.bounds.size.x, moveTime: soData.trainSettings.doorMoveTime, State.Locked).Forget();
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

        stats.curState = newState;
    }
    public void ResetDoors()
    {
        stats.curState = State.Locked;
        for (int i = 0; i < componentData.sliderDoors.Length; i++)
        {
            float xPos = soData.trainSettings.slideDoorSprite.bounds.size.x * -componentData.sliderDoors[i].transform.localScale.x;
            componentData.sliderDoors[i].transform.localPosition = new Vector3(xPos, 0, 0);
        }
    }
}
