using UnityEngine;

public class MaxBounds : MonoBehaviour
{
    [SerializeField] MaxBoundsSO maxBounds;
    [SerializeField] TrainStatsSO trainStats;
    [SerializeField] StationsDataSO stationsData;
    [SerializeField] CameraSettingsSO camSettings;
    [SerializeField] CameraStatsSO camStats;

    private void OnValidate()
    {
        Reinit();
    }

    private void Awake()
    {
        Reinit();
    }

    private void Reinit()
    {
        float camMeterWidth = camSettings.maxProjectionSize * camStats.aspect;
        maxBounds.min.x = ((stationsData.stations[0].metersPosition - trainStats.trainHalfLength) - camMeterWidth);
        maxBounds.min.y = -camSettings.maxProjectionSize;
        maxBounds.max.x = ((stationsData.stations[0].metersPosition + trainStats.trainHalfLength) + camMeterWidth);
        maxBounds.max.y = (trainStats.trainMaxHeight + camSettings.maxProjectionSize);
        Vector2 bufferOffet = new Vector2(maxBounds.bufferAmount, maxBounds.bufferAmount);
        maxBounds.min -= bufferOffet;
        maxBounds.max += bufferOffet;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 bottomRight = new Vector2(maxBounds.max.x, maxBounds.min.y);
        Vector2 topLeft = new Vector2(maxBounds.min.x, maxBounds.max.y);
        Gizmos.DrawLine(maxBounds.min, bottomRight);
        Gizmos.DrawLine(bottomRight, maxBounds.max);
        Gizmos.DrawLine(maxBounds.max, topLeft);
        Gizmos.DrawLine(topLeft, maxBounds.min);
    }
}
