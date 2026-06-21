using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif

using static Atlas;
using static AtlasRendering;
using static AtlasUI;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;

[ExecuteAlways]
public class AtlasTextRenderer : MonoBehaviour
{
    public AtlasTextRendererType rendererType;

    public BatchKey batchKey;
    public TextAtlas textAtlas;

    [TextArea(3, 10)] public string text;
    public float kerning = 1.1f;
    public float spacing = 1;
    public AtlasTextAlignmentType alignmentType;

    [Header("Scroll Settings")]
    public float scrollSpeed;
    public float scrollBoundSize;

    [Header("Border Settings")]
    public AtlasRenderer background_renderer;

    [Header("Generated")]
    public CancellationTokenSource ctsWrite;

    public TextBoxData textBoxData;
    
    public Bounds bounds;

    public Vector4[] worldPivotsAndSizes;
    public Vector4[] uvSizesAndPositions;
    public Vector4[] scalesAndFlips;
    public Vector4[] customs;

    public Vector3 boundsOffset;
    
    public bool hasText;

    [Header("Border Generated")]
    public Vector3 borderLocalPos;

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
            case AtlasTextRendererType.Simple:
            {
            }
            break;
            case AtlasTextRendererType.Scroll:
            {
                bounds = GetBounds(text);
                SetScrollingText();
            }
            break;
            case AtlasTextRendererType.Border:
            {
                bounds = GetBounds(text);
                SetBorderText();
            }
            break;
        }
    }
    public void UpdateAppearTextAlpha(float normAmount, bool appear, ref float clock)
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
    public void SetAppearTextAlpha(float normAmount)
    {
        for (int i = 0; i < customs.Length; i++)
        {
            customs[i].w = normAmount;
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
    private void SetScrollingText()
    {
        customs = new Vector4[worldPivotsAndSizes.Length];
        for (int i = 0; i < customs.Length; i++)
        {
            Vector4 custom = customs[i];
            custom.x = bounds.size.x;
            custom.y = scrollBoundSize;
            custom.z = scrollSpeed;
            custom.w = scrollSpeed < 0 ? 1 : 0;
            customs[i] = custom; 
        }
    }
    private void SetBorderText()
    {
        borderLocalPos.z = background_renderer.transform.localPosition.z;

        switch (alignmentType)
        {
            case AtlasTextAlignmentType.Left:
            {
                borderLocalPos.x = -background_renderer.worldPivotsAndSizes[0].z;
            }
            break;
            case AtlasTextAlignmentType.Center:
            {
                borderLocalPos.x = -bounds.extents.x - background_renderer.worldPivotsAndSizes[0].z;

            }
            break;
            case AtlasTextAlignmentType.Right:
            {

                borderLocalPos.x = -bounds.size.x - background_renderer.worldPivotsAndSizes[0].z;
            }
            break;
        }

        borderLocalPos.y = -background_renderer.worldPivotsAndSizes[0].w - bounds.size.y;

        if (!hasText)
        {
            background_renderer.enabled = false;
        }
        else
        {
            background_renderer.enabled = true;

            Vector2 worldSize = new Vector2();
            worldSize.x = bounds.size.x + BORDER_PADDING * 2;
            worldSize.y = bounds.size.y + BORDER_PADDING * 2;

            background_renderer.transform.localPosition = borderLocalPos;

            background_renderer.SetNineSliceSizeFromWorldSpace(worldSize, background_renderer.atlas.slicedSprites[background_renderer.spriteIndex]);
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

            float letterPos = curPosX * kerning;
            float right = letterPos + sprite.worldSize.x;

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

    public void WriteText(string text, float writeLetterTime)
    {
        ctsWrite?.Cancel();
        ctsWrite = new CancellationTokenSource();

        WritingText(text, writeLetterTime).Forget();
    }
    private async UniTask WritingText(string text, float writeLetterTime)
    {
        int stationNameLetterCount = text.Length;
        int curLetterIndex = 0;

        string curStationString = "";
        SetText(curStationString);

        try
        {
            while (curLetterIndex < stationNameLetterCount)
            {
                curStationString += text[curLetterIndex];
                await UniTask.WaitForSeconds(writeLetterTime, cancellationToken: ctsWrite.Token);
                SetText(curStationString);
                curLetterIndex++;
            }
        }
        catch (OperationCanceledException) { }
    }

    public void EraseText(string text, float writeLetterTime)
    {
        ctsWrite?.Cancel();
        ctsWrite = new CancellationTokenSource();
        ErasingText(text, writeLetterTime).Forget();
    }
    private async UniTask ErasingText(string text, float writeLetterTime)
    {
        string curStationString = text;
        try
        {
            while (curStationString.Length > 0)
            {
                await UniTask.WaitForSeconds(writeLetterTime, cancellationToken: ctsWrite.Token);
                curStationString = curStationString[..^1];
                SetText(curStationString);
            }
        }
        catch (OperationCanceledException) { }
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
        switch(rendererType)
        {
            case AtlasTextRendererType.Simple:
            {
            }
            break;
        }
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(AtlasTextRenderer))]
public class AtlasTextRendererEditor : Editor
{
    BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

    private void OnSceneGUI()
    {
        AtlasTextRenderer textRend = (AtlasTextRenderer)target;

        switch(textRend.rendererType)
        {
            case AtlasTextRendererType.Simple:
            {

            }
            break;
            case AtlasTextRendererType.Scroll:
            {
                float scrollBoundsXPos = textRend.transform.position.x + textRend.scrollBoundSize * 0.5f;
                boundsHandle.center = new Vector3(scrollBoundsXPos, textRend.bounds.center.y, textRend.transform.position.z);
                boundsHandle.size = new Vector2(textRend.scrollBoundSize, textRend.bounds.size.y);
                boundsHandle.SetColor(Color.red);
                boundsHandle.DrawHandle();

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(textRend, "Resize Bounds");

                    textRend.scrollBoundSize = boundsHandle.size.x;
                    textRend.SetText(textRend.text);
                }
            }
            break;
        }

    }
}
#endif