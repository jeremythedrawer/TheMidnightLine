using UnityEngine;

public class PatternTiles : LoopingTiles
{
    [Header("Parameters")]
    public int tileCount = 1;
    private void Awake()
    {
    }
    public override void Start()
    {
        base.Start();
        spriteRenderer.drawMode = SpriteDrawMode.Tiled;
        spriteRenderer.size = new Vector2(spriteRenderer.size.x * tileCount, spriteRenderer.size.y);
        transform.localScale = Vector3.one;   
    }
}
