using UnityEngine;
using System.Collections;

public class ParallaxController : MonoBehaviour
{
    [HideInInspector] public float spawnPosition;
    private Camera cam => GlobalReferenceManager.Instance.mainCam;
    private TrainData trainData => GlobalReferenceManager.Instance.trainData;
    private CanvasBounds canvasBounds => GlobalReferenceManager.Instance.canvasBounds;
    private SpriteRenderer spriteRenderer;
    private Spawner parentSpawner;
    private Vector2 startPos;
    private float startZ;

    private SpriteRenderer[] spriteRenderers;
    private SpriteRenderer rightMostSprite;
    public float parallaxFactor {  get; private set; }

    private float currentTrainDistanceMoved;
    private float totalTrainDistanceMoved;
    private float distanceFromClipPlaneZ;
    private float clipPlaneZ;

    private bool parallaxActive;
    private bool isOneShot;
    private bool withinDistance;

    private void Awake()
    {
        parentSpawner = GetComponentInParent<Spawner>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        isOneShot = parentSpawner is OneShotSpawner;


        if (isOneShot)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            SpriteRenderer currentSpriteRen;
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                currentSpriteRen = spriteRenderers[i];
                if (i == 0)
                {
                    rightMostSprite = currentSpriteRen;
                }
                else
                {
                    if (currentSpriteRen.bounds.max.x > rightMostSprite.bounds.max.x)
                    {
                        rightMostSprite = currentSpriteRen;
                    }
                }
            }
        }
    }
    private void OnEnable()
    {
        ScrollObject();
    }

    private void Update()
    {
        if (trainData == null || parentSpawner == null) return;

        if (isOneShot)
        {
            withinDistance = trainData.metersTravelled >= spawnPosition && rightMostSprite.bounds.max.x > canvasBounds.left;
        }
        else
        {
            withinDistance = trainData.metersTravelled > parentSpawner.startSpawnDistance && trainData.metersTravelled < parentSpawner.endSpawnDistance;
        }

        bool inCanvasBounds =  spriteRenderer.bounds.min.x < canvasBounds.right && spriteRenderer.bounds.max.x > canvasBounds.left;
        parallaxActive = withinDistance || inCanvasBounds;
        
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
        yield return new WaitUntil(()=> parallaxActive);
        Initialize();
        while (parallaxActive)
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
