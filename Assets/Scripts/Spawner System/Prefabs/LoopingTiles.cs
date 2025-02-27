using System.Collections;
using UnityEngine;

public class LoopingTiles : MonoBehaviour
{
    public CanvasBounds canvasBounds => GlobalReferenceManager.Instance.canvasBounds;
    public SpriteRenderer spriteRenderer {  get; private set; }
    public ParallaxController parallaxController {  get; private set; }

    public float spriteBoundsMaxX {  get; private set; }

    public virtual void Start()
    {
        parallaxController = GetComponent<ParallaxController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        spriteBoundsMaxX = spriteRenderer.bounds.max.x;
    }
}
