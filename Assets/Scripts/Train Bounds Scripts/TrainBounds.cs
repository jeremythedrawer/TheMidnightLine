using UnityEngine;

public class TrainBounds : MonoBehaviour
{
    public float wheelLevel = 1.0f;
    public float roofLevel = 1.0f;

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        float bottom = transform.position.y - this.wheelLevel;
        float roof = transform.position.y + this.roofLevel;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, bottom));
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, roof));
        Gizmos.DrawLine(new Vector3(transform.position.x - 3, bottom), new Vector3(transform.position.x + 3, bottom));
        Gizmos.DrawLine(new Vector3(transform.position.x - 3, roof), new Vector3(transform.position.x + 3, roof));
    }

    private void Start()
    {
        wheelLevel = transform.position.y - wheelLevel;
        roofLevel = transform.position.y + roofLevel;    
    }

    private void Update()
    {
        Debug.DrawLine(new Vector3(transform.position.x - 3, wheelLevel), new Vector3(transform.position.x + 3, wheelLevel), Color.yellow);
        Debug.DrawLine(new Vector3(transform.position.x - 3, roofLevel), new Vector3(transform.position.x + 3, roofLevel), Color.yellow);
    }
}
