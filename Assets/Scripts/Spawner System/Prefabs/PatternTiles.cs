using UnityEngine;

public class PatternTiles : LoopingTiles
{
    [Header("Parameters")]
    public int tileCount = 1;
    private void Awake()
    {
        spriteRenderer.drawMode = SpriteDrawMode.Tiled;
        spriteRenderer.size = new Vector2(spriteRenderer.size.x * tileCount, spriteRenderer.size.y);
        transform.localScale = Vector3.one;   
    }
    public override void Start()
    {
        base.Start();
    }
}
