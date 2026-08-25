using System;
using System.Threading;
using System.Collections.Generic;

using UnityEngine;

using Cysharp.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif

using static Atlas;
using static AtlasRendering;
using static AtlasUI;

[ExecuteAlways]
public class AtlasTextRenderer : MonoBehaviour
{
    public enum AtlasTextRendererType
    {
        Simple,
        Scroll,
        Border,
    }
    public enum AtlasTextAlignmentType
    {
        Left,
        Center,
        Right,
    }
    [Serializable]
    public struct TextBoxData
    {
        public Vector2 size;
        public float[] wordPosXArray;
        public float[] lineWidthArray;
    }

    public AtlasTextRendererType rendererType;

    public BatchKey batchKey;
    public TextAtlas textAtlas;

    [TextArea(3, 10)] public string text;
    public float kerning = 1.1f;
    public float spacing = 1;
    public AtlasTextAlignmentType alignmentType;
    public Color color;

    [Header("Scroll Settings")]
    public float scrollSpeed;
    public float scrollBoundSize;

    [Header("Border Settings")]
    public AtlasRenderer backgroundRenderer;

    [Header("Generated")]
    public CancellationTokenSource ctsWrite;
    public CancellationTokenSource ctsChangeCustom;


    public TextBoxData textBoxData;
    
    public Bounds bounds;

    public Vector4[] worldPivotsAndSizes;
    public Vector4[] uvSizesAndPositions;
    public Vector4[] scalesAndFlips;
    public Vector4[] customs;

    public Vector3 boundsOffset;

    public float curAlpha;

    public int customBit;

    public bool hasText;
    public bool erasingText;
    public bool completedWritingText;
    [Header("Border Generated")]
    public Vector3 borderLocalPos;

    public delegate void OnCompletedWritingText();

