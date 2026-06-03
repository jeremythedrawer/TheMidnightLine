using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static Atlas;
using static AtlasRendering;

[ExecuteAlways]
public class AtlasTextRenderer : MonoBehaviour
{
    public AtlasTextRendererType rendererType;
    public BatchKey batchKey;
    public TextAtlas textAtlas;

    [TextArea(3, 10)] public string text;
    public float kerning = 1.1f;
    public float spacing = 1;
    public float scrollSize;
    public AtlasTextAlignmentType alignmentType;

    [Header("Generated")]
    public Bounds bounds;
    public TextBoxData textBoxData;

    public Vector3 boundsOffset;

    public Vector4[] worldPivotsAndSizes;
    public Vector4[] uvSizesAndPositions;
    public Vector4[] scalesAndFlips;
    public Vector4[] customs;
    public bool hasText;

    private void OnValidate()
    {
        SetText(text);
        bounds = GetBounds(text);
    }
    private void OnEnable()
    {
        batchKey.texture = textAtlas.atlas.texture;
        textAtlas?.SetWorldSpaceLineHeight();
        RegisterTextRenderer(this);
    }
    private void OnDisable()
    {
        UnregisterTextRenderer(this);
    }
    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            bounds = GetBounds(text);
            UpdateDepthEditor();
        }
