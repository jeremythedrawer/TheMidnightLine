using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Atlas;

public class RailMap : MonoBehaviour
{
    public const float MOVE_TIME = 3f;
    public const float APPEARING_TIME = 1f;

    public TripData curTrip;

    public AtlasRenderer railMapRend;
    public AtlasRenderer trainPosRend;

    public AtlasRenderer stationIconRendPrefab;
    public AtlasRenderer ticketCheckIconRendPrefab;

    [Header("Generated")]
    public AtlasRenderer[] markIcons;
    public float[] positions;

    public int curPosIndex;

    public CancellationTokenSource ctsMove;
    public CancellationTokenSource ctsAppear;
    private void Start()
    {
        Init();
    }
    private void Init()
    {
        int ticketCheckAmount = (curTrip.stationsDataArray.Length - 1) * 2;
        int totalPositions = curTrip.stationsDataArray.Length + ticketCheckAmount;
        positions = new float[totalPositions];

        Vector4[] worldPivAndSizes = railMapRend.worldPivotsAndSizes;
        float startPos = worldPivAndSizes[1].x;
        float endPos = worldPivAndSizes[2].x;
        float totalDist = endPos - startPos;
        float segment = totalDist / (totalPositions - 1);

        markIcons = new AtlasRenderer[totalPositions];
        
        float curPos = startPos;
        Vector3 localPos = new Vector3(0, 0, -0.5f);
        for (int i = 0; i < totalPositions; i++)
        {
            positions[i] = curPos;
            localPos.x = curPos;

            bool isStationPos = i % 3 == 0;

            if (isStationPos)
            {
                AtlasRenderer stationIcon = Instantiate(stationIconRendPrefab, transform);
                stationIcon.transform.localPosition = localPos;
                stationIcon.custom.w = 0;
                markIcons[i] = stationIcon;
            }
            else
            {
                AtlasRenderer ticketCheckIcon = Instantiate(ticketCheckIconRendPrefab, transform);
                ticketCheckIcon.transform.localPosition = new Vector3(localPos.x, -railMapRend.bounds.extents.y, localPos.z);
                ticketCheckIcon.custom.w = 0;
                markIcons[i] = ticketCheckIcon;
            }
            
            curPos += segment;
        }

        trainPosRend.transform.localPosition = new Vector3(startPos, localPos.y, -1);
        railMapRend.SetSliceCustom(w: 0);
        trainPosRend.custom.w = 0;
    }
    public void Appear()
    {
        ctsAppear?.Cancel();
        ctsAppear = new CancellationTokenSource();
        Appearing().Forget();
    }
    public void Dissappear()
    {
        ctsAppear?.Cancel();
        ctsAppear = new CancellationTokenSource();
        Dissappearing().Forget();
    }
    public void MoveToNextPosition()
    {
        ctsMove?.Cancel();
        ctsMove =  new CancellationTokenSource();

        MovingToNextPosition().Forget();
    }
    private async UniTask Appearing()
    {
        float clock = 0;
        try
        {
            while (clock < APPEARING_TIME)
            {
                clock += Time.deltaTime;
                float t = clock / APPEARING_TIME;

                for (int i = 0; i < markIcons.Length; i++)
                {
                    markIcons[i].custom.w = t;
                }
                trainPosRend.custom.w = t;
                railMapRend.SetSliceCustom(w: t);
                await UniTask.Yield(ctsAppear.Token);
            }

            for (int i = 0; i < markIcons.Length; i++)
            {
                markIcons[i].custom.w = 1;
            }
        }
        catch(OperationCanceledException)
        {
            for (int i = 0; i < markIcons.Length; i++)
            {
                markIcons[i].custom.w = 1;
            }
            trainPosRend.custom.w = 1;
            railMapRend.SetSliceCustom(w: 1);
        }
    }
    private async UniTask Dissappearing()
    {
        float clock = APPEARING_TIME;

        try
        {
            while (clock >= 0)
            {
                clock -= Time.deltaTime;
                float t = clock / APPEARING_TIME;

                for (int i = 0; i < markIcons.Length; i++)
                {
                    markIcons[i].custom.w = t;
                }
                trainPosRend.custom.w = t;
                railMapRend.SetSliceCustom(w: t);
                await UniTask.Yield(ctsAppear.Token);
            }

            for (int i = 0; i < markIcons.Length; i++)
            {
                markIcons[i].custom.w = 0;
            }
        }
        catch (OperationCanceledException)
        {
            for (int i = 0; i < markIcons.Length; i++)
            {
                markIcons[i].custom.w = 0;
            }
            trainPosRend.custom.w = 0;
            railMapRend.SetSliceCustom(w: 0);
        }
    }
    private async UniTask MovingToNextPosition()
    {
        float clock = 0;
        Vector3 curPos = trainPosRend.transform.localPosition;
        float startPos = curPos.x;
        curPosIndex++;
        float nextPos = positions[curPosIndex];

        try
        {
            while(clock < MOVE_TIME)
            {
                clock += Time.deltaTime;
                float t = clock / MOVE_TIME;
                curPos.x = Mathf.Lerp(startPos, nextPos, t);
                trainPosRend.transform.localPosition = curPos;
                await UniTask.Yield(ctsMove.Token);
            }
        }
        catch(OperationCanceledException)
        {

        }
    }
}
