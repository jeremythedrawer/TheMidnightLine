using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    private Camera cam;
    private TrainController trainController;

    private Vector2 startPos;
    private float startZ;

    private float currentTrainDistanceMoved;
    private float totalTrainDistanceMoved;
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

        startPos = transform.position;
        startZ = transform.position.z;
        totalTrainDistanceMoved = trainController.distanceTravelled;

    }
    private void UpdatePos()
    {
        currentTrainDistanceMoved = trainController.distanceTravelled - totalTrainDistanceMoved;
        float newPosXTrainFactor = currentTrainDistanceMoved * parallaxFactor;
        float newPosX = startPos.x - newPosXTrainFactor;
        transform.position = new Vector3(newPosX, startPos.y, startZ);
    }

    private void GetParralaxData()
    {
        clipPlaneZ = cam.transform.position.z + cam.farClipPlane;
        distanceFromClipPlaneZ = transform.position.z - clipPlaneZ;
        parallaxFactor = Mathf.Abs(distanceFromClipPlaneZ) / clipPlaneZ;
    }
}
