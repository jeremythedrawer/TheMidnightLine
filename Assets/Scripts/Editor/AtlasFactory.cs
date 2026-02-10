using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static Atlas;

public class AtlasFactory : EditorWindow
{
    private AtlasBaseSO atlas;
    private MaterialIDSO materialIDs;
    private float spriteOrderTolerance;
    private Vector2 scroll;

    private float cellSize = 256f;
    private float markerSize = 4f;

    private float previewScale;
    private Vector2 previewPosition;
    private int selectedIndex;
    private Rect previewRect;
    private RenderTexture previewRT;
    private AtlasSprite previewSprite;
    private int flip;

    //Motion
    private ClipType selectedClipType;
    private int[] motionSelections;
    private int[] holdTimeValues;
    private float editorTimeDelta;
    private double lastEditorTime;
    private int curFrameIndex;
    private int prevFrameIndex;
    private bool startMotion;

    //Tile
    private int previewTileLength = 1;

    const float padding = 50f;
    const float GUIHeaderColoumnWidth = 300;


    [MenuItem("Tools/Atlas Factory")]
    private static void Open()
    {
        GetWindow<AtlasFactory>("Atlas Factory");
    }
    private void Update()
    {
        switch (atlas)
        {
            case AtlasMotionSO motion:
            {
                UpdateMotionPreview(motion);
            }
            break;
        }

    }
    private void OnGUI()
    {
        float halfWindowWidth = position.width * 0.5f;

        GUILayoutOption GUIWidth = GUILayout.Width(GUIHeaderColoumnWidth);
        GUILayoutOption GUIHalfWindowWidth = GUILayout.Width(halfWindowWidth);

        EditorGUILayout.BeginHorizontal();

        #region Input Fields
        EditorGUILayout.BeginVertical(GUIWidth);
        EditorGUILayout.LabelField("Inputs", EditorStyles.boldLabel);
        atlas = (AtlasBaseSO)EditorGUILayout.ObjectField("Atlas", atlas, typeof(AtlasBaseSO), allowSceneObjects: false, GUIWidth);
        materialIDs = (MaterialIDSO)EditorGUILayout.ObjectField("Material SO", materialIDs, typeof(MaterialIDSO), allowSceneObjects: false, GUIWidth);
        spriteOrderTolerance = EditorGUILayout.Slider("Sprite Order Tolerance", spriteOrderTolerance, 0f, 0.1f, GUIWidth);

        bool generateSprites = GUILayout.Button("Generate Sprites", GUIWidth);

        if (generateSprites)
        {
            MakeTextureReadable(atlas.texture);
            atlas.sprites = GenerateSprites();
            previewRect = new Rect(halfWindowWidth + 50, 200, halfWindowWidth * 0.75f, halfWindowWidth * 0.75f);
        }

        switch (atlas)
        {
            case AtlasMotionSO motion:
            {
                if (generateSprites)
                {
                    motion.UpdateClipDictionary();
                    GetMotionData(motion);
                }
            }
            break;
        }
        if (generateSprites)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        EditorGUILayout.EndVertical();
        #endregion

        #region View Fields
        EditorGUILayout.BeginVertical(GUILayout.Width(halfWindowWidth - GUIHeaderColoumnWidth));
        EditorGUILayout.LabelField("View", EditorStyles.boldLabel);
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize, GUIWidth);
        markerSize = EditorGUILayout.FloatField("Marker Size", markerSize, GUIWidth);
        EditorGUILayout.EndVertical();
        #endregion

        #region Right Column Fields
        EditorGUILayout.BeginVertical(GUIWidth);

