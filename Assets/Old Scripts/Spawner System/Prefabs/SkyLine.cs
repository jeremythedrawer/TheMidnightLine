using UnityEngine;

public class SkyLine : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public ParallaxController parallaxController;
    public CanvasBounds canvasBounds;

    [Header("Parameters")]
    public float lengthInMeters  = 1000f;
    public float positionFromStart = 0f;
    public bool isLastSkyLine;

    private float endAddOn;

#if UNITY_EDITOR
    public void ApplyTileAndOffset()
    {
        endAddOn = isLastSkyLine? canvasBounds.width : 0;
        spriteRenderer.size = new Vector2(lengthInMeters * parallaxController.parallaxFactor + endAddOn, spriteRenderer.size.y);
        transform.localScale = Vector2.one;

        transform.localPosition = new Vector3(positionFromStart * parallaxController.parallaxFactor, transform.position.y, transform.position.z);
    }
#endif
}
