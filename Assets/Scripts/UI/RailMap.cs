using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using static Atlas;

public class RailMap : MonoBehaviour
{
    public const float MOVE_TIME = 3f;

    public TripSO curTrip;

    public AtlasRenderer railMapRend;
    public AtlasRenderer trainPosRend;
    public AtlasRenderer stationIconRendPrefab;
    public AtlasRenderer ticketCheckIconRendPrefab;

    [Header("Generated")]
    
    public float[] positions;

    public int curPosIndex;

    public CancellationTokenSource ctsMove;

    private void OnEnable()
    {
        
    }
    private void OnDisable()
    {
        
    }
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
            }
            else
            {
                AtlasRenderer ticketCheckIcon = Instantiate(ticketCheckIconRendPrefab, transform);
                ticketCheckIcon.transform.localPosition = new Vector3(localPos.x, -railMapRend.bounds.extents.y, localPos.z);
            }
            
            curPos += segment;
        }

        trainPosRend.transform.localPosition = new Vector3(startPos, localPos.y, -1);
    }
    public void MoveToNextPosition()
    {
        ctsMove?.Cancel();
        ctsMove =  new CancellationTokenSource();

        MovingToNextPosition().Forget();
    }
    private async UniTask MovingToNextPosition()
    {
        float clock = 0;
        Vector3 curPos = trainPosRend.transform.localPosition;
        float startPos = curPos.x;
        curPosIndex++;
        float nextPos = positions[curPosIndex];
        
        while(clock < MOVE_TIME)
        {
            clock += Time.deltaTime;
            float t = clock / MOVE_TIME;
            curPos.x = Mathf.Lerp(startPos, nextPos, t);
            trainPosRend.transform.localPosition = curPos;
            await UniTask.Yield();
        }
    }
}