        switch (atlas)
        {
            case AtlasMotionSO motion:
            {
                EditorGUILayout.LabelField("Motion", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();

                switch (motion.entityType)
                {
                    case EntityType.NPC:
                    {
                        EditorGUILayout.LabelField($"Preview Motion: {(NPCMotion)selectedIndex} ", GUIWidth);
                    }
                    break;
                    case EntityType.Spy:
                    {
                        EditorGUILayout.LabelField($"Preview Motion: {(SpyMotion)selectedIndex} ", GUIWidth);
                    }
                    break;
                }

                if (motion.clipDict.TryGetValue(selectedIndex, out AtlasClip validclip))
                {
                    selectedClipType = validclip.clipType;
                }

                selectedClipType = (ClipType)EditorGUILayout.EnumPopup("Clip Type", selectedClipType, GUIWidth);

                if (EditorGUI.EndChangeCheck())
                {
                    editorTimeDelta = 0;
                    prevFrameIndex = 0;
                    curFrameIndex = 0;

                    for (int i = 0; i < motion.clips.Length; i++)
                    {
                        if (motion.clips[i].motionIndex == selectedIndex)
                        {
                            motion.clips[i].clipType = selectedClipType;
                            break;
                        }
                    }
                    motion.UpdateClipDictionary();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUIHalfWindowWidth);
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

                if (GUILayout.Button("Play", GUIWidth))
                {
                    startMotion = true;
                    editorTimeDelta = 0;
                    curFrameIndex = 0;
                    if (motion.clipDict.TryGetValue(selectedIndex, out AtlasClip clip))
                    {
                        int startSpriteIndex = clip.keyFrames[curFrameIndex].spriteIndex;
                        AtlasSprite sprite = motion.sprites[startSpriteIndex];
                        Vector4 curUVSizeAndPos = new Vector4(sprite.uvSize.x, sprite.uvSize.y, sprite.uvPos.x, sprite.uvPos.y);
                        motion.material.SetInt(materialIDs.ids.uvSizeAndPos, motion.clipDict[selectedIndex].keyFrames[curFrameIndex].spriteIndex);
                    }
                }
                if (GUILayout.Button("Stop", GUIWidth))
                {
                    startMotion = false;
                }
                if (GUILayout.Button("Flip", GUIWidth))
                {
                    flip = flip == 1 ? -1 : 1;
                }
                previewScale = EditorGUILayout.FloatField("Preview Scale", previewScale, GUIWidth);
                previewPosition = EditorGUILayout.Vector2Field("Preview Position", previewPosition, GUIWidth);

                if (motion.clips == null)
                {
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                    return;
                }
            }
            break;

            case AtlasTileSO tile:
            {
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                previewTileLength = EditorGUILayout.IntField("Preview Length", previewTileLength, GUIWidth);
                previewScale = EditorGUILayout.FloatField("Preview Scale", previewScale, GUIWidth);
                previewPosition = EditorGUILayout.Vector2Field("Preview Position", previewPosition, GUIWidth);
            }
            break;
        }
        EditorGUILayout.EndVertical();
        #endregion

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        #region Sprite Editor
        EditorGUILayout.BeginVertical(GUIHalfWindowWidth);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        int columns = Mathf.RoundToInt((halfWindowWidth - padding) / (cellSize + padding));
        columns = Mathf.Max(1, columns);
        for (int i = 0; i < atlas.sprites.Length; i++)
        {
            if (i % columns == 0) EditorGUILayout.BeginHorizontal();

            EditorDrawAtlasSprite(atlas.sprites[i], i);

            if (i % columns == columns - 1 || i == atlas.sprites.Length - 1) EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        #endregion

        #region Preview Window

        EditorGUILayout.BeginVertical(GUIHalfWindowWidth);

        switch (atlas)
        {
            case AtlasSimpleSO simple:
            {
                EditorGUILayout.LabelField("Preview Sprite", EditorStyles.boldLabel);
                DrawPreview();
            }
            break;
            case AtlasMotionSO motion:
            {
                EditorGUILayout.LabelField("Preview Motion", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Preview Frame Index: {curFrameIndex.ToString()}", GUIWidth);
                DrawPreview();
            }
            break;

            case AtlasTileSO tile:
            {
                EditorGUILayout.LabelField("Preview Tile", EditorStyles.boldLabel);
                DrawPreview();
            }
            break;
        }
        EditorGUILayout.EndVertical();
        #endregion

        EditorGUILayout.EndHorizontal();

        GUI.enabled = atlas != null;
    }
    private AtlasSprite[] GenerateSprites()
    {
        Color32[] pixels = atlas.texture.GetPixels32();
        bool[] visited = new bool[pixels.Length];

        int spriteIndex = 0;

        List<AtlasSprite> atlasSpritesList = new List<AtlasSprite>();

        for (int y = atlas.texture.height - 1; y >= 0; y--)
        {
            for (int x = 0; x < atlas.texture.width; x++)
            {
                int index = x + y * atlas.texture.width;

                if (visited[index]) continue;

                if (pixels[index].a == 0) continue;

                List<Vector2Int> pixelPositions = FloodFill(x, y, atlas.texture.width, atlas.texture.height, visited, pixels);

                if (pixelPositions.Count < 10) continue;
                
                AtlasSprite newAtlasSprite = CreateAtlasSprite(pixelPositions, atlas.texture.width, atlas.texture.height, spriteIndex, pixels);
                atlasSpritesList.Add(newAtlasSprite);
                spriteIndex++;
            }
        }

        AtlasSprite[] atlasSpriteArray = atlasSpritesList.ToArray();

        Array.Sort(atlasSpriteArray, (a, b) =>
        {
            if (Mathf.Abs(b.uvPos.y - a.uvPos.y) > spriteOrderTolerance) return b.uvPos.y.CompareTo(a.uvPos.y);

            return a.uvPos.x.CompareTo(b.uvPos.x);
        });

        for (int i = 0; i < atlas.sprites.Length; i++)
        {
            atlas.sprites[i].index = i;
        }

        return atlasSpriteArray;
    }
    private List<Vector2Int> FloodFill(int startX, int startY, int width, int height, bool[] visited, Color32[] pixels)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(new Vector2Int(startX, startY)); // Starting at the bottom left of the new sprite

        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height) continue; // skip if the current pixel isnt within the texture bounds
            int index = p.x + p.y * width; // Convert to 1D array
            if (visited[index]) continue; // skip if the pixel is already apart of another sprite
            visited[index] = true;
            if (pixels[index].a == 0) continue; // skip if the alpha is 0

            result.Add(p);

            Vector2Int topRightNeighbour = new Vector2Int(p.x + 1, p.y + 1);
            Vector2Int rightCenterNeighbour = new Vector2Int(p.x + 1, p.y);
            Vector2Int bottomRightNeighbour = new Vector2Int(p.x + 1, p.y - 1);
            Vector2Int bottomCenterNeightbour = new Vector2Int(p.x, p.y - 1);
            Vector2Int bottomLeftNeightbour = new Vector2Int(p.x - 1, p.y - 1);
            Vector2Int leftCenterNeighbour = new Vector2Int(p.x - 1, p.y);
            Vector2Int topLeftNeighbour = new Vector2Int(p.x - 1, p.y + 1);
            Vector2Int topCenterNeightbour = new Vector2Int(p.x, p.y + 1);

            queue.Enqueue(topRightNeighbour);
            queue.Enqueue(rightCenterNeighbour);
            queue.Enqueue(bottomRightNeighbour);
            queue.Enqueue(bottomCenterNeightbour);
            queue.Enqueue(bottomLeftNeightbour);
            queue.Enqueue(leftCenterNeighbour);
            queue.Enqueue(topLeftNeighbour);
            queue.Enqueue(topCenterNeightbour);
        }
        return result;
    }
    private AtlasSprite CreateAtlasSprite(List<Vector2Int> pixelPositions, float texWidth, float texHeight, int index, Color32[] pixelColors)
    {
        AtlasSprite newAtlasSprite = new AtlasSprite();

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < pixelPositions.Count; i++)
        {
            Vector2 p = pixelPositions[i];

            minX = Mathf.Min(minX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxX = Mathf.Max(maxX, p.x);
            maxY = Mathf.Max(maxY, p.y);
        }
        
        List<SpriteMarker> spriteMarkers = new List<SpriteMarker>();
        Vector2 pivot = Vector2.zero;
        bool foundPivot = false;
        for (int x = (int)minX; x <= maxX; x++)
        {
            for (int y = (int)minY; y <= maxY; y++)
            {
                int pixelIndex = x + y * (int)texWidth;
                Color32 pixelColor = pixelColors[pixelIndex];

                if (pixelColor.a == 0) continue; 

                if (!foundPivot && pixelColor.r == atlas.pivotColor.r && pixelColor.g == atlas.pivotColor.g && pixelColor.b == atlas.pivotColor.b)
                {
                    pivot.x = x;
                    pivot.y = y;
                    foundPivot = true;
                }

                for (int j = 0; j < atlas.markers.Length; j++)
                {
                    AtlasMarker atlasMarker = atlas.markers[j];

                    if (atlasMarker.color.r != pixelColor.r || atlasMarker.color.g != pixelColor.g || atlasMarker.color.b != pixelColor.b) continue;
                    SpriteMarker newSpriteMarker = new SpriteMarker();
                    newSpriteMarker.type = atlasMarker.type;
                    newSpriteMarker.objectPos.x = (x - minX) / PIXELS_PER_UNIT;
                    newSpriteMarker.objectPos.y = (y - minY) / PIXELS_PER_UNIT;
                    spriteMarkers.Add(newSpriteMarker);
                }
            }
        }

        float spriteWidth = maxX - minX + 1;
        float spriteHeight = maxY - minY + 1;

        newAtlasSprite.uvPos.x = minX / texWidth;
        newAtlasSprite.uvPos.y = minY / texHeight;

        newAtlasSprite.uvSize.x = spriteWidth / texWidth;
        newAtlasSprite.uvSize.y = spriteHeight / texHeight;

        if (foundPivot)
        {
            newAtlasSprite.uvPivot.x = (pivot.x - minX) / spriteWidth;
            newAtlasSprite.uvPivot.y = (pivot.y - minY) / spriteHeight;
        }
        else
        {
            newAtlasSprite.uvPivot.x = 0;
            newAtlasSprite.uvPivot.y = 0;
        }

        newAtlasSprite.markers = spriteMarkers.ToArray();
        return newAtlasSprite;
    }
    private void MakeTextureReadable(Texture texture)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(path);
        if (textureImporter.isReadable) return;

        textureImporter.isReadable = true;
        textureImporter.SaveAndReimport();

    }
    private void GetMotionData(AtlasMotionSO motion)
    {
        motionSelections = new int[atlas.sprites.Length];
        holdTimeValues = new int[atlas.sprites.Length];
        for (int i = 0; i < atlas.sprites.Length; i++)
        {
            for (int j = 0; j < motion.clips.Length; j++)
            {
                AtlasClip clip = motion.clips[j];

                for (int k = 0; k < clip.keyFrames.Length; k++)
                {
                    AtlasKeyframe keyframe = clip.keyFrames[k];

                    if (keyframe.spriteIndex == i)
                    {
                        motionSelections[i] = clip.motionIndex;
                        holdTimeValues[i] = keyframe.holdTime;
                        break;
                    }
                }
            }
        }
    }
    private void EditorDrawAtlasSprite(AtlasSprite atlasSprite, int index)
    {
        Rect gridRect = GUILayoutUtility.GetRect(cellSize + padding, cellSize + padding, GUILayout.ExpandWidth(false));
        gridRect = new Rect(gridRect.x + padding * 0.5f, gridRect.y + padding * 0.5f, cellSize, cellSize);

        Rect uvRect = new Rect(atlasSprite.uvPos, atlasSprite.uvSize);
        float spritePixelWidth = atlasSprite.uvSize.x * atlas.texture.width;
        float spritePixelHeight = atlasSprite.uvSize.y * atlas.texture.height;

        float aspectRatio = Mathf.Min(gridRect.width / spritePixelWidth, gridRect.height / spritePixelHeight);
        Vector2 drawSize = new Vector2(spritePixelWidth * aspectRatio, spritePixelHeight * aspectRatio);
        Rect spriteRect = new Rect(gridRect.center.x - drawSize.x * 0.5f, gridRect.center.y - drawSize.y * 0.5f, drawSize.x, drawSize.y);

        GUI.DrawTextureWithTexCoords(spriteRect, atlas.texture, uvRect, alphaBlend: true);

        Vector2 pivotPos = new Vector2(Mathf.Lerp(spriteRect.xMin, spriteRect.xMax, atlasSprite.uvPivot.x),Mathf.Lerp(spriteRect.yMax, spriteRect.yMin, atlasSprite.uvPivot.y));
        Rect pivotRect = new Rect(pivotPos - Vector2.one * markerSize, Vector2.one * (markerSize * 2));

        Handles.BeginGUI();
        Handles.DrawSolidRectangleWithOutline(spriteRect, Color.clear, Color.grey);
        Handles.DrawSolidRectangleWithOutline(pivotRect, Color.clear, atlas.pivotColor);

        if (atlasSprite.markers.Length != 0)
        {
            for (int i = 0; i < atlasSprite.markers.Length; i++)
            {
                SpriteMarker marker = atlasSprite.markers[i];

                for (int j = 0; j < atlas.markers.Length; j++)
                {
                    AtlasMarker atlasMarker = atlas.markers[j];
                    if ((marker.type & atlasMarker.type) != 0)
                    {
                        Vector2 markerPixelInSprite = marker.objectPos * PIXELS_PER_UNIT;
                        Vector2 markerNormalized = new Vector2(markerPixelInSprite.x / spritePixelWidth, markerPixelInSprite.y / spritePixelHeight);
                        Vector2 markerPixel = new Vector2(Mathf.Lerp(spriteRect.xMin, spriteRect.xMax, markerNormalized.x), Mathf.Lerp(spriteRect.yMax, spriteRect.yMin, markerNormalized.y));
                        Rect markerRect = new Rect(markerPixel - Vector2.one * markerSize, Vector2.one * (markerSize * 2));
                        Handles.DrawSolidRectangleWithOutline(markerRect, Color.clear, atlasMarker.color);
                        break;
                    }
                }
            }
        }
        Handles.EndGUI();

        //Sprite Index Label
        Vector2 indexPos = new Vector2(spriteRect.xMin, spriteRect.yMin);
        GUIStyle indexStyle = new GUIStyle(EditorStyles.boldLabel) 
        { 
            alignment = TextAnchor.UpperLeft, normal = { textColor = Color.white } 
        };
        GUI.Label(new Rect(indexPos, new Vector2(20, 20)), index.ToString(), indexStyle);

        switch (atlas)
        {
            case AtlasMotionSO motion:
            {
                float clipDataRectWidth = cellSize * 0.5f;
                float clipDataRectHeight = 16;
                float clipDataRectYPos = spriteRect.yMax + 2;
                float minRectX = spriteRect.center.x - clipDataRectWidth;

                Rect animTypeRect = new Rect(minRectX, clipDataRectYPos, clipDataRectWidth, clipDataRectHeight);
                Rect holdTimeRect = new Rect(spriteRect.center.x, clipDataRectYPos, clipDataRectWidth, clipDataRectHeight);

                EditorGUI.BeginChangeCheck();

                int prevMotion = motionSelections[index];

                switch (motion.entityType)
                {
                    case EntityType.NPC:
                    {
                        NPCMotion nextNPCMotion = (NPCMotion)EditorGUI.EnumPopup(animTypeRect, (NPCMotion)prevMotion);
                        motionSelections[index] = (int)nextNPCMotion;
                    }
                    break;
                    case EntityType.Spy:
                    {
                        SpyMotion nextSpyMotion = (SpyMotion)EditorGUI.EnumPopup(animTypeRect, (SpyMotion)prevMotion);
                        motionSelections[index] = (int)nextSpyMotion;
                    }
                    break;
                }

                int prevHoldTimeValue = holdTimeValues[index];
                int selectedHoldTimeValue = EditorGUI.IntField(holdTimeRect, prevHoldTimeValue);
                holdTimeValues[index] = selectedHoldTimeValue;

                if (EditorGUI.EndChangeCheck())
                {
                    if (prevMotion == motionSelections[index]) return;
                    selectedIndex = motionSelections[index];
                    Handles.BeginGUI();
                    Handles.DrawSolidRectangleWithOutline(spriteRect, Color.clear, Color.limeGreen);
                    Handles.EndGUI();

                    curFrameIndex = 0;

                    bool foundKeyframe = false;
                    int selectedClipIndex = -1;
                    int prevClipIndex = -1;
                    for (int i = 0; i < motion.clips.Length; i++)
                    {
                        AtlasClip clip = motion.clips[i];

                        if (clip.motionIndex == motionSelections[index])
                        {
                            for (int j = 0; j < clip.keyFrames.Length; j++)
                            {
                                AtlasKeyframe keyframe = clip.keyFrames[j];
                                if (keyframe.spriteIndex == index)
                                {
                                    foundKeyframe = true;
                                    if (selectedHoldTimeValue > -1)
                                    {
                                        motion.clips[i].keyFrames[j].holdTime = selectedHoldTimeValue;
                                    }
                                    break;
                                }
                            }
                            selectedClipIndex = i;
                        }
                        else if (clip.motionIndex == prevMotion)
                        {
                            prevClipIndex = i;
                        }
                        if (selectedClipIndex != -1) break;
                    }

                    if (selectedClipIndex == -1) // Creating new clip
                    {
                        if (motionSelections[index] > 0 && selectedHoldTimeValue > -1)
                        {
                            List<AtlasClip> clipsList = motion.clips.ToList();

                            AtlasClip newClip = new AtlasClip();

                            newClip.motionIndex = motionSelections[index];

                            switch (motion.entityType)
                            {
                                case EntityType.NPC:
                                {
                                    newClip.clipName = ((NPCMotion)motionSelections[index]).ToString();
                                }
                                break;

                                case EntityType.Spy:
                                {
                                    newClip.clipName = ((SpyMotion)motionSelections[index]).ToString();

                                }
                                break;
                            }

                            newClip.keyFrames = new AtlasKeyframe[1];
                            newClip.keyFrames[0].spriteIndex = index;
                            newClip.keyFrames[0].holdTime = selectedHoldTimeValue;
                            clipsList.Add(newClip);
                            motion.clips = clipsList.ToArray();
                        }
                    }
                    else if (!foundKeyframe && selectedHoldTimeValue > -1) // Creating new frame
                    {
                        List<AtlasKeyframe> selectedClipKeyframesList = motion.clips[selectedClipIndex].keyFrames.ToList();
                        AtlasKeyframe newKeyframe = new AtlasKeyframe();
                        newKeyframe.spriteIndex = index;
                        newKeyframe.holdTime = selectedHoldTimeValue;
                        selectedClipKeyframesList.Add(newKeyframe);
                        motion.clips[selectedClipIndex].keyFrames = selectedClipKeyframesList.ToArray();
                    }

                    if (prevClipIndex != -1)
                    {
                        AtlasClip curClip = motion.clips[prevClipIndex];
                        List<AtlasKeyframe> keyframesList = curClip.keyFrames.ToList();
                        for (int i = 0; i < keyframesList.Count; i++)
                        {
                            if (keyframesList[i].spriteIndex == index)
                            {
                                keyframesList.RemoveAt(i);
                                break;
                            }
                        }

                        if (keyframesList.Count == 0) // remove empty clips
                        {
                            List<AtlasClip> clipList = motion.clips.ToList();
                            clipList.RemoveAt(prevClipIndex);
                            motion.clips = clipList.ToArray();

                        }
                        else
                        {
                            motion.clips[prevClipIndex].keyFrames = keyframesList.ToArray();
                        }
                    }

                    editorTimeDelta = 0;
                    prevFrameIndex = 0;
                    curFrameIndex = 0;
                    motion.UpdateClipDictionary();
                    if (selectedIndex != 0)
                    {
                        motion.material.SetInt(materialIDs.ids.atlasIndex, motion.clipDict[selectedIndex].keyFrames[curFrameIndex].spriteIndex);
                    }
                    EditorUtility.SetDirty(atlas);
                    AssetDatabase.SaveAssets();
                }
            }
            break;
        }
    }
    private void DrawPreview()
    {
        Handles.BeginGUI();
        Handles.DrawSolidRectangleWithOutline(previewRect, Color.clear, Color.grey);
        Handles.EndGUI();
        if (previewRT == null)
        {
            previewRT = new RenderTexture((int)previewRect.width, (int)previewRect.height, 16, RenderTextureFormat.ARGB32);
            previewRT.Create();
        }
        RenderTexture.active = previewRT;
        GL.Clear(true, true, Color.clear);
        float spritePixelWidth = previewSprite.uvSize.x * atlas.texture.width;
        float spritePixelHeight = previewSprite.uvSize.y * atlas.texture.height;

        float scaledWidth = spritePixelWidth * previewScale * 2 * flip;
        float scaledHeight = spritePixelHeight * previewScale;

        float pivotX = previewSprite.uvPivot.x * scaledWidth;

        float offsetX = previewRT.width - pivotX;
        float offsetY = previewRT.height - scaledHeight;

        Rect destRect = new Rect(offsetX + previewPosition.x, offsetY + previewPosition.y, scaledWidth, scaledHeight);
        Rect uvRect = new Rect(previewSprite.uvPos.x, previewSprite.uvPos.y, previewSprite.uvSize.x, previewSprite.uvSize.y);

        Graphics.DrawTexture(destRect, atlas.texture, uvRect, 0, 0, 0, 0);

        RenderTexture.active = null;

        GUI.DrawTexture(previewRect, previewRT, ScaleMode.ScaleToFit, true);
    }

    private void UpdateMotionPreview(AtlasMotionSO motion)
    {
        if (atlas == null || motion.sprites == null || motion.clipDict == null) return;
        double now = EditorApplication.timeSinceStartup;
        float delta = (float)(now - lastEditorTime);
        lastEditorTime = now;
        editorTimeDelta += delta;

        if (startMotion)
        {
            if (!motion.clipDict.TryGetValue(selectedIndex, out AtlasClip clip)) return;

            SetNextFrameIndex(clip, motion.framesPerSecond, ref editorTimeDelta, ref curFrameIndex, ref prevFrameIndex);

            if (curFrameIndex != prevFrameIndex)
            {
                SetPreviewSprite(clip.keyFrames[curFrameIndex].spriteIndex);
                Repaint();
            }
        }
    }

    public void SetPreviewSprite(int spriteIndex)
    {
        previewSprite = atlas.sprites[spriteIndex];

        float spritePixelWidth = previewSprite.uvSize.x * atlas.texture.width;
        float spritePixelHeight = previewSprite.uvSize.y * atlas.texture.height;

        Vector2 offset = previewSprite.uvPos + (previewSprite.uvSize * 0.5f);
    }
}
