using System.Collections;
using UnityEngine;

public class SpawnedBuilding : MonoBehaviour
{
    private CanvasBounds canvasBounds;

    private ParallaxController parallaxController;
    private string buildingName => gameObject.name.Replace("(Clone)", "");

    private void Awake()
    {
    }
    private void OnEnable()
    {
        parallaxController = GetComponent<ParallaxController>();
        canvasBounds = GameObject.FindFirstObjectByType<CanvasBounds>();
        StartCoroutine(SetLifeTime());
    }

    private void Start()
    {
        
    }
    private void OnDisable()
    {
        StopCoroutine(SetLifeTime());
    }

    private IEnumerator SetLifeTime()
    {
        while (canvasBounds == null)
        {
            yield return null;
        }
        yield return new WaitUntil(() => transform.position.x < canvasBounds.despawnPoint.x);

        if (gameObject != null && gameObject.activeSelf)
        {
            BackgroundSpawner.Instance.buildingPools[buildingName].Release(this);
        }
    }

    public void Initialize()
    {
        parallaxController.Initialize();
    }
}
