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
    public virtual void Initialize()
    {
        parallaxController.Initialize();
    }
}
