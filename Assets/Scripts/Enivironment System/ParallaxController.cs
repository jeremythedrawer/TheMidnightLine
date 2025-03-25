using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.U2D;

public class ParallaxController : MonoBehaviour
{
    [HideInInspector] public float spawnPosition;
    private Camera cam => GlobalReferenceManager.Instance.mainCam;
    private TrainData trainData => GlobalReferenceManager.Instance.trainData;
    private CanvasBounds canvasBounds => GlobalReferenceManager.Instance.canvasBounds;
    private Spawner parentSpawner;
    private Vector2 startPos;
    private float startZ;

    private Renderer[] edgeRenderers;
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
        isOneShot = parentSpawner is OneShotSpawner;

        List<Renderer> allRenderers = new List<Renderer>(GetComponents<Renderer>());
        allRenderers.AddRange(GetComponentsInChildren<Renderer>());

        if (allRenderers.Count > 1)
        {
            Renderer leftMost = allRenderers[0];
            Renderer rightMost = allRenderers[0];

            foreach (Renderer r in allRenderers)
            {
                if (r.bounds.min.x < leftMost.bounds.min.x) leftMost = r;
                if (r.bounds.max.x > rightMost.bounds.max.x) rightMost = r;
            }

            edgeRenderers = new Renderer[] { leftMost, rightMost };
        }
        else
        {
            edgeRenderers = allRenderers.ToArray();
        }
    }
    private void OnEnable()
    {
        ScrollObject();
    }

    private void Update()
    {
        if (trainData == null || parentSpawner == null) return;

        Renderer leftRenderer = edgeRenderers[0];
        Renderer rightRenderer = edgeRenderers.Length > 1 ? edgeRenderers[^1] : leftRenderer;

        if (isOneShot)
        {
            withinDistance = trainData.metersTravelled >= spawnPosition && rightRenderer.bounds.max.x > canvasBounds.left;
        }
        else
        {
            withinDistance = trainData.metersTravelled > parentSpawner.startSpawnDistance && trainData.metersTravelled < parentSpawner.endSpawnDistance;
        }


        bool inCanvasBounds = leftRenderer.bounds.min.x < canvasBounds.right &&
                              rightRenderer.bounds.max.x > canvasBounds.left;

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
