using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class TrainData : MonoBehaviour
{
    [Header("References")]
    public Collider2D backCarriageCollider;
    public Collider2D frontCarriageCollider;
    public Transform agentsParent;
    public Transform bystandersParent;

    [Header("Parameters")]
    public float wheelLevel = 1.0f;
    public float roofLevel = 1.0f;

    [SerializeField] private bool seeBoundsGizmos;
    public float boundsMinX => backCarriageCollider.bounds.min.x;
    public float boundsMaxX => frontCarriageCollider.bounds.max.x;
    public float boundsHalfX => boundsMinX + ((boundsMaxX - boundsMinX) / 2);

    //controls
    public float kmPerHour = 10f;

    //stats
    public float mPerSec { get; protected set; }
    public float metersTravelled { get; protected set; }
    public float kmConversion { get; private set; } = 3.6f;

    //References
    public List<SlideDoorBounds> slideDoorsList = new List<SlideDoorBounds>();
    public List<InsideBounds> insideBoundsList = new List<InsideBounds>();
    public List<SeatBounds> seatBoundsList = new List<SeatBounds>();
    //StationData
    [SerializeField] protected List<StationData> stationDataList = new List<StationData>();
    public Queue<StationData> stationDataQueue = new Queue<StationData>();
    public StationData currentStation { get; private set; }
    //Characters
    public List<StateCore> charactersList = new List<StateCore>();

    private void OnDrawGizmos()
    {
        DebugLines(true);
    }

    private void Start()
    {
        SetStationDataList();
        SetTrainDoors();
        SetInsideBoundsList();
        SetSeats();

        UpdateStationQueue();

        wheelLevel = transform.position.y + wheelLevel;
        roofLevel = transform.position.y + roofLevel;

        DebugLines(false);
    }

    private void Update()
    {
        mPerSec = kmPerHour / kmConversion;
        float frameDistance = mPerSec * Time.deltaTime;
        metersTravelled += frameDistance;
    }

    private async void UpdateStationQueue()
    {
        while (stationDataQueue.Count == 0) { await Task.Yield(); } //wait for queue to fill

        currentStation = stationDataQueue.Peek();
    }
    private void SetStationDataList()
    {
        StationData[] stations = FindObjectsByType<StationData>(FindObjectsSortMode.None);
        stationDataList.AddRange(stations);

        //stations order by position
        stationDataList = stations.OrderBy(station => station.transform.position.x).ToList();

        foreach (StationData stationData in stationDataList)
        {
            stationDataQueue.Enqueue(stationData);
        }
        currentStation = stationDataQueue.Peek();
    }
    private void SetTrainDoors()
    {
        SlideDoorBounds[] slideDoors = FindObjectsByType<SlideDoorBounds>(FindObjectsSortMode.None);
        slideDoorsList.AddRange(slideDoors);
    }
    private void SetSeats()
    {
        SeatBounds[] setsOfSeats = FindObjectsByType<SeatBounds>(FindObjectsSortMode.None);
        seatBoundsList.AddRange(setsOfSeats);
    }
    private void SetInsideBoundsList()
    {
        InsideBounds[] insideBounds = FindObjectsByType<InsideBounds>(FindObjectsSortMode.None);
        insideBoundsList.AddRange(insideBounds);
    }

    private void DebugLines(bool usingGizmos)
    {
#if UNITY_EDITOR
        if (seeBoundsGizmos)
        {
            Vector2 wheelsMin = new Vector2 (boundsMinX, wheelLevel);
            Vector2 wheelsMax = new Vector2 (boundsMaxX, wheelLevel);
            Vector2 roofMin = new Vector2 (boundsMinX, roofLevel);
            Vector2 roofMax = new Vector2 (boundsMaxX, roofLevel);

            Vector2 halfUpperOrigin = new Vector2(boundsHalfX, roofLevel);
            Vector2 halfLowerOrigin = new Vector2(boundsHalfX, wheelLevel);

            if (!usingGizmos)
            {
                Debug.DrawLine(wheelsMin, wheelsMax, Color.yellow);
                Debug.DrawLine(roofMin, roofMax, Color.yellow);
                Debug.DrawLine(halfLowerOrigin, halfUpperOrigin, Color.yellow);

            }
            else
            {
                if (Application.isPlaying) return;

                float wheels = transform.position.y + this.wheelLevel;
                float roof = transform.position.y + this.roofLevel;

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(wheelsMin, wheelsMax);
                Gizmos.DrawLine(roofMin, roofMax);
                Gizmos.DrawLine(halfLowerOrigin, halfUpperOrigin);
            }

        }
#endif
    }
}
