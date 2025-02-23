using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;

public class TrainController : MonoBehaviour
{
    //controls
    public float kmPerHour = 10f;

    //stats
    public float mPerSec {  get; private set; }
    public float metersTravelled { get; private set; }
    private const float kmConversion = 3.6f; // 1 m/s = 3.6 km/h

    //References
    public TrainBounds trainBounds => GetComponent<TrainBounds>();
    public List<SlideDoorBounds> slideDoorsList = new List<SlideDoorBounds>();
    public List<InsideBounds> insideBoundsList = new List<InsideBounds>();
    public List<SeatBounds> seatBoundsList =  new List<SeatBounds>();
    //StationData
    [SerializeField] private List<StationData> stationDataList = new List<StationData>();
    public Queue<StationData> stationDataQueue = new Queue<StationData>();
    public StationData currentStation {  get; private set; }

    public float normalizedTime { get; private set; }
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
        float frameDistance = mPerSec * Time.deltaTime;
        metersTravelled += frameDistance;
    }

    public async void TrainInputs()
    {   
        while (stationDataQueue.Count == 0) {  await Task.Yield(); } //wait for queue to fill
        currentStation = stationDataQueue.Peek();
        while (trainBounds.boundsMaxX < currentStation.decelThreshold) { await Task.Yield(); } // wait until train cross decel threshold
        await UpdateSpeed(0);
        await UnlockDoors();
        await LockDoors();
    }
    private async Task UpdateSpeed(float newSpeed)
    {
        float startSpeed = kmPerHour;
        float stoppingDistance = currentStation.accelerationThresholds + (trainBounds.boundsMaxX - trainBounds.boundsHalfX);
        float accelationTime = (2 * stoppingDistance) / (startSpeed / kmConversion); // using the equation of motion v=u+at where t is equal to 2s/u
        float elapsedTime = 0f;
        while (kmPerHour != newSpeed)
        {
            elapsedTime += Time.deltaTime;

            normalizedTime = Mathf.Clamp01(elapsedTime / accelationTime);
            kmPerHour = Mathf.Lerp(startSpeed, newSpeed, normalizedTime);

            await Task.Yield();
        }
    }
    private async Task UnlockDoors()
    {
        foreach (SlideDoorBounds slideDoors in slideDoorsList)
        {
            slideDoors.UnlockDoors();
        }
        await Task.Yield();
    }

    private async Task LockDoors()
    {
        while (currentStation.charactersList.Count != 0) { await Task.Yield(); }
        await Task.Delay(1000);
        List<SlideDoorBounds> openDoors = slideDoorsList.Where(slideDoors => slideDoors.openDoor).ToList();
        foreach (SlideDoorBounds slideDoors in openDoors)
        {
            slideDoors.CloseDoors();
        }
        while (openDoors.Any(openDoor => openDoor.normMoveDoorTime < 1.0f)) { await Task.Yield(); }
        foreach (SlideDoorBounds slideDoors in slideDoorsList)
        {
            slideDoors.LockDoors();
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
