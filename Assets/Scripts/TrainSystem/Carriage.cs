using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Carriage : MonoBehaviour
{
    [SerializeField] TrainStatsSO trainStats;
    [SerializeField] TrainSettingsSO trainSettings;
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] LayerSettingsSO layerSettings;
    [SerializeField] MaterialIDSO materialIDs;
    [SerializeField] Transform[] wheelTransforms;
    [SerializeField] SpriteRenderer[] exteriorSprites;
    public BoxCollider2D carriageCollider;
    public BoxCollider2D insideBoundsCollider;
    public SlideDoors[] exteriorSlideDoors;
    public SlideDoors[] interiorSlideDoors;


    [Serializable] public struct MaterialProps
    {
        public float alpha;
    }
    [SerializeField] MaterialProps materialProps;

    [Serializable] public struct ChairData
    {
        internal float xPos;
        internal bool filled;
    }
    internal ChairData[] chairData;
    internal float chairZPos;

    [Serializable] public struct SmokersRoomData
    {
        internal float minXPos;
        internal float maxXPos;
        internal int npcCount;
    }
    MaterialPropertyBlock mpb;
    CancellationTokenSource ctsFade;

    internal SmokersRoomData[] smokersRoomData;
    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        ctsFade = new CancellationTokenSource();
        materialProps.alpha = 1;
    }

    private void OnEnable()
    {
        gameEventData.OnTrainArrivedAtStartPosition.RegisterListener(GetData);
        gameEventData.OnStationArrival.RegisterListener(UnlockDoors);
        gameEventData.OnStationLeave.RegisterListener(CloseDoors);

    }

    private void OnDisable()
    {
        gameEventData.OnTrainArrivedAtStartPosition.UnregisterListener(GetData);
        gameEventData.OnStationArrival.UnregisterListener(UnlockDoors);
        gameEventData.OnCloseSlideDoors.UnregisterListener(CloseDoors);
    }

    private void Update()
    {
        if (trainStats.wheelCircumference <= 0f) return;

        float wheelRotation = (trainStats.metersTravelled / trainStats.wheelCircumference) * 360f;

        wheelRotation %= 360f;

        foreach (Transform wheel in wheelTransforms)
        {
            wheel.localRotation = Quaternion.Euler(0f, 0f, -wheelRotation);
        }
    }
    [ContextMenu("Get Chair Data")]
    private void GetData()
    {
        GetChairData(carriageCollider.bounds);
        GetSmokersRoomData(carriageCollider.bounds);
    }
    private void GetChairData(Bounds checkBounds)
    {
        Collider2D[] chairsHits = Physics2D.OverlapBoxAll(checkBounds.center, checkBounds.size, 0f, layerSettings.trainLayerStruct.carriageChairs);
        float tileWidth = trainSettings.chairSprite.bounds.size.x * 0.333333f;

        List<ChairData> chairDataList = new List<ChairData>();
        for (int i = 0; i < chairsHits.Length; i++)
        {
            SpriteRenderer chairRenderer = chairsHits[i].GetComponent<SpriteRenderer>();
            float totalWidth = chairRenderer.size.x;

            int chairAmount = Mathf.RoundToInt(totalWidth / tileWidth);
            float firstChairPos = chairsHits[i].transform.position.x + (tileWidth * 0.5f);
            for (int j = 0; j < chairAmount; j++)
            {
                chairDataList.Add(new ChairData { xPos = firstChairPos + (tileWidth * j), filled = false });
            }
        }
        chairData = chairDataList.ToArray();
        chairZPos = chairsHits[0].transform.position.z - 1;
    }
    private void GetSmokersRoomData(Bounds checkBounds)
    {
        Collider2D[] smokersRoomHits = Physics2D.OverlapBoxAll(checkBounds.center, checkBounds.size, 0, layerSettings.trainLayerStruct.smokingRoom);

        List<SmokersRoomData> smokersRoomDataList = new List<SmokersRoomData>();

        for (int i = 0; i < smokersRoomHits.Length; i++)
        {
            Collider2D col = smokersRoomHits[i];
            smokersRoomDataList.Add(new SmokersRoomData { minXPos = col.bounds.min.x, maxXPos = col.bounds.max.x });
        }
        smokersRoomData = smokersRoomDataList.ToArray();
    }
    private void UnlockDoors()
    {
        if (trainStats.curStation.isFrontOfTrain)
        {
            for (int i = 0; i < exteriorSlideDoors.Length; i++)
            {
                exteriorSlideDoors[i].UnlockDoors();
            }
        }
        else
        {
            for (int i = 0; i < interiorSlideDoors.Length; i++)
            {
                interiorSlideDoors[i].UnlockDoors();
            }
        }
    }

    private void CloseDoors()
    {
        for(int i = 0; i < exteriorSlideDoors.Length; i++)
        {
            exteriorSlideDoors[i].CloseDoors();
        }

        for (int i = 0;i < interiorSlideDoors.Length; i++)
        {
            interiorSlideDoors[i].CloseDoors();
        }
    }
    public void StartFade(bool fadeIn)
    {
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();

        Fade(fadeIn).Forget();

    }
    private async UniTask Fade(bool fadeIn)
    {
        float elaspedTime = materialProps.alpha * trainSettings.exteriorWallFadeTime;
        try
        {
            while (fadeIn ? elaspedTime < trainSettings.exteriorWallFadeTime : elaspedTime > 0f)
            {

                elaspedTime += (fadeIn ? Time.deltaTime : -Time.deltaTime);

                materialProps.alpha = elaspedTime / trainSettings.exteriorWallFadeTime;
                mpb.SetFloat(materialIDs.ids.alpha, materialProps.alpha);
                for (int i = 0; i < exteriorSprites.Length; i++)
                {
                    exteriorSprites[i].SetPropertyBlock(mpb);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ctsFade.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

#if UNITY_EDITOR

    [ContextMenu("Reset Doors")]
    private void ResetDoors()
    {
        foreach(SlideDoors slideDoor in  exteriorSlideDoors)
        {
            slideDoor.ResetDoors();
        }
    }
    private void OnDrawGizmos()
    {
        if (trainSettings == null) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y + 2, trainSettings.maxMinWorldZPos.min), new Vector3(transform.position.x, transform.position.y + 2, trainSettings.maxMinWorldZPos.max));
    }
#endif
}
