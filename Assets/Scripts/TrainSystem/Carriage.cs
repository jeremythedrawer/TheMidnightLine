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
        public float minXPos;
        public float maxXPos;
        public int npcCount;
    }

    [SerializeField] TrainStatsSO trainStats;
    [SerializeField] TrainSettingsSO trainSettings;
    [SerializeField] TripSO trip;
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] LayerSettingsSO layerSettings;
    [SerializeField] MaterialIDSO materialIDs;
    [SerializeField] AtlasSimpleRenderer[] wheelRenderers;
    public AtlasSimpleRenderer[] exteriorRenderers;
    public AtlasSimpleRenderer[] chairRenderers;
    public AtlasSimpleRenderer[] grapPoleRenderers;
    public BoxCollider2D insideBoundsCollider;

    public BoxCollider2D smokingRoomCollider_right;
    public BoxCollider2D smokingRoomCollider_left;

    public SlideDoors[] exteriorSlideDoors;
    public SlideDoors[] interiorSlideDoors;

    public float alpha;
    public float wheelCircumference;

    [Header("Generated")]
    public ChairData[] chairData;
    public SmokersRoomData[] smokersRoomData;
    public int sittingDepth;
    public int standingDepth;
    public Transform[] wheelTransforms;
    public CancellationTokenSource ctsFade;
    public float prevMeters;
    private void Awake()
    {
        ctsFade = new CancellationTokenSource();
    }

    private void OnEnable()
    {
        gameEventData.OnTrainArrivedAtStartPosition.RegisterListener(SetSeatData);
        gameEventData.OnTrainArrivedAtStartPosition.RegisterListener(SetSmokerRoomData);
        gameEventData.OnStationArrival.RegisterListener(UnlockDoors);
        gameEventData.OnCloseSlideDoors.RegisterListener(CloseDoors);

    }

    private void OnDisable()
    {
        gameEventData.OnTrainArrivedAtStartPosition.UnregisterListener(SetSeatData);
        gameEventData.OnTrainArrivedAtStartPosition.UnregisterListener(SetSmokerRoomData);
        gameEventData.OnStationArrival.UnregisterListener(UnlockDoors);
        gameEventData.OnCloseSlideDoors.UnregisterListener(CloseDoors);
    }

    private void Start()
    {
        alpha = 1;
        wheelCircumference = wheelRenderers[0].sprite.worldSize.x * Mathf.PI;
        wheelTransforms = new Transform[wheelTransforms.Length];
        for (int i = 0; i < wheelRenderers.Length; i++)
        {
            wheelTransforms[i] = wheelRenderers[i].transform;
        }
    }
    private void Update()
    {
        float wheelRotation = (trainStats.metersTravelled / wheelCircumference) * 360f;
        wheelRotation %= 360;
        wheelRotation = -wheelRotation;
        foreach (Transform wheel in wheelTransforms)
        {
            wheel.localRotation = Quaternion.Euler(0f, 0f, wheelRotation);
        }
    }

    private void SetSmokerRoomData()
    {
        smokersRoomData = new SmokersRoomData[2];

        smokersRoomData[0].minXPos = smokingRoomCollider_left.bounds.min.x;
        smokersRoomData[0].maxXPos = smokingRoomCollider_left.bounds.max.x;
        smokersRoomData[1].minXPos = smokingRoomCollider_right.bounds.min.x;
        smokersRoomData[1].maxXPos = smokingRoomCollider_right.bounds.max.x;
    }

    private void SetSeatData()
    {
        AtlasSimpleRenderer seatRenderer = chairRenderers[0];
        float tileWidth = seatRenderer.atlas.slicedSprites[seatRenderer.spriteIndex].worldSlices.x;

        List<ChairData> seatDataList = new List<ChairData>();
        for (int i = 0; i < chairRenderers.Length; i++)
        {
            AtlasSimpleRenderer seat = chairRenderers[i];

            float totalWidth = seat.renderInput.widthHeightFlip[0].x;
            int chairAmount = Mathf.RoundToInt(totalWidth / tileWidth);
            float firstChairPos = seat.transform.position.x + (tileWidth * 0.5f);

            for (int j = 0; j < chairAmount; j++)
            {
                ChairData chairData = new ChairData { xPos = firstChairPos + (tileWidth * j), filled = false };
                seatDataList.Add(chairData);
            }
        }
        chairData = seatDataList.ToArray();
        sittingDepth = seatRenderer.renderInput.batchKey.depthOrder - 1;
        standingDepth = sittingDepth - 1;
    }
    private void UnlockDoors()
    {
        if (trip.curStation.isFrontOfTrain)
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
        if (trip.curStation.isFrontOfTrain)
        {
            for(int i = 0; i < exteriorSlideDoors.Length; i++)
            {
                exteriorSlideDoors[i].CloseDoors();
            }
        }
        else
        {
            for (int i = 0; i < interiorSlideDoors.Length; i++)
            {
                interiorSlideDoors[i].CloseDoors();
            }
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
                alpha = alpha < 0.5 ? 16 * alpha * alpha * alpha * alpha * alpha : 1 - Mathf.Pow(-2 * alpha + 2, 5) * 0.5f; 
                for (int i = 0; i < exteriorRenderers.Length; i++)
                {
                    exteriorRenderers[i].customMPB.SetFloat(materialIDs.ids.alpha, alpha);
                }

                await UniTask.Yield(cancellationToken: ctsFade.Token);
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
                alpha = alpha < 0.5 ? 16 * alpha * alpha * alpha * alpha * alpha : 1 - Mathf.Pow(-2 * alpha + 2, 5) * 0.5f;
                for (int i = 0; i < exteriorRenderers.Length; i++)
                {
                    exteriorRenderers[i].customMPB.SetFloat(materialIDs.ids.alpha, alpha);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ctsFade.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
