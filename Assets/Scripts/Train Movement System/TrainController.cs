using UnityEngine;

public class TrainController : MonoBehaviour
{
    public enum Direction { Left, Right }
    public Direction trainDirection;
    public float KmPerHour = 10f;

    public float metersTravelled { get; private set; }
    private float currentSpeed;
    private const float kmConversion = 3.6f; // 1 m/s = 3.6 km/h

    private void Update()
    {
        float mPerSec = KmPerHour / kmConversion;

        currentSpeed = trainDirection == Direction.Right ? mPerSec : -mPerSec;

        float frameDistance = currentSpeed * Time.deltaTime;

        metersTravelled += frameDistance;
    }
}
