using UnityEngine;

public class RampBounds : MonoBehaviour
{
    private PolygonCollider2D polyCollider2D;
    private ExitBounds exitBounds;
    private DisableBounds disableBounds;
    private Transform parentTransform;
    private SpriteRenderer[] spriteRenderers;

    private void OnValidate()
    {
        SetRamp();
    }
    private void Awake()
    {
        SetRamp();
    }

    private void SetRamp()
    {
        if (!gameObject.scene.isLoaded) return;
        polyCollider2D = GetComponent<PolygonCollider2D>();
        exitBounds = GetComponentInChildren<ExitBounds>();
        disableBounds = GetComponentInChildren<DisableBounds>();
        parentTransform = transform.parent.transform;
        spriteRenderers = parentTransform.GetComponentsInChildren<SpriteRenderer>();
        if (exitBounds == null) return;
        float exitBoundsWidth = exitBounds.boxCollider2D.size.x / 2f;
        float bottomBounds = FindBottomBounds();


        Vector2[] rampPoints = new Vector2[]
        {
            new Vector2(-exitBoundsWidth,0),
            new Vector2(exitBoundsWidth,0),
            new Vector2(exitBoundsWidth, bottomBounds),
            new Vector2(-50,bottomBounds)

        };

        polyCollider2D.points = rampPoints;
        disableBounds.transform.localPosition = new Vector2(-50, bottomBounds);
    }
    private float FindBottomBounds()
    {
        float bottomBounds = float.MaxValue;


        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer.bounds.min.y < bottomBounds)
            {
                bottomBounds = spriteRenderer.bounds.size.y;
            }
        }
        return -bottomBounds;
    }
}
