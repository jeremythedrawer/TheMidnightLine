using UnityEngine;

public class TrainCamBounds : MonoBehaviour
{
    public float camBoundBottomOffset = 1.0f;
    public float camBoundRoofOffset = 1.0f;

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        float bottom = transform.position.y - camBoundBottomOffset;
        float roof = transform.position.y + camBoundRoofOffset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, bottom));
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, roof));
        Gizmos.DrawLine(new Vector3(transform.position.x - 3, bottom), new Vector3(transform.position.x + 3, bottom));
        Gizmos.DrawLine(new Vector3(transform.position.x - 3, roof), new Vector3(transform.position.x + 3, roof));
    }

    private void Start()
    {
        camBoundBottomOffset = transform.position.y - camBoundBottomOffset;
        camBoundRoofOffset = transform.position.y + camBoundRoofOffset;    
    }

    private void Update()
    {
        Debug.DrawLine(new Vector3(transform.position.x - 3, camBoundBottomOffset), new Vector3(transform.position.x + 3, camBoundBottomOffset), Color.yellow);
        Debug.DrawLine(new Vector3(transform.position.x - 3, camBoundRoofOffset), new Vector3(transform.position.x + 3, camBoundRoofOffset), Color.yellow);
    }
}
