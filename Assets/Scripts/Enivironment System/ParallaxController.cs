using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public TrainController trainController;
    public Transform playerTransform;

    private Vector2 startPos;
    private float startZ;

    private Vector2 playerDistanceMoved;
    private float trainDistanceMoved;
    private float distanceFromPlayerZ;
    private float distanceFromClipPlaneZ;
    private float clipPlaneZ;
    private float parallaxFactor;
    private void Start()
    {
        startPos = transform.position;
        startZ = transform.position.z;
    }

    private void Update()
    {
        GetParralaxData();
        UpdatePos();
    }

    private void UpdatePos()
    {
        playerDistanceMoved = (Vector2)playerTransform.position;
        trainDistanceMoved = trainController.distanceTravelled;

        float newPosXPlayerFactor = playerDistanceMoved.x * parallaxFactor;
        float newPosXTrainFactor = trainDistanceMoved * parallaxFactor;
        float newPosX = startPos.x + (newPosXPlayerFactor - newPosXTrainFactor);
        float newPosY = startPos.y + (playerDistanceMoved.y * parallaxFactor);

        transform.position = new Vector3(newPosX, newPosY, startZ);
    }

    private float NewPos(float startPos, float distanceMoved)
    {
        return startPos + (distanceMoved * parallaxFactor);
    }

    private void GetParralaxData()
    {
        distanceFromPlayerZ = transform.position.z - playerTransform.position.z;
        clipPlaneZ = (cam.transform.position.z + (distanceFromPlayerZ > 0 ? cam.farClipPlane : cam.nearClipPlane));
        distanceFromClipPlaneZ = transform.position.z - clipPlaneZ;
        parallaxFactor = Mathf.Abs(distanceFromClipPlaneZ) / clipPlaneZ;
    }
}
