using UnityEngine;

[CreateAssetMenu(fileName = "CursorData", menuName = "Data / Cursor Data")]
public class CursorData : ScriptableObject
{
    public AudioClip hoverSFX;
    public AudioClip clickSFX;

    public float[] buttonDepths;

    public Bounds cursorBounds;

    public float curDepth;
    public float prevDepth;

    public int hoverCount;

    public bool changeButton;
    public bool isHovering;
    private void OnEnable()
    {
        buttonDepths = new float[8];
    }
    public bool IsInsideBounds(Bounds inputBounds, bool isClickable)
    {
        bool inside = cursorBounds.min.x >= inputBounds.min.x && cursorBounds.min.x <= inputBounds.max.x && cursorBounds.max.y >= inputBounds.min.y && cursorBounds.max.y <= inputBounds.max.y;
        if (inside && isClickable && hoverCount < buttonDepths.Length)
        {
            buttonDepths[hoverCount] = inputBounds.min.z;
            hoverCount++;
        }

        return inside;
    }
    public void CheckButtonResults()
    {
        if (hoverCount == 0)
        {
            curDepth = 0;
            prevDepth = 0;
            isHovering = false;
            return;
        }

        for (int i = 0; i < hoverCount; i++)
        {
            float depth = buttonDepths[i];
            if (depth < curDepth)
            {
                curDepth = depth;
            }
        }

        if (curDepth != prevDepth)
        {
            changeButton = true;
            prevDepth = curDepth;
        }
        hoverCount = 0;
        curDepth = 0;
        isHovering = true;
    }
}
