using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Unity.VisualScripting;

public class TrainController : MonoBehaviour
{
    //controls
    public float kmPerHour = 10f;

    //stats
    public float mPerSec {  get; private set; }
    public float metersTravelled { get; private set; }
    private float currentSpeed;
    private const float kmConversion = 3.6f; // 1 m/s = 3.6 km/h

    //References
    private TrainBounds trainBounds => GetComponent<TrainBounds>();
    public List<SlideDoorBounds> slideDoorsList = new List<SlideDoorBounds>();
    public List<InsideBounds> insideBoundsList = new List<InsideBounds>();
    public List<SeatBounds> seatBoundsList =  new List<SeatBounds>();
    //StationData
    [SerializeField] private List<StationData> stationDataList = new List<StationData>();
    public Queue<StationData> stationDataQueue = new Queue<StationData>();
    public StationData currentStation {  get; private set; }

    private void Start()
    {
        SetStationDataList();
        SetTrainDoors();
        SetInsideBoundsList();
        SetSeats();
    }

    private void Update()
    {
        mPerSec = kmPerHour / kmConversion;
        float frameDistance = currentSpeed * Time.deltaTime;
        metersTravelled += frameDistance;
    }

    public void AccelationController()
    {
        StartCoroutine(AcceleratingTrain());
    }
    private IEnumerator AcceleratingTrain()
    {
        while (stationDataQueue.Count > 0)
        {
            currentStation = stationDataQueue.Peek();

            yield return new WaitUntil(() => trainBounds.boundsMaxX > currentStation.decelThreshold);

            float startSpeed = kmPerHour;
            float stoppingDistance = currentStation.accelerationThresholds + (trainBounds.boundsMaxX - trainBounds.boundsHalfX);
            float accelationSpeed = (2 * stoppingDistance) / (startSpeed / kmConversion); // using the equation of motion v=u+at where t is equal to 2s/u

            float elapsedTime = 0f;
            while (kmPerHour > 0)
            {
                elapsedTime += Time.deltaTime;

                float normalizedTime = Mathf.Clamp01(elapsedTime / accelationSpeed);
                kmPerHour = Mathf.Lerp(startSpeed, 0, normalizedTime);

                yield return null;
            }
        }
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
}