#endif
    }
    public void SetText(string inputText)
    {
        if (inputText == null) return;
        text = inputText;

        int maxChars = text.Length;

        worldPivotsAndSizes = new Vector4[maxChars];
        uvSizesAndPositions = new Vector4[maxChars];
        scalesAndFlips = new Vector4[maxChars];
        customs = new Vector4[maxChars];

        SetTextWorld();
        switch (rendererType)
        {
            case AtlasTextRendererType.SimpleWorld:
            {
            }
            break;
            case AtlasTextRendererType.ScrollWorld:
            {
                SetScrollingText();
            }
            break;
        }
    }
    public void AppearText(float normAmount, bool appear, ref float clock)
    {
        if (appear)
        {
            if (clock > 0)
            {
                clock -= Time.deltaTime;
                float t = (clock / APPEAR_TEXT_TIME) * normAmount + normAmount;

                for (int i = 0; i < customs.Length; i++)
                {
                    customs[i].w = t;
                }
            }
        }
        else
        {
            if (clock < APPEAR_TEXT_TIME)
            {
                clock += Time.deltaTime;
                float t = (clock / APPEAR_TEXT_TIME) * normAmount + normAmount;

                for (int i = 0; i < customs.Length; i++)
                {
                    customs[i].w = t;
                }
            }

        }
    }
    public void SetTextAlpha(float alpha)
    {
        for (int i = 0; i < customs.Length; i++)
        {
            customs[i].w = alpha;
        }
    }
    public void UpdateBounds()
    {
        bounds.center = transform.position + boundsOffset;
    }
    public void UpdateDepthRealtime(int newDepth)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, newDepth);
    }
    public void UpdateDepthEditor()
    {
        if ((int)transform.position.z != batchKey.depthOrder)
        {
            RegisterTextRenderer(this);
        }
    }
    public void SetScrollingText()
    {
        customs = new Vector4[worldPivotsAndSizes.Length];
        for (int i = 0; i < customs.Length; i++)
        {
            customs[i].x = bounds.size.x;
            customs[i].y = scrollSize;
        }
    }
    public void SetTextWorld()
    {
        textBoxData = GetTextBoxData(text);

        float newLineOffsetScale = 0;

        switch(alignmentType)
        {
            case AtlasTextAlignmentType.Left:
            {
                newLineOffsetScale = 0;
            }
            break;
            case AtlasTextAlignmentType.Center:
            {
                newLineOffsetScale = 0.5f;
            }
            break;
            case AtlasTextAlignmentType.Right:
            {
                newLineOffsetScale = 1f;
            }
            break;
        }

        int spriteIndex = 0;
        int maxChars = text.Length;
        int curLineIndex = 0;

        float startLineWidth = textBoxData.lineWidths[curLineIndex];

        float curPosX = -(startLineWidth * newLineOffsetScale);
        float curPosY = -textAtlas.typeWorldHeight;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == ' ')
            {
                curPosX += spacing;
                continue;
            }
            if (c == '\n')
            {
                curPosY -= textAtlas.typeWorldHeight;
                
                curLineIndex++;
                float lineWidth = textBoxData.lineWidths[curLineIndex];
                
                curPosX = -(lineWidth * newLineOffsetScale);
                continue;
            }

            int letterIndex = c - 33;

            if (letterIndex < 0) continue;
            SimpleSprite sprite = textAtlas.atlas.simpleSprites[letterIndex];

            float letterPos = curPosX * kerning;
            Vector4 worldPivotAndSize = new Vector4(letterPos, curPosY, sprite.worldSize.x, sprite.worldSize.y);
            worldPivotsAndSizes[spriteIndex] = worldPivotAndSize;

            float widthRatio = sprite.worldSize.x / LETTER_ADVANCE;
            float advanceScale = widthRatio * 0.5f + 0.5f;
            curPosX += LETTER_ADVANCE * advanceScale;

            uvSizesAndPositions[spriteIndex] = sprite.uvSizeAndPos;
            scalesAndFlips[spriteIndex] = Vector4.one;
            spriteIndex++;

        }
        if (maxChars == 0)
        {
            bounds.size = Vector3.zero;
            bounds.center = transform.position;
            boundsOffset = Vector3.zero;
            hasText = false;
        }
        else
        {
            hasText = true;
        }

    }
    public Bounds GetBounds(string text)
    {
        Bounds bounds = new Bounds();

        TextBoxData textBoxData = GetTextBoxData(text);

        bounds.size = textBoxData.size;
        
        switch (alignmentType)
        {
            case AtlasTextAlignmentType.Left:
            {
                boundsOffset = new Vector3(textBoxData.size.x * 0.5f, -textBoxData.size.y * 0.5f, 0f);
            }
            break;

            case AtlasTextAlignmentType.Center:
            {
                boundsOffset = new Vector3(0, -textBoxData.size.y * 0.5f, 0f);
            }
            break;

            case AtlasTextAlignmentType.Right:
            {
                boundsOffset = new Vector3(-textBoxData.size.x * 0.5f, -textBoxData.size.y * 0.5f, 0f);
            }
            break;
        }
        bounds.center = transform.position + boundsOffset;

        return bounds;
    }
    private TextBoxData GetTextBoxData(string text)
    {
        float maxX = 0;
        float curPosX = 0;

        float curPosY = -textAtlas.typeWorldHeight;
        List<float> linesWidths = new List<float>();

        float maxLineX = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == ' ')
            {
                curPosX += spacing;
                continue;
            }
            if (c == '\n')
            {
                linesWidths.Add(maxLineX / kerning);
                maxLineX = 0;   
                curPosX = 0;
                curPosY -= textAtlas.typeWorldHeight;
                continue;
            }
            
            int letterIndex = c - 33;
            
            if (letterIndex < 0) continue;

            SimpleSprite sprite = textAtlas.atlas.simpleSprites[letterIndex];

            float letterPos = curPosX * kerning; // * camStates.worldUnitsPerPixel
            float right = letterPos + sprite.worldSize.x; // * camStates.worldUnitsPerPixel * PIXELS_PER_UNIT

            if (right > maxX) maxX = right;
            if (right > maxLineX) maxLineX = right;

            float widthRatio = sprite.worldSize.x / LETTER_ADVANCE;
            float advanceScale = widthRatio * 0.5f + 0.5f;
            curPosX += LETTER_ADVANCE * advanceScale;
        }

        linesWidths.Add(maxLineX / kerning);

        return new TextBoxData()
        {
            size = new Vector2 (maxX, -curPosY),
            lineWidths = linesWidths.ToArray(),
        };
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.clear;
        Gizmos.DrawCube(bounds.center, bounds.size);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.indigo;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
#endif
}
