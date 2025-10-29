using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;

public class Carriage : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public CarriageSO stats;
        public TrainStatsSO trainStats;
    }
    [SerializeField] SOData soData;

    [Serializable] public struct ComponentData
    {
        public Transform[] wheelTransforms;
        public Transform[] slideDoorTransforms;
    }
    [SerializeField] ComponentData componentData;
    [Serializable]
    public struct GameEventData
    {
        public GameEvent OnReset;
        public GameEvent OnOpenSlideDoors;
    }
    [SerializeField] GameEventData gameEventData;

    private void OnEnable()
    {
        ResetStats();
        gameEventData.OnReset.RegisterListener(ResetStats);
        gameEventData.OnOpenSlideDoors.RegisterListener(OpenDoors);
    }

    private void OnDisable()
    {
        gameEventData.OnReset.UnregisterListener(ResetStats);
        gameEventData.OnOpenSlideDoors.UnregisterListener(OpenDoors);
    }

    private void Start()
    {

    }
    private void Update()
    {
        float metersMoved = soData.trainStats.curMPerSec * Time.deltaTime;
        float wheelRotation = -metersMoved * soData.stats.degPerMeter;

        foreach (Transform wheel in componentData.wheelTransforms)
        {
            wheel.transform.Rotate(0, 0, wheelRotation);
        }
    }

    private void OnApplicationQuit()
    {
        ResetStats();
    }

    private void ResetStats()
    {
        soData.stats.degPerMeter = 360.0f / ((soData.stats.wheelSprite.rect.size.x / soData.stats.wheelSprite.pixelsPerUnit) * Mathf.PI);
    }

    private void OpenDoors()
    {
        OpeningDoors().Forget();
    }
    private async UniTaskVoid OpeningDoors()
    {
        float elapsedTime = 0.0f;
        Debug.Log(soData.stats.slideDoorSprite.bounds.size.x);

        Vector3[] doorStartPos = new Vector3[componentData.slideDoorTransforms.Length];
        for (int i = 0; i < componentData.slideDoorTransforms.Length; i++)
        {
            doorStartPos[i] = componentData.slideDoorTransforms[i].localPosition;
        }

        while (elapsedTime < soData.stats.doorMovingTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / soData.stats.doorMovingTime;

            for (int i = 0; i < componentData.slideDoorTransforms.Length; i++)
            {
                float xPos = doorStartPos[i].x + (soData.stats.slideDoorSprite.bounds.size.x * -componentData.slideDoorTransforms[i].localScale.x * t);
                componentData.slideDoorTransforms[i].localPosition = new Vector3(xPos, doorStartPos[i].y, doorStartPos[i].z);
            }
            await UniTask.Yield();
        }
    }
}