    private void OnValidate()
    {
        if (textAtlas == null) return;
        if (batchKey.material == null) return;
        SetTextWorld(text);

        bounds = GetBoundsNewText(text);
    }
    private void OnEnable()
    {
        batchKey.texture = textAtlas?.atlas?.texture;
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
            if (textAtlas == null) return;
            bounds = GetBoundsNewText(text);
        }
#endif
    }
    public void SetText(string inputText, float alpha = 1)
    {
        SetTextWorld(inputText);
        
        switch (rendererType)
        {
            case AtlasTextRendererType.Simple:
            {
                SetColorText(color);
            }
            break;
            case AtlasTextRendererType.Scroll:
            {
                bounds = GetBoundsNewText(text);
                SetScrollingText();
            }
            break;
            case AtlasTextRendererType.Border:
            {
                bounds = GetBoundsNewText(text);
                SetColorText(color);
                SetBorderText();
            }
            break;
        }
    }
    public void SetAlignmentType(AtlasTextAlignmentType newAlignType)
    {
        alignmentType = newAlignType;
    }
    public void SetAppearTextAlpha(float normAmount)
    {
        for (int i = 0; i < customs.Length; i++)
        {
            customs[i].w = normAmount;
        }
    }
    public void SetBounds()
    {
        bounds.center = transform.position + boundsOffset;
    }
    public void UpdateDepthRealtime(int newDepth)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, newDepth);
    }
    private void SetScrollingText()
    {
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
    public void SetColorText(Color color)
    {
        Color linearColor = color.linear;

        for (int i = 0; i < customs.Length; i++)
        {
            Vector4 custom = customs[i];
            custom.x = linearColor.r;
            custom.y = linearColor.g;
            custom.z = linearColor.b;
            customs[i] = custom;
        }
    }
    private void SetBorderText()
    {
        borderLocalPos.z = backgroundRenderer.transform.localPosition.z;

        switch (alignmentType)
        {
            case AtlasTextAlignmentType.Left:
            {
                borderLocalPos.x = -backgroundRenderer.worldPivotsAndSizes[0].z;
            }
            break;
            case AtlasTextAlignmentType.Center:
            {
                borderLocalPos.x = -bounds.extents.x - backgroundRenderer.worldPivotsAndSizes[0].z;

            }
            break;
            case AtlasTextAlignmentType.Right:
            {

                borderLocalPos.x = -bounds.size.x - backgroundRenderer.worldPivotsAndSizes[0].z;
            }
            break;
        }

        borderLocalPos.y = -backgroundRenderer.worldPivotsAndSizes[0].w - bounds.size.y;

        if (!hasText)
        {
            backgroundRenderer.enabled = false;
        }
        else
        {
            backgroundRenderer.enabled = true;

            Vector2 worldSize = new Vector2();
            worldSize.x = bounds.size.x + BORDER_PADDING * 2;
            worldSize.y = bounds.size.y + BORDER_PADDING * 2;

            backgroundRenderer.transform.localPosition = borderLocalPos;

            backgroundRenderer.SetNineSliceSizeFromWorldSpace(worldSize, backgroundRenderer.atlas.slicedSprites[backgroundRenderer.spriteIndex]);
        }
    }
    public void SetTextWorld(string inputText)
    {
        if (inputText == null) return;
        text = inputText;

        int maxChars = text.Length;

        worldPivotsAndSizes = new Vector4[maxChars];
        uvSizesAndPositions = new Vector4[maxChars];
        scalesAndFlips = new Vector4[maxChars];
        customs = new Vector4[maxChars];

        string[] words = text.Split(' ');
        
        textBoxData.wordPosXArray = new float[words.Length];
        
        List<string> linesTextList = new List<string>();
        List<float> lineWidthList = new List<float>();

        string curLineText = "";
        float curLineWidth = 0f;

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];

            float wordWidth = 0f;

            for (int j = 0; j < word.Length; j++)
            {
                char c = word[j];

                int letterIndex = c - 33;

                if (letterIndex < 0) continue;

                SimpleSprite sprite = textAtlas.atlas.simpleSprites[letterIndex];

                wordWidth += sprite.worldSize.x + kerning;
            }

            float spacingWidth = curLineText.Length > 0 ? spacing : 0f;
            float newLineWidth = curLineWidth + spacingWidth + wordWidth;


            if (curLineText.Length > 0 && newLineWidth > textBoxData.size.x)
            {
                linesTextList.Add(curLineText);
                lineWidthList.Add(newLineWidth - wordWidth - spacingWidth);
                curLineText = word;
                curLineWidth = wordWidth;
            }
            else
            {
                if (curLineText.Length > 0) curLineText += " ";

                curLineText += word;
                curLineWidth = newLineWidth;
            }
        }

        if (curLineText.Length > 0)
        {
            linesTextList.Add(curLineText);
            lineWidthList.Add(curLineWidth);
        }

        textBoxData.lineWidthArray = lineWidthList.ToArray();

        float curPosY = -textAtlas.typeWorldHeight;
        int spriteIndex = 0;


        for (int i = 0; i < linesTextList.Count; i++)
        {
            string line = linesTextList[i];
            float lineWidth = lineWidthList[i];

            float curPosX = 0;

            if (alignmentType == AtlasTextAlignmentType.Center)
            {
                curPosX = -(lineWidth * 0.5f);
            }
            else if (alignmentType == AtlasTextAlignmentType.Right)
            {
                curPosX = -lineWidth;
            }

            for (int  j = 0; j < line.Length; j++)
            {
                char c = line[j];

                if (c == ' ')
                {
                    curPosX += spacing;
                    continue;
                }

                int letterIndex = c - 33;

                if (letterIndex < 0) continue;

                SimpleSprite sprite = textAtlas.atlas.simpleSprites[letterIndex];

                Vector4 worldPivSize = new Vector4();

                worldPivSize.x = curPosX;
                worldPivSize.y = curPosY;
                worldPivSize.z = sprite.worldSize.x;
                worldPivSize.w = sprite.worldSize.y;

                worldPivotsAndSizes[spriteIndex] = worldPivSize;
                uvSizesAndPositions[spriteIndex] = sprite.uvSizeAndPos;
                scalesAndFlips[spriteIndex] = Vector4.one;
                customs[spriteIndex] = Vector4.one;

                spriteIndex++;

                curPosX += sprite.worldSize.x + kerning;
            }
            curPosY -= textAtlas.typeWorldHeight;
        }

        textBoxData.size.y = curPosY + textAtlas.typeWorldHeight; 
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
    public void CancelWriting()
    {
        ctsWrite?.Cancel();
        ctsWrite = new CancellationTokenSource();
    }
    public void WriteText(string text, float writeLetterTime, OnCompletedWritingText callback = null, bool setTextIfCancelled = false)
    {
        CancelWriting();
        WritingText(text, writeLetterTime, callback, setTextIfCancelled).Forget();
    }
    public void EraseText(float writeLetterTime)
    {
        CancelWriting();
        erasingText = true;
        completedWritingText = false;
        ErasingText(writeLetterTime).Forget();
    }
    public void ChangeCustom(float time, float newValue, int customChannel)
    {
        ctsChangeCustom?.Cancel();
        ctsChangeCustom = new CancellationTokenSource();

        switch(customChannel)
        {
            case 1:
            {
                ChangingCustomX(time, newValue).Forget();

            }
            break;
            case 2:
            {
                ChangingCustomY(time, newValue).Forget();

            }
            break;
            case 3:
            {
                ChangingCustomZ(time, newValue).Forget();

            }
            break;
            case 4:
            {
                ChangingCustomW(time, newValue).Forget();

            }
            break;

        }
    }
    public Bounds GetBoundsCurrentText()
    {
        Bounds bounds = new Bounds();

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
    public Bounds GetBoundsNewText(string text)
    {
        Bounds bounds = new Bounds();

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

    private async UniTask WritingText(string text, float writeLetterTime, OnCompletedWritingText callback, bool setTextIfCancelled)
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
            completedWritingText = true;
            if (callback != null) callback();
        }
        catch (OperationCanceledException)
        {
            if (setTextIfCancelled)
            {
                SetText(text);
                completedWritingText = true;
                if (callback != null) callback();
            }
        }
    }
    private async UniTask ErasingText(float writeLetterTime)
    {
        string curText = text;
        try
        {
            while (curText.Length > 0)
            {
                await UniTask.WaitForSeconds(writeLetterTime, cancellationToken: ctsWrite.Token);
                curText = curText[..^1];
                SetText(curText);
            }
            erasingText = false;
        }
        catch (OperationCanceledException) 
        { 
            erasingText = false;
        }
    }
    private async UniTask ChangingCustomX(float time, float newValue)
    {
        try
        {
            float elapsed = 0;
            float startValue = customs[0].x;

            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / time;
                float curValue = Mathf.Lerp(startValue, newValue, t);
                for (int i = 0; i < customs.Length; i++)
                {
                    customs[i].x = curValue;
                }
                await UniTask.Yield(ctsChangeCustom.Token);
            }
            for (int i = 0; i < customs.Length; i++)
            {
                customs[i].x = newValue;
            }
        }
        catch (OperationCanceledException)
        {
            for (int i = 0; i < customs.Length; i++)
            {
                customs[i].x = newValue;
            }
        }
    }
    private async UniTask ChangingCustomY(float time, float newValue)
    {
        try
        {
            float elapsed = 0;
            float startValue = customs[0].y;

            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / time;
                float curValue = Mathf.Lerp(startValue, newValue, t);
                for (int i = 0; i < customs.Length; i++)
                {
                    customs[i].y = curValue;
                }
                await UniTask.Yield(ctsChangeCustom.Token);
            }
            for (int i = 0; i < customs.Length; i++)
            {
                customs[i].y = newValue;
            }
        }
        catch (OperationCanceledException)
        {
            for (int i = 0; i < customs.Length; i++)
            {
                customs[i].y = newValue;
            }
        }
    }
    private async UniTask ChangingCustomZ(float time, float newValue)
    {
        try
        {
            float elapsed = 0;
            float startValue = customs[0].z;

            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / time;
                float curValue = Mathf.Lerp(startValue, newValue, t);
                for (int i = 0; i < customs.Length; i++)
                {
                    customs[i].z = curValue;
                }
                await UniTask.Yield(ctsChangeCustom.Token);
            }
            for (int i = 0; i < customs.Length; i++)
            {
                customs[i].z = newValue;
            }
        }
        catch (OperationCanceledException)
        {
            for (int i = 0; i < customs.Length; i++)
            {
                customs[i].z = newValue;
            }
        }
    }
    private async UniTask ChangingCustomW(float time, float newValue)
    {
        try
        {
            float elapsed = 0;
            float startValue = customs[0].w;

            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / time;
                float curValue = Mathf.Lerp(startValue, newValue, t);
                for (int i = 0; i < customs.Length; i++)
                {
                    customs[i].w = curValue;
                }
                await UniTask.Yield(ctsChangeCustom.Token);
            }
            for (int i = 0; i < customs.Length; i++)
            {
                customs[i].w = newValue;
            }
        }
        catch (OperationCanceledException)
        {
            for (int i = 0; i < customs.Length; i++)
            {
                customs[i].w = newValue;
            }
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.clear;
        Gizmos.DrawCube(bounds.center, bounds.size);
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
            case AtlasTextRenderer.AtlasTextRendererType.Simple:
            {
                boundsHandle.size = textRend.textBoxData.size;

                float centerX = textRend.transform.position.x;

                switch (textRend.alignmentType)
                {
                    case AtlasTextRenderer.AtlasTextAlignmentType.Left:
                    {
                        centerX += boundsHandle.size.x * 0.5f;
                    }
                    break;

                    case AtlasTextRenderer.AtlasTextAlignmentType.Center:
                    {

                    }
                    break;

                    case AtlasTextRenderer.AtlasTextAlignmentType.Right:
                    {

                        centerX -= boundsHandle.size.x * 0.5f;
                    }
                    break;
                }


                boundsHandle.center = new Vector3(centerX, textRend.transform.position.y - boundsHandle.size.y * 0.5f, textRend.transform.position.z);
                boundsHandle.SetColor(Color.green);
                boundsHandle.DrawHandle();

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(textRend, "Resize Textbox");
                    textRend.textBoxData.size = boundsHandle.size;
                    textRend.SetText(textRend.text);
                }
            }
            break;
            case AtlasTextRenderer.AtlasTextRendererType.Scroll:
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