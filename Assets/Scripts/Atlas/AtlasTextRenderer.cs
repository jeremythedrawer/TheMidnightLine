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
    const int MAX_CHARS = 256;
    const int MAX_LINES = 16;
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
        public float[] lineWidthArray;
        public string[] lineTextArray;

        public float maxLineDimension;
        public int lineCount;
        public int charCount;
    }

    public delegate void Callback();

    public AtlasTextRendererType rendererType;

    public BatchKey batchKey;
    public TextAtlas textAtlas;

    [TextArea(3, 10)] public string text;
    [Range(-0.1f, 0.1f)] public float kerning = 0.02f;
    [Range(0, 0.2f)]public float spacing = 0.04f;
    public bool verticalText;
    public AtlasTextAlignmentType alignmentType;

    [Header("Scroll Settings")]
    public float scrollSpeed;
    public float scrollBoundSize;

    [Header("Border Settings")]
    public AtlasRenderer backgroundRenderer;
    public Vector2 margin;

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

    public bool erasingText;
    public bool completedWritingText;
    [Header("Border Generated")]
    public Vector3 borderLocalPos;

    public delegate void OnCompletedWritingText();

    private void OnValidate()
    {
        if (textAtlas == null) return;
        if (batchKey.material == null) return;
        textBoxData.lineWidthArray = new float[MAX_LINES];
        textBoxData.lineTextArray = new string[MAX_LINES];
        SetText(text);

        bounds = GetBoundsNewText(text);
    }
    private void OnEnable()
    {
        batchKey.texture = textAtlas?.atlas?.texture;
        textAtlas?.SetWorldSpaceLineHeight();
        RegisterTextRenderer(this);
        if (backgroundRenderer != null) backgroundRenderer.enabled = true;

        worldPivotsAndSizes = new Vector4[MAX_CHARS];
        uvSizesAndPositions = new Vector4[MAX_CHARS];
        scalesAndFlips = new Vector4[MAX_CHARS];
        customs = new Vector4[MAX_CHARS];

        textBoxData.lineWidthArray = new float[MAX_LINES];
        textBoxData.lineTextArray = new string[MAX_LINES];
                SetText(text);
    }
    private void OnDisable()
    {
        UnregisterTextRenderer(this);
        if (backgroundRenderer != null) backgroundRenderer.enabled = false;
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
        SetTextWorld(inputText, alpha);
        
        switch (rendererType)
        {
            case AtlasTextRendererType.Simple:
            {
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
                SetBorderText();
            }
            break;
        }
    }
    public void SetAppearTextAlpha(float normAmount)
    {
        for (int i = 0; i < customs.Length; i++)
        {
            customs[i].w = normAmount;
        }
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
    private void SetBorderText()
    {
        if (text == "")
        {
            backgroundRenderer.enabled = false;
            return;
        }
        Vector4 centerLeftWPS = backgroundRenderer.worldPivotsAndSizes[3];
        Vector4 centerRightWPS = backgroundRenderer.worldPivotsAndSizes[5];
        float xOffset = (centerRightWPS.z - centerLeftWPS.z) * 0.5f;
        switch (alignmentType)
        {
            case AtlasTextAlignmentType.Left:
            {
                borderLocalPos.x = bounds.extents.x + xOffset;
            }
            break;

            case AtlasTextAlignmentType.Center:
            {
                borderLocalPos.x = xOffset;
            }
            break;

            case AtlasTextAlignmentType.Right:
            {
                borderLocalPos.x = -bounds.extents.x + xOffset;
            }
            break;
        }

        borderLocalPos.y = -bounds.size.y - backgroundRenderer.worldPivotsAndSizes[0].w - margin.y;
        borderLocalPos.z = backgroundRenderer.transform.localPosition.z;

        backgroundRenderer.enabled = true;

        Vector2 worldSize = new Vector2();
        worldSize.x = bounds.size.x + (margin.x * 2);  
        worldSize.y = bounds.size.y + (margin.y * 2);

        backgroundRenderer.transform.localPosition = borderLocalPos;

        backgroundRenderer.SetNineSliceSizeFromWorldSpace(worldSize);

    }
    public void SetTextWorld(string inputText, float alpha)
    {
        if (inputText == null) return;

        text = inputText;

        textBoxData.lineCount = 0;
        textBoxData.charCount = 0;

        string curLineText = "";
        float curLineDimension = -kerning;
        float maxLineDimension = 0f;
        
        string[] words = text.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];

            float wordDimension = 0f;

            for (int j = 0; j < word.Length; j++)
            {
                char c = word[j];

                int letterIndex = c - 33;

                if (letterIndex < 0) continue;

                SimpleSprite sprite = textAtlas.atlas.simpleSprites[letterIndex];

                float spriteDimension = verticalText ? sprite.worldSize.y : sprite.worldSize.x;
                wordDimension += spriteDimension + kerning;
            }
            float spacingDimension = curLineText.Length > 0 ? spacing : 0f;
            float newLineDimension = curLineDimension + spacingDimension + wordDimension;

            float textBoxBorder = verticalText ? textBoxData.size.y : textBoxData.size.x;
            bool pastTextBoxThreshold = newLineDimension > textBoxBorder;
            if (curLineText.Length > 0 && pastTextBoxThreshold)
            {
                textBoxData.lineTextArray[textBoxData.lineCount] = curLineText;
                textBoxData.lineWidthArray[textBoxData.lineCount] = curLineDimension;
                textBoxData.lineCount++;
                curLineText = word;
                curLineDimension = wordDimension - kerning;
            }
            else
            {
                if (curLineText.Length > 0) curLineText += " ";

                curLineText += word;
                curLineDimension = newLineDimension;
            }

            if (curLineDimension > maxLineDimension) maxLineDimension = curLineDimension;

            textBoxData.charCount += word.Length;
        }

        if (curLineText.Length > 0)
        {
            textBoxData.lineTextArray[textBoxData.lineCount] = curLineText;
            textBoxData.lineWidthArray[textBoxData.lineCount] = curLineDimension;
            textBoxData.lineCount++;
        }

        textBoxData.maxLineDimension = maxLineDimension;

        float linePos = -textAtlas.typeWorldHeight;
        float charPos = 0;
        int spriteIndex = 0;


        for (int i = 0; i < textBoxData.lineCount; i++)
        {
            string line = textBoxData.lineTextArray[i];
            float lineWidth = textBoxData.lineWidthArray[i];


            if (alignmentType == AtlasTextAlignmentType.Center)
            {
                charPos = -(lineWidth * 0.5f);
            }
            else if (alignmentType == AtlasTextAlignmentType.Right)
            {
                charPos = -lineWidth;
            }

            for (int  j = 0; j < line.Length; j++)
            {
                char c = line[j];

                if (c == ' ')
                {
                    if (verticalText)
                    {
                        linePos -= spacing;
                    }
                    else
                    {
                        charPos += spacing;
                    }
                    continue;
                }

                int letterIndex = c - 33;

                if (letterIndex < 0) continue;

                SimpleSprite sprite = textAtlas.atlas.simpleSprites[letterIndex];

                Vector4 worldPivSize = new Vector4();

                worldPivSize.x = charPos;
                worldPivSize.y = linePos;
                worldPivSize.z = sprite.worldSize.x;
                worldPivSize.w = sprite.worldSize.y;
                worldPivotsAndSizes[spriteIndex] = worldPivSize;

                Vector4 custom = new Vector4();
                custom.x = 0;
                custom.y = 0;
                custom.z = 0;
                custom.w = alpha;
                customs[spriteIndex] = custom;

                uvSizesAndPositions[spriteIndex] = sprite.uvSizeAndPos;
                scalesAndFlips[spriteIndex] = Vector4.one;

                spriteIndex++;

                if (verticalText)
                {
                    linePos -= sprite.worldSize.y + kerning;
                }
                else
                {
                    charPos += sprite.worldSize.x + kerning;
                }
            }

            if (verticalText)
            {
                charPos += textAtlas.typeWorldHeight;
                linePos = -textAtlas.typeWorldHeight;
            }
            else
            {
                linePos -= textAtlas.typeWorldHeight;
                charPos = 0;
            }
        }

        if (verticalText)
        {
            float typeWorldWidth = textAtlas.typeWorldHeight / 2;
            textBoxData.size.x = Mathf.Max(charPos - typeWorldWidth, typeWorldWidth);
        }
        else
        {
            textBoxData.size.y = Mathf.Max(Mathf.Abs(linePos + textAtlas.typeWorldHeight), textAtlas.typeWorldHeight);
        }
        Vector2 boundsSize = new Vector2();
        boundsSize.x = verticalText ? textBoxData.size.x : maxLineDimension;
        boundsSize.y = verticalText ? maxLineDimension : textBoxData.size.y;
        bounds.size = boundsSize;
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
    public void ChangeCustom(float time, float newValue, int customChannel, Callback callback = null)
    {
        ctsChangeCustom?.Cancel();
        ctsChangeCustom = new CancellationTokenSource();

        switch(customChannel)
        {
            case 1:
            {
                ChangingCustomX(time, newValue, callback).Forget();

            }
            break;
            case 2:
            {
                ChangingCustomY(time, newValue, callback).Forget();

            }
            break;
            case 3:
            {
                ChangingCustomZ(time, newValue, callback).Forget();

            }
            break;
            case 4:
            {
                ChangingCustomW(time, newValue, callback).Forget();

            }
            break;

        }
    }
    public Bounds GetBoundsNewText(string text)
    {
        Bounds bounds = new Bounds();
        Vector2 boundsSize = new Vector2();
        boundsSize.x = verticalText ? textBoxData.size.x : textBoxData.maxLineDimension;
        boundsSize.y = verticalText ? textBoxData.maxLineDimension : textBoxData.size.y;

        bounds.size = boundsSize;
        
        switch (alignmentType)
        {
            case AtlasTextAlignmentType.Left:
            {
                boundsOffset.x = bounds.extents.x;
                boundsOffset.y = -bounds.extents.y;
            }
            break;

            case AtlasTextAlignmentType.Center:
            {
                boundsOffset.x = 0;
                boundsOffset.y = -textBoxData.size.y * 0.5f;
            }
            break;

            case AtlasTextAlignmentType.Right:
            {
                boundsOffset.x = -bounds.extents.x;
                boundsOffset.y = -textBoxData.size.y * 0.5f;
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

        string curText = "";
        SetText(curText);

        try
        {
            while (curLetterIndex < stationNameLetterCount)
            {
                curText += text[curLetterIndex];
                await UniTask.WaitForSeconds(writeLetterTime, cancellationToken: ctsWrite.Token);
                SetText(curText);
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
    private async UniTask ChangingCustomX(float time, float newValue, Callback callback = null)
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
            if (callback != null) callback();
        }
        catch (OperationCanceledException)
        {
            for (int i = 0; i < customs.Length; i++)
            {
                customs[i].x = newValue;
            }
            if (callback != null) callback();
        }
    }
    private async UniTask ChangingCustomY(float time, float newValue, Callback callback = null)
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
            if (callback != null) callback();

        }
        catch (OperationCanceledException)
        {
            for (int i = 0; i < customs.Length; i++)
            {
                customs[i].y = newValue;
            }
            if (callback != null) callback();
        }
    }
    private async UniTask ChangingCustomZ(float time, float newValue, Callback callback = null)
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
            if (callback != null) callback();
        }
        catch (OperationCanceledException)
        {
            for (int i = 0; i < customs.Length; i++)
            {
                customs[i].z = newValue;
            }
            if (callback != null) callback();
        }
    }
    private async UniTask ChangingCustomW(float time, float newValue, Callback callback = null)
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
            if (callback != null) callback();
        }
        catch (OperationCanceledException)
        {
            for (int i = 0; i < customs.Length; i++)
            {
                customs[i].w = newValue;
            }
            if (callback != null) callback();
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.clear;
        Gizmos.DrawCube(bounds.center, bounds.size);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.purple;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(AtlasTextRenderer))]
