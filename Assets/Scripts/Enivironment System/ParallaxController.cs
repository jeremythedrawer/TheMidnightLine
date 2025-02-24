using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    private Camera cam;
    private TrainData trainData;

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
        trainData = GameObject.FindWithTag("Train Object").GetComponent<TrainData>();
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
        totalTrainDistanceMoved = trainData.metersTravelled;

    }
    private void UpdatePos()
    {
        currentTrainDistanceMoved = trainData.metersTravelled - totalTrainDistanceMoved;
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
