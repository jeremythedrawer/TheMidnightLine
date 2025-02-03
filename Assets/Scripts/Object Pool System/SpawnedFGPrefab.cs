using System.Collections;
using UnityEngine;

public class SpawnedFGPrefab : SpawnedPrefab
{
    public float endPosX {  get; private set; }

    public override IEnumerator SetLifeTime()
    {
        while (canvasBounds == null || endPosX == 0)
        {
            yield return null;
        }
        yield return null;
        yield return new WaitUntil(() => endPosX < canvasBounds.despawnPoint.x);

        if (gameObject != null && gameObject.activeSelf)
        {
            ForgroundSpawner.Instance.prefabPool.Release(this);
        }
    }

    private void Update()
    {
        endPosX = transform.position.x + spriteRenderer.size.x;
        
    }
    public override void Initialize()
    {
        base.Initialize();
        spriteRenderer.size = new Vector2 (canvasBounds.width, spriteRenderer.size.y);
    }
}