public class AtlasTextRendererEditor : Editor
{
    BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

    Vector4 custom;
    private void OnSceneGUI()
    {
        AtlasTextRenderer textRend = (AtlasTextRenderer)target;

        EditorGUI.BeginChangeCheck();
        
        switch(textRend.rendererType)
        {
            case AtlasTextRenderer.AtlasTextRendererType.Simple:
            case AtlasTextRenderer.AtlasTextRendererType.Border:
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


    ColorBits colorBits;
    public override void OnInspectorGUI()
    {
        AtlasTextRenderer textRenderer = (AtlasTextRenderer)target;
        base.OnInspectorGUI();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Editor");
        custom.x = EditorGUILayout.Slider(custom.x, 0, 1);
        custom.y = EditorGUILayout.Slider(custom.y, 0, 1);
        custom.z = EditorGUILayout.Slider(custom.z, 0, 1);
        custom.w = EditorGUILayout.Slider(custom.w, 0, 1);

        if (EditorGUI.EndChangeCheck())
        {
            for (int i = 0; i < textRenderer.customs.Length; i++)
            {
                textRenderer.customs[i] = custom;
            }
        }

        EditorGUI.BeginChangeCheck();
        GUIContent coloBitsContent = new GUIContent("Color Bits");
        EditorGUILayout.LabelField("Editor");
        colorBits = (ColorBits)EditorGUILayout.EnumFlagsField(coloBitsContent, (ColorBits)textRenderer.customBit);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(textRenderer, "Change Color Bits");
            textRenderer.customBit = (int)colorBits;
            EditorUtility.SetDirty(textRenderer);
        }
    }
}
#endif