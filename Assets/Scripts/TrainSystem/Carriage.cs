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
    public AtlasRenderer[] exteriorRenderers;
    public AtlasRenderer[] chairRenderers;
    public AtlasRenderer[] grapPoleRenderers;
    public BoxCollider2D insideBoundsCollider;

    public BoxCollider2D smokingRoomCollider_right;
    public BoxCollider2D smokingRoomCollider_left;

    public SlideDoors[] exteriorSlideDoors;
    public SlideDoors[] interiorSlideDoors;

    public float alpha;


    [Header("Generated")]
    public ChairData[] chairData;
    public SmokersRoomData[] smokersRoomData;
    public int sittingDepth;
    public int standingDepth;

    private CancellationTokenSource ctsFade;
    private void Awake()
    {
        ctsFade = new CancellationTokenSource();
    }

    private void OnEnable()
    {
        gameEventData.OnTrainArrivedAtStartPosition.RegisterListener(GetSeatData);
        gameEventData.OnStationArrival.RegisterListener(UnlockDoors);
        gameEventData.OnStationLeave.RegisterListener(CloseDoors);

    }

    private void OnDisable()
    {
        gameEventData.OnTrainArrivedAtStartPosition.UnregisterListener(GetSeatData);
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

    private void GetSeatData()
    {
        AtlasRenderer seatRenderer = chairRenderers[0];
        float tileWidth = seatRenderer.atlas.slicedSprites[seatRenderer.spriteIndex].worldSlices.x;

        List<ChairData> seatDataList = new List<ChairData>();
        for (int i = 0; i < chairRenderers.Length; i++)
        {
            AtlasRenderer seat = chairRenderers[i];

            float totalWidth = seat.worldWidth;
            int chairAmount = Mathf.RoundToInt(totalWidth / tileWidth);
            float firstChairPos = seat.transform.position.x + (tileWidth * 0.5f);

            for (int j = 0; j < chairAmount; j++)
            {
                ChairData chairData = new ChairData { xPos = firstChairPos + (tileWidth * j), filled = false };
                seatDataList.Add(chairData);
            }
        }
        chairData = seatDataList.ToArray();
        sittingDepth = seatRenderer.depthOrder - 1;
        standingDepth = sittingDepth - 1;
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
    public void FadeIn()
    {
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();

        FadingIn().Forget();

    }

    public void FadeOut()
    {
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();

        FadingOut().Forget();
    }
    private async UniTask FadingIn()
    {
        float elaspedTime = alpha * trainSettings.exteriorWallFadeTime;
        try
        {
            while (elaspedTime < trainSettings.exteriorWallFadeTime)
            {

                elaspedTime += Time.deltaTime;

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

    private async UniTask FadingOut()
    {
        float elaspedTime = alpha * trainSettings.exteriorWallFadeTime;
        try
        {
            while (elaspedTime > 0)
            {

                elaspedTime -= Time.deltaTime;

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
