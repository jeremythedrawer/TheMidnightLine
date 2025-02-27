using UnityEngine;
using System.Collections;

public class ParallaxController : MonoBehaviour
{
    private Camera cam => GlobalReferenceManager.Instance.mainCam;
    private TrainData trainData => GlobalReferenceManager.Instance.trainData;

    private Vector2 startPos;
    private float startZ;

    public float parallaxFactor {  get; private set; }

    private float currentTrainDistanceMoved;
    private float totalTrainDistanceMoved;
    private float distanceFromClipPlaneZ;
    private float clipPlaneZ;

    private void Start()
    {
        GetParralaxData(cam);
    }

    private void OnEnable()
    {
        ScrollObject();
    }

    public void Initialize()
    {
        startPos = transform.position;
        startZ = transform.position.z;
        totalTrainDistanceMoved = trainData.metersTravelled;
    }

    private void ScrollObject()
    {
        StartCoroutine(ScrollingObject());
    }
    private IEnumerator ScrollingObject()
    {
        yield return new WaitUntil(()=> trainData.arrivedToStartPosition);
        Initialize();
        while (true)
        {
            UpdatePos();
            yield return null;
        }
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
