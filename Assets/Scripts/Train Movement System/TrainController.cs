using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

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
    private List<Transform> trainWheels = new List<Transform>();

    //StationData
    [SerializeField] private List<StationData> stationDataList = new List<StationData>();
    public Queue<StationData> stationDataQueue = new Queue<StationData>();

    private void Start()
    {
        SetStationDataList();
    }

    private void Update()
    {
        mPerSec = kmPerHour / kmConversion;
        float frameDistance = currentSpeed * Time.deltaTime;
        metersTravelled += frameDistance;
    }


    public IEnumerator AccelationController()
    {
        while (stationDataQueue.Count > 0)
        {
            StationData currentStation = stationDataQueue.Peek();

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

    private void Decelerate()
    {

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
    }
}
