using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    private Camera cam;
    private TrainController trainController;

    private Vector2 startPos;
    private float startZ;

    public float parallaxFactor {  get; private set; }

    private float currentTrainDistanceMoved;
    private float totalTrainDistanceMoved;
    private float distanceFromClipPlaneZ;
    private float clipPlaneZ;

    private void Awake()
    {
        cam = Camera.main;
        trainController = GameObject.FindWithTag("Train Object").GetComponent<TrainController>();
        GetParralaxData(cam);
        
    }

    private void OnEnable()
    {
        Initialize(); 
    }

    private void Update()
    {
        UpdatePos();
    }
    public void Initialize()
    {

        startPos = transform.position;
        startZ = transform.position.z;
        totalTrainDistanceMoved = trainController.metersTravelled;

    }
    private void UpdatePos()
    {
        currentTrainDistanceMoved = trainController.metersTravelled - totalTrainDistanceMoved;
        float newPosXTrainFactor = currentTrainDistanceMoved * parallaxFactor;
        float newPosX = startPos.x - newPosXTrainFactor;
        transform.position = new Vector3(newPosX, startPos.y, startZ);
    }

    public void GetParralaxData(Camera cam)
    {
        clipPlaneZ = cam.transform.position.z + cam.farClipPlane;
        distanceFromClipPlaneZ = transform.position.z - clipPlaneZ;
        parallaxFactor = Mathf.Abs(distanceFromClipPlaneZ) / clipPlaneZ;
    }
}
