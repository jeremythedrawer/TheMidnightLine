using System.Collections;
using UnityEngine;

public class SpawnedPrefab : MonoBehaviour
{
    [Header ("References")]
    public SpriteRenderer spriteRenderer;
    public ParallaxController parallaxController;
    public CanvasBounds canvasBounds {  get; private set; }

    private void Awake()
    {
        canvasBounds = GameObject.FindFirstObjectByType<CanvasBounds>();
    }

    private void OnEnable()
    {
        StartCoroutine(SetLifeTime());
    }


    private void OnDisable()
    {
        StopCoroutine(SetLifeTime());
    }


    public virtual void Initialize()
    {
        parallaxController.Initialize();
    }

    public virtual IEnumerator SetLifeTime()
    {
        yield return null;
    }
}
