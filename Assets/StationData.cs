using UnityEngine;

public class StationData : MonoBehaviour
{
    public float accelerationThresholds;

    public float decelThreshold => transform.position.x - accelerationThresholds;
    public float accelThreshold => transform.position.x + accelerationThresholds;

    private void OnDrawGizmos()
    {
        DrawAccelThresholds(true);
    }

    private void Start()
    {
        DrawAccelThresholds(false);
    }

    private void DrawAccelThresholds(bool usingGizmos)
    {
#if UNITY_EDITOR
        float decelX = transform.position.x - accelerationThresholds;
        float accelX = transform.position.x + accelerationThresholds;
        float height = 10;
        Vector2 decelUpperOrigin = new Vector2(decelX , transform.position.y + height);
        Vector2 decelLowerOrigin = new Vector2(decelX , transform.position.y);

        Vector2 accelUpperOrigin = new Vector2(accelX, transform.position.y + height);
        Vector2 accelLowerOrigin = new Vector2(accelX, transform.position.y);

        if (usingGizmos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(decelUpperOrigin, decelLowerOrigin);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine (accelUpperOrigin, accelLowerOrigin);
        }
        else
        {
            Debug.DrawLine(decelUpperOrigin , decelLowerOrigin, Color.red);
            Debug.DrawLine(accelLowerOrigin , accelUpperOrigin, Color.blue);
        }
#endif
    }
}
