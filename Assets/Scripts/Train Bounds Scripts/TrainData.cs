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
    public Transform playerParent;

    [Header("Parameters")]
    public float wheelLevel = 1.0f;
    public float roofLevel = 1.0f;

    [SerializeField] private bool seeBoundsGizmos;

    private TrainController trainController => GlobalReferenceManager.Instance.trainController;

    public float boundsMinX => backCarriageCollider.bounds.min.x;
    public float boundsMaxX => frontCarriageCollider.bounds.max.x;
    public float boundsHalfX => boundsMinX + ((boundsMaxX - boundsMinX) / 2);

    public float trainWidth {  get; private set; }
    public float boundsHalfXDistance {  get; private set; }

    //controls
    public float kmPerHour = 10f;

    //stats
    public float mPerSec { get; set; }
    public float metersTravelled { get; set; }
    public float kmConversion => 5f/18f;

    public bool arrivedToStartPosition { get; set; }
    //References
    public List<SlideDoorBounds> slideDoorsList = new List<SlideDoorBounds>();
    public List<WheelData> wheels = new List<WheelData>();
    public List<InsideBounds> insideBoundsList = new List<InsideBounds>();
    public List<SeatBounds> seatBoundsList = new List<SeatBounds>();
    //StationData
    private List<StationData> stationDataList => GlobalReferenceManager.Instance.stations;
    public Queue<StationData> stationDataQueue = new Queue<StationData>();
    public StationData currentStation { get; private set; }
    //Characters
    public List<StateCore> charactersList = new List<StateCore>();

    private void Awake()
    {
        trainWidth = boundsMaxX - boundsMinX;
    }
    private void OnValidate()
    {
        trainWidth = boundsMaxX - boundsMinX;
    }
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
        SetWheels();

        UpdateStationQueue();

        boundsHalfXDistance = boundsMaxX - boundsHalfX;
        wheelLevel = transform.position.y + wheelLevel;
        roofLevel = transform.position.y + roofLevel;

        DebugLines(false);
    }

    private void Update()
    {
        mPerSec = kmPerHour * kmConversion;

        if (trainController.setSpeed == kmPerHour)
        {
            metersTravelled += mPerSec * Time.deltaTime;
            foreach (WheelData wheel in wheels)
            {
                float deltaX = mPerSec * Time.deltaTime;
                float rotation = -deltaX * wheel.degreesPerMeter;
                wheel.transform.Rotate(0, 0, rotation);
            }
        }

    }

    public async void UpdateStationQueue()
    {
        if (currentStation != null)
        {
            stationDataQueue.Dequeue();
        }
        while (stationDataQueue.Count == 0) { await Task.Yield(); } //wait for queue to fill

        currentStation = stationDataQueue.Peek();
    }
    private void SetStationDataList()
    {
        foreach (StationData stationData in stationDataList)
        {
            stationDataQueue.Enqueue(stationData);
        }
    }
    private void SetTrainDoors()
    {
        SlideDoorBounds[] slideDoors = FindObjectsByType<SlideDoorBounds>(FindObjectsSortMode.None);
        slideDoorsList.AddRange(slideDoors);
    }    
    private void SetWheels()
    {
        WheelData[] wheelsList = FindObjectsByType<WheelData>(FindObjectsSortMode.None);
        wheels.AddRange(wheelsList);
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
