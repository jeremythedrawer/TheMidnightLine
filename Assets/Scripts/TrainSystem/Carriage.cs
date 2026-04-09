using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Train;

public class Carriage : MonoBehaviour
{
    public TrainStatsSO trainStats;
    public TrainSettingsSO trainSettings;
    public TripSO trip;
    public GameEventDataSO gameEventData;
    public LayerSettingsSO layerSettings;
    public MaterialIDSO materialIDs;

    public AtlasRenderer nextStationSignRenderer;

    public AtlasRenderer[] wheelRenderers;
    public AtlasRenderer[] exteriorRenderers;
    public AtlasRenderer[] seatRenderers;
    public AtlasRenderer[] grapPoleRenderers;

    public BoxCollider2D insideBoundsCollider;
    public BoxCollider2D smokingRoomCollider_right;
    public BoxCollider2D smokingRoomCollider_left;

    public SlideDoors[] exteriorSlideDoors;
    public SlideDoors[] interiorSlideDoors;

    [Header("Generated")]
    public float wheelCircumference;
    public float alpha;
    public SeatData seatData;
    public SmokersRoomData[] smokersRoomData;
    public Transform[] wheelTransforms;
    public CancellationTokenSource ctsFade;
    public float prevMeters;
    public int seatAmount;
    private void Awake()
    {
        ctsFade = new CancellationTokenSource();
    }
    private void OnEnable()
    {
        gameEventData.OnTrainArrivedAtStartPosition.RegisterListener(SetSeatData);
        gameEventData.OnTrainArrivedAtStartPosition.RegisterListener(SetSmokerRoomData);

    }
    private void OnDisable()
    {
        gameEventData.OnTrainArrivedAtStartPosition.UnregisterListener(SetSeatData);
        gameEventData.OnTrainArrivedAtStartPosition.UnregisterListener(SetSmokerRoomData);
    }
    private void Start()
    {
        alpha = 0;
        wheelCircumference = wheelRenderers[0].sprite.worldSize.x * Mathf.PI;
        wheelTransforms = new Transform[wheelTransforms.Length];
        for (int i = 0; i < wheelRenderers.Length; i++)
        {
            wheelTransforms[i] = wheelRenderers[i].transform;
        }
    }
    private void Update()
    {
        //float wheelRotation = (trainStats.totalTicketsChecked / wheelCircumference) * 360f;
        //wheelRotation %= 360;
        //wheelRotation = -wheelRotation;
        //foreach (Transform wheel in wheelTransforms)
        //{
        //    wheel.localRotation = Quaternion.Euler(0f, 0f, wheelRotation);
        //}
    }
    public void UnlockInteriorDoors()
    {
        for (int i = 0; i < interiorSlideDoors.Length; i++)
        {
            interiorSlideDoors[i].UnlockDoors();
        }
    }
    public void UnlockExteriorSlideDoors()
    {
        for (int i = 0; i < exteriorSlideDoors.Length; i++)
        {
            exteriorSlideDoors[i].UnlockDoors();
        }
    }
    public void CloseInteriorSlideDoors()
    {
        for (int i = 0; i < interiorSlideDoors.Length; i++)
        {
            interiorSlideDoors[i].CloseDoors();
        }
    }
    public void CloseExteriorSlideDoors()
    {
        for (int i = 0; i < exteriorSlideDoors.Length; i++)
        {
            exteriorSlideDoors[i].CloseDoors();
        }
    }
    public void SetSignToNextStation(string stationName)
    {
        string text = "Next Station is " + stationName;
        nextStationSignRenderer.SetText(text);
    }
    public void SetSignToCurrentStation(string stationName)
    {
        nextStationSignRenderer.SetText(stationName);
    }
    public void MoveUp()
    {
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();

        MovingUp().Forget();

    }
    public void MoveDown()
    {
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();

        MovingDown().Forget();
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
        AtlasRenderer seatRenderer = seatRenderers[0];
        float tileWidth = seatRenderer.atlas.slicedSprites[seatRenderer.spriteIndex].worldSlices.x;

        seatAmount = 0;
        int[] seatsPerRenderer = new int[seatRenderers.Length];
        for (int i = 0; i < seatRenderers.Length; i++)
        {
            AtlasRenderer seat = seatRenderers[i];

            float totalWidth = seat.bounds.size.x;
            int seats = Mathf.RoundToInt(totalWidth / tileWidth);
            seatAmount += seats;
            seatsPerRenderer[i] = seats;
        }
        seatData.xPos = new float[seatAmount];
        seatData.filled = new bool[seatAmount];

        int seatIndex = 0;
        for (int i = 0; i < seatRenderers.Length; i++)
        {
            AtlasRenderer seat = seatRenderers[i];
            float firstSeatPos = seat.transform.position.x + (tileWidth * 0.5f);

            for (int j = 0; j < seatsPerRenderer[i]; j++)
            {
                seatData.xPos[seatIndex] = firstSeatPos + (tileWidth * j);
                seatIndex++;
            }
        }
    }
    private async UniTask MovingDown()
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
                    exteriorRenderers[i].custom.x = alpha;
                }

                await UniTask.Yield(cancellationToken: ctsFade.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
    private async UniTask MovingUp()
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
                    exteriorRenderers[i].custom.x = alpha;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ctsFade.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
