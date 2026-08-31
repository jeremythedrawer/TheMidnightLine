using UnityEngine;
using static Atlas;

[CreateAssetMenu(fileName = "CursorData", menuName = "Data / Cursor Data")]
public class CursorData : ScriptableObject
{
    public InputData inputData;
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
    public bool IsInsideSprite(AtlasRenderer renderer, bool isClickable)
    {
        Bounds inputBounds = renderer.bounds;
        bool inside = cursorBounds.min.x >= inputBounds.min.x && cursorBounds.min.x <= inputBounds.max.x && cursorBounds.max.y >= inputBounds.min.y && cursorBounds.max.y <= inputBounds.max.y;

        if (!inside) return false;

        SimpleSprite sprite = renderer.sprite;
        Texture2D tex = renderer.batchKey.texture;
        float u = Mathf.InverseLerp(renderer.bounds.min.x, renderer.bounds.max.x, inputData.mouseWorldPos.x);
        float v = Mathf.InverseLerp(renderer.bounds.min.y, renderer.bounds.max.y, inputData.mouseWorldPos.y);
        
        u *= sprite.uvSizeAndPos.x;
        u += sprite.uvSizeAndPos.z;

        v *= sprite.uvSizeAndPos.y;
        v += sprite.uvSizeAndPos.w;

        int pixelX = Mathf.FloorToInt(u * tex.width);
        int pixelY = Mathf.FloorToInt(v * tex.height);

        Color pixel = tex.GetPixel(pixelX, pixelY);
        if (pixel.a == 0) return false;

        if (isClickable && hoverCount < buttonDepths.Length)
        {
            buttonDepths[hoverCount] = inputBounds.min.z;
            hoverCount++;
        }

        return true;
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
