using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class TrainController : MonoBehaviour
{
    //controls
    public enum Direction { Left, Right }
    public Direction trainDirection;
    public float kmPerHour = 10f;

    //stats
    public float metersTravelled { get; private set; }
    private float currentSpeed;
    private const float kmConversion = 3.6f; // 1 m/s = 3.6 km/h

    //References
    private TrainBounds trainBounds => GetComponent<TrainBounds>();

    //StationData
    [SerializeField] private List<StationData> stationDataList = new List<StationData>();
    public Queue<StationData> stationDataQueue = new Queue<StationData>();

    private void Start()
    {
        StationData[] stations = FindObjectsByType<StationData>(FindObjectsSortMode.None);
        stationDataList.AddRange(stations);

        //stations order by position
        stationDataList = stations.OrderBy(station => station.transform.position.x).ToList();

        foreach (StationData stationData in stationDataList)
        {
            stationDataQueue.Enqueue(stationData);
        }

        StartCoroutine(AccelationController());
    }

    private void Update()
    {
        float mPerSec = kmPerHour / kmConversion;
        currentSpeed = trainDirection == Direction.Right ? mPerSec : -mPerSec;
        float frameDistance = currentSpeed * Time.deltaTime;
        metersTravelled += frameDistance;
    }


    private IEnumerator AccelationController()
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
                Debug.Log(accelationSpeed);
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
}
