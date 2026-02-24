using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Carriage : MonoBehaviour
{
    [Serializable] public struct ChairData
    {
        public float xPos;
        public bool filled;
    }
    [Serializable] public struct SmokersRoomData
    {
        internal float minXPos;
        internal float maxXPos;
        internal int npcCount;
    }

    [SerializeField] TrainStatsSO trainStats;
    [SerializeField] TrainSettingsSO trainSettings;
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] LayerSettingsSO layerSettings;
    [SerializeField] MaterialIDSO materialIDs;
    [SerializeField] Transform[] wheelTransforms;
    [SerializeField] AtlasRenderer[] exteriorRenderers;
    public BoxCollider2D carriageCollider;
    public BoxCollider2D insideBoundsCollider;

    public BoxCollider2D rightSmokingRoomCollider;
    public BoxCollider2D leftSmokingRoomCollider;

    public SlideDoors[] exteriorSlideDoors;
    public SlideDoors[] interiorSlideDoors;

    public float alpha;


    [Header("Generated")]
    public ChairData[] chairData;
    public SmokersRoomData[] smokersRoomData;
    public float chairZPos;

    private CancellationTokenSource ctsFade;
    private void Awake()
    {
        ctsFade = new CancellationTokenSource();
    }

    private void OnEnable()
    {
        //gameEventData.OnTrainArrivedAtStartPosition.RegisterListener(GetChairData);
        gameEventData.OnStationArrival.RegisterListener(UnlockDoors);
        gameEventData.OnStationLeave.RegisterListener(CloseDoors);

    }

    private void OnDisable()
    {
        //gameEventData.OnTrainArrivedAtStartPosition.UnregisterListener(GetChairData);
        gameEventData.OnStationArrival.UnregisterListener(UnlockDoors);
        gameEventData.OnCloseSlideDoors.UnregisterListener(CloseDoors);
    }

    private void Start()
    {
        alpha = 1;
    }
    private void Update()
    {
        if (trainStats.wheelCircumference <= 0f) return;

        float wheelRotation = (trainStats.metersTravelled / trainStats.wheelCircumference) * 360f;

        foreach (Transform wheel in wheelTransforms)
        {
            wheel.localRotation = Quaternion.Euler(0f, 0f, -wheelRotation);
        }
    }

    //private void GetChairData()
    //{
    //    Collider2D[] chairsHits = Physics2D.OverlapBoxAll(insideBoundsCollider.bounds.center, insideBoundsCollider.bounds.size, 0f, layerSettings.trainLayerStruct.carriageChairs);
    //   //float tileWidth = trainSettings.chairSprite.bounds.size.x * 0.333333f;

    //    List<ChairData> chairDataList = new List<ChairData>();
    //    for (int i = 0; i < chairsHits.Length; i++)
    //    {
    //        SpriteRenderer chairRenderer = chairsHits[i].GetComponent<SpriteRenderer>();
    //        float totalWidth = chairRenderer.size.x;

    //        //int chairAmount = Mathf.RoundToInt(totalWidth / tileWidth);
    //        //float firstChairPos = chairsHits[i].transform.position.x + (tileWidth * 0.5f);
    //        //for (int j = 0; j < chairAmount; j++)
    //        //{
    //           // chairDataList.Add(new ChairData { xPos = firstChairPos + (tileWidth * j), filled = false });
    //        //}
    //    }
    //    chairData = chairDataList.ToArray();
    //    chairZPos = chairsHits[0].transform.position.z - 1;
    //}
    private void UnlockDoors()
    {
        if (trainStats.curStation.stationPrefab.platformRenderer.depthOrder < exteriorRenderers[0].depthOrder)
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
        float elaspedTime = alpha * trainSettings.exteriorWallFadeTime;
        try
        {
            while (fadeIn ? elaspedTime < trainSettings.exteriorWallFadeTime : elaspedTime > 0f)
            {

                elaspedTime += (fadeIn ? Time.deltaTime : -Time.deltaTime);

                alpha = elaspedTime / trainSettings.exteriorWallFadeTime;
                for (int i = 0; i < exteriorRenderers.Length; i++)
                {
                    exteriorRenderers[i].mpb.SetFloat(materialIDs.ids.alpha, alpha);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ctsFade.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
