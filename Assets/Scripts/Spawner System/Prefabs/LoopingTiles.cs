using System.Collections;
using UnityEngine;

public class LoopingTiles : MonoBehaviour
{
    public CanvasBounds canvasBounds => GlobalReferenceManager.Instance.canvasBounds;
    public SpriteRenderer spriteRenderer;
    public ParallaxController parallaxController {  get; private set; }
    public float endPosX {  get; private set; }

    public virtual void Start()
    {
        parallaxController = GetComponent<ParallaxController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    private void Update()
    {
        endPosX = transform.position.x + spriteRenderer.size.x;   
    }
}
