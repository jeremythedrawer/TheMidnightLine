using UnityEngine;

[CreateAssetMenu(fileName = "CursorData", menuName = "Data / Cursor Data")]
public class CursorData : MonoBehaviour
{
    public Bounds bounds;
    public bool canClick;

    public bool IsInsideBounds(Bounds inputBounds, bool isClickable)
    {
        bool inside = bounds.min.x >= inputBounds.min.x && bounds.min.x <= inputBounds.max.x && bounds.max.y >= inputBounds.min.y && bounds.max.y <= inputBounds.max.y;
        if (inside && isClickable) canClick = true;

        return inside;
    }
}
