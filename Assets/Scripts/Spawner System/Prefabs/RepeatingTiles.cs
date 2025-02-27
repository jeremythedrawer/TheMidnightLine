using UnityEngine;

public class RepeatingTiles : LoopingTiles
{
    public override void Start()
    {
        base.Start();
        spriteRenderer.drawMode = SpriteDrawMode.Tiled;
        spriteRenderer.size = new Vector2(canvasBounds.width, spriteRenderer.size.y);
        transform.localScale = Vector3.one;
    }
}
