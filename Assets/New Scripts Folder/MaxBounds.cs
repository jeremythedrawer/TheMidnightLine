using UnityEngine;

public class MaxBounds : MonoBehaviour
{
    [SerializeField] MaxBoundsStatsSO stats;
    [SerializeField] TrainStatsSO trainStats;
    [SerializeField] StationsDataSO stationsData;
    [SerializeField] CameraSettingsSO camSettings;
    [SerializeField] CameraStatsSO camStats;
    [SerializeField] float bufferAmount = 1.1f;
    private void OnValidate()
    {
        float camMeterWidth = camSettings.maxProjectionSize * camStats.aspect;
        stats.min.x = ((stationsData.stations[0].metersPosition - trainStats.trainHalfLength) - camMeterWidth);
        stats.min.y = -camSettings.maxProjectionSize;
        stats.max.x = ((stationsData.stations[0].metersPosition + trainStats.trainHalfLength) + camMeterWidth);
        stats.max.y = (trainStats.trainMaxHeight + camSettings.maxProjectionSize);
        float aspectBufferAmount = bufferAmount * ((stats.max.x - stats.min.x) / (stats.max.y - stats.min.y));
        Vector2 bufferOffet = new Vector2(bufferAmount, bufferAmount);
        stats.min -= bufferOffet;
        stats.max += bufferOffet;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 bottomRight = new Vector2(stats.max.x, stats.min.y);
        Vector2 topLeft = new Vector2(stats.min.x, stats.max.y);
        Gizmos.DrawLine(stats.min, bottomRight);
        Gizmos.DrawLine(bottomRight, stats.max);
        Gizmos.DrawLine(stats.max, topLeft);
        Gizmos.DrawLine(topLeft, stats.min);
    }
}
