using System.Collections;
using UnityEngine;

public class SpawnedBGPrefab : SpawnedPrefab
{
    public Sprite[] spriteLods;

    private CanvasBounds canvasBounds;
    private ParallaxController parallaxController;
    private string buildingName => gameObject.name.Replace("(Clone)", "");

    private void OnEnable()
    {
        parallaxController = GetComponent<ParallaxController>();
        canvasBounds = GameObject.FindFirstObjectByType<CanvasBounds>();
        StartCoroutine(SetLifeTime());
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
            Spawner.Instance.buildingPools[buildingName].Release(this);
        }
    }

    public override void Initialize()
    {
        ChooseLOD();
    }

    private void ChooseLOD()
    {
        if (transform.position.z < canvasBounds.oneThirdClipPlane)
        {
            spriteRenderer.sprite = spriteLods[0];
        }
        else if (transform.position.z > canvasBounds.twoThirdsClipPlane)
        {
            spriteRenderer.sprite = spriteLods[2];
        }
        else
        {
            spriteRenderer.sprite = spriteLods[1];
        }
    }
}
