using System.Collections;
using UnityEngine;

public class LoopingTiles : MonoBehaviour
{
    [Header("References")]
    public CanvasBounds canvasBounds;

    public ParallaxController parallaxController {  get; private set; }
    public SpriteRenderer spriteRenderer {  get; private set; }
    public float endPosX {  get; private set; }

    private void Start()
    {
        parallaxController = GetComponent<ParallaxController>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.drawMode = SpriteDrawMode.Tiled;
        spriteRenderer.size = new Vector2(canvasBounds.width, spriteRenderer.size.y);
        transform.localScale = Vector3.one;
        
    }
    private void Update()
    {
        endPosX = transform.position.x + spriteRenderer.size.x;
        
    }
}
