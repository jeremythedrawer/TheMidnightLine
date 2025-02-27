using System.Collections;
using UnityEngine;

public class SpawnedPrefab : MonoBehaviour
{
    public SpriteRenderer spriteRenderer {  get; private set; }
    public ParallaxController parallaxController { get; private set; }
    public Spawner spawner {  get; private set; }

    private TrainData trainData => GlobalReferenceManager.Instance.trainData;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        parallaxController = GetComponent<ParallaxController>();
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
