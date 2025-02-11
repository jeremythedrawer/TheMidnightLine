using System.Collections;
using UnityEngine;

public class SpawnedPrefab : MonoBehaviour
{
    [Header ("References")]
    public SpriteRenderer spriteRenderer;
    public ParallaxController parallaxController;
    public Spawner spawner {  get; private set; }

    private void Awake()
    {
        spawner = gameObject.GetComponentInParent<Spawner>();
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
