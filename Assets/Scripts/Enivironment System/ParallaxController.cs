using UnityEngine;
using System.Collections;

public class ParallaxController : MonoBehaviour
{
    private Camera cam => GlobalReferenceManager.Instance.mainCam;
    private TrainData trainData => GlobalReferenceManager.Instance.trainData;
    private CanvasBounds canvasBounds => GlobalReferenceManager.Instance.canvasBounds;
    private SpriteRenderer spriteRenderer;
    private Spawner parentSpawner;
    private Vector2 startPos;
    private float startZ;

    public float parallaxFactor {  get; private set; }

    private float currentTrainDistanceMoved;
    private float totalTrainDistanceMoved;
    private float distanceFromClipPlaneZ;
    private float clipPlaneZ;

    private bool parallaxEnabled;

    private void Awake()
    {
        parentSpawner = GetComponentInParent<Spawner>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    private void OnEnable()
    {
        ScrollObject();
    }

    private void Update()
    {
        if (trainData == null || parentSpawner == null) return;
        bool withinDistance = trainData.metersTravelled > parentSpawner.startSpawnDistance && trainData.metersTravelled < parentSpawner.endSpawnDistance;
        bool inCanvasBounds =  spriteRenderer.bounds.min.x < canvasBounds.right && spriteRenderer.bounds.max.x > canvasBounds.left;
        parallaxEnabled = withinDistance || inCanvasBounds;
        
    }
    public void Initialize()
    {
        startPos = transform.position;
        startZ = transform.position.z;
        totalTrainDistanceMoved = trainData.metersTravelled;
        GetParralaxData(cam);
    }

    private void ScrollObject()
    {
        StartCoroutine(ScrollingObject());
    }
    private IEnumerator ScrollingObject()
    {
        yield return new WaitUntil(()=> trainData.arrivedToStartPosition);
        yield return new WaitUntil(()=> parallaxEnabled);
        Initialize();
        while (parallaxEnabled)
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
