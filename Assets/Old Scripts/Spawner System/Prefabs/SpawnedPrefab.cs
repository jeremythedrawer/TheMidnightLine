using System.Collections;
using UnityEngine;

public class SpawnedPrefab : MonoBehaviour
{
    public SpriteRenderer spriteRenderer {  get; private set; }
    public ParallaxController parallaxController { get; private set; }
    public Spawner spawner {  get; private set; }

    private void Start()
    {
        SetComponents();
        
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
        SetComponents();
        parallaxController.Initialize();
    }

    public virtual IEnumerator SetLifeTime()
    {
        yield return null;
    }

    private void SetComponents()
    {
       if (spriteRenderer  == null) spriteRenderer = GetComponent<SpriteRenderer>();
       if (parallaxController  == null) parallaxController = GetComponent<ParallaxController>();
       if (spawner == null) spawner = gameObject.GetComponentInParent<Spawner>();
    }
}
