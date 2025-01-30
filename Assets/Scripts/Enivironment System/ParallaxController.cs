using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    private Camera cam;
    private TrainController trainController;
    private Transform playerTransform;

    private Vector2 startPos;
    private float startZ;

    private Vector2 playerDistanceMoved;
    private float currentTrainDistanceMoved;
    private float totalTrainDistanceMoved;
    private float distanceFromPlayerZ;
    private float distanceFromClipPlaneZ;
    private float clipPlaneZ;
    private float parallaxFactor;

    private void OnEnable()
    {
        Initialize();
        
    }
    private void Update()
    {
        GetParralaxData();
        UpdatePos();
    }
    public void Initialize()
    {
        cam = Camera.main;
        trainController = GameObject.FindWithTag("Train Object").GetComponent<TrainController>();
        playerTransform = GameObject.FindWithTag("Player").GetComponent<Transform>();

        startPos = transform.position;
        startZ = transform.position.z;
        totalTrainDistanceMoved = trainController.distanceTravelled;
    }
    private void UpdatePos()
    {
        playerDistanceMoved = (Vector2)playerTransform.position;
        currentTrainDistanceMoved = trainController.distanceTravelled - totalTrainDistanceMoved;

        float newPosXPlayerFactor = playerDistanceMoved.x * parallaxFactor;
        float newPosXTrainFactor = currentTrainDistanceMoved * parallaxFactor;
        float newPosX = startPos.x - newPosXTrainFactor;
        transform.position = new Vector3(newPosX, startPos.y, startZ);
    }

    private void GetParralaxData()
    {
        distanceFromPlayerZ = transform.position.z - playerTransform.position.z;
        clipPlaneZ = (cam.transform.position.z + (distanceFromPlayerZ > 0 ? cam.farClipPlane : cam.nearClipPlane));
        distanceFromClipPlaneZ = transform.position.z - clipPlaneZ;
        parallaxFactor = Mathf.Abs(distanceFromClipPlaneZ) / clipPlaneZ;
    }
}
