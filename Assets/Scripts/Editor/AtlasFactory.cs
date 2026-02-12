using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Atlas;

public class AtlasFactory : EditorWindow
{
    private AtlasSO atlas;
    private float spriteOrderTolerance;
    
    private Vector2 scroll;
    private bool changeSprites;

    private float cellSize = 256f;
    private float markerSize = 4f;

    private int selectedIndex;

    //Preview
    private float previewScale =  0.2f;
    private int selectedMotionIndex;

    private Rect previewRect;
    private RenderTexture previewRT;
    private SimpleSprite previewSprite;
    private int flip;
    private Color32 previewBGColor;
    List<SimpleSprite> simpleSpritesList;
    List<MotionSprite> motionSpritesList;
    List<SliceSprite> slicedSpritesList;

    //Simple
    private Vector2[] simplePivots = new Vector2[]
    {
        new Vector2(0.0f, 1.0f), new Vector2(0.5f, 1.0f), new Vector2(1.0f, 1.0f),
        new Vector2(0.0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1.0f, 0.5f),
        new Vector2(0.0f, 0.0f), new Vector2(0.5f, 0.0f), new Vector2(1.0f, 0.0f),
    };


    //Motion
    private AtlasClip previewClip;
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
    private float halfWindowWidth;

    [MenuItem("Tools/Atlas Factory")]
    private static void Open()
    {
        GetWindow<AtlasFactory>("Atlas Factory");
    }
    private void Update()
    {
        UpdateMotionPreview();
    }
    private void OnGUI()
    {
        halfWindowWidth = position.width * 0.5f;

        GUILayoutOption[] GUIWidth = { GUILayout.Width(GUIHeaderColoumnWidth) };
        GUILayoutOption GUIHalfWindowWidth = GUILayout.Width(halfWindowWidth);

        EditorGUILayout.BeginHorizontal();

        #region Input Fields
        EditorGUILayout.BeginVertical(GUIWidth);
        EditorGUILayout.LabelField("Inputs", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        atlas = (AtlasSO)EditorGUILayout.ObjectField("Atlas", atlas, typeof(AtlasSO), allowSceneObjects: false, GUIWidth);
        if (EditorGUI.EndChangeCheck())
        {
            changeSprites = false;
        }
        
        spriteOrderTolerance = EditorGUILayout.Slider("Sprite Order Tolerance", spriteOrderTolerance, 0.01f, 0.2f, GUIWidth);

        if (atlas == null)
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            return;
        }

        bool generateSprites = GUILayout.Button("Generate Sprites", GUIWidth);

        if (generateSprites)
        {
            previewClip = default;
            selectedIndex = 0;
            changeSprites = true;
            MakeTextureReadable(atlas.texture);
            GenerateSprites();
            atlas.UpdateClipDictionary();
            GetMotionData();
            previewRT = null;
            flip = 1;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        EditorGUILayout.EndVertical();
        if (!changeSprites)
        {
            EditorGUILayout.EndHorizontal();
            return;
        }
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
        EditorGUILayout.LabelField("Motion", EditorStyles.boldLabel);
        switch (atlas.entityMotionType)
        {
            case EntityMotionType.NPC:
            {
                EditorGUILayout.LabelField($"Preview Motion: {(NPCMotion)selectedMotionIndex} ", GUIWidth);
            }
            break;
            case EntityMotionType.Spy:
            {
                EditorGUILayout.LabelField($"Preview Motion: {(SpyMotion)selectedMotionIndex} ", GUIWidth);
            }
            break;
        }
        if (atlas.clipDict != null)
        {
            if (atlas.clipDict.TryGetValue(selectedMotionIndex, out AtlasClip clip))
            {
                previewClip = clip;
                EditorGUI.BeginChangeCheck();

                previewClip.clipType = (ClipType)EditorGUILayout.EnumPopup("Clip Type", previewClip.clipType, GUIWidth);

                if (EditorGUI.EndChangeCheck())
                {
                    editorTimeDelta = 0;
                    prevFrameIndex = 0;
                    curFrameIndex = 0;

                    for (int i = 0; i < atlas.clips.Length; i++)
                    {
                        if (atlas.clips[i].motionIndex == selectedMotionIndex)
                        {
                            atlas.clips[i].clipType = previewClip.clipType;
                            break;
                        }
                    }
                   atlas.UpdateClipDictionary();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No motion selected", EditorStyles.boldLabel);
            }
        }
        else
        {
            previewClip = default;
        }

            EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical(GUIHalfWindowWidth);
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        if (GUILayout.Button("Play", GUIWidth))
        {
            startMotion = true;
            editorTimeDelta = 0;
            curFrameIndex = 0;
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
        previewBGColor = EditorGUILayout.ColorField("Background Color", previewBGColor, GUIWidth);
        if (atlas.clips == null)
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            return;
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


        int gridIndex = 0;
        for (int i = 0; i < atlas.simpleSprites.Length; i++)
        {
            if (i % columns == 0) EditorGUILayout.BeginHorizontal();

            DrawAtlasSprite(atlas.simpleSprites[i], gridIndex);
            gridIndex++;

            if (i % columns == columns - 1 || i == atlas.simpleSprites.Length - 1) EditorGUILayout.EndHorizontal();
        }
        for (int i = 0; i < atlas.motionSprites.Length; i++)
        {
            if (i % columns == 0) EditorGUILayout.BeginHorizontal();

            DrawAtlasSprite(atlas.motionSprites[i].sprite, gridIndex, motionSpriteNullable: atlas.motionSprites[i]);
            gridIndex++;

            if (i % columns == columns - 1 || i == atlas.motionSprites.Length - 1) EditorGUILayout.EndHorizontal();
        }
        for (int i = 0; i < atlas.slicedSprites.Length; i++)
        {
            if (i % columns == 0) EditorGUILayout.BeginHorizontal();

            DrawAtlasSprite(atlas.slicedSprites[i].sprite, gridIndex, slicedSprite: atlas.slicedSprites[i]);
            gridIndex++;

            if (i % columns == columns - 1 || i == atlas.slicedSprites.Length - 1) EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        #endregion

        #region Preview Window

        EditorGUILayout.BeginVertical(GUIHalfWindowWidth);

        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        DrawPreview();
        EditorGUILayout.EndVertical();
        #endregion

        EditorGUILayout.EndHorizontal();

        GUI.enabled = atlas != null;
    }
    private void GenerateSprites()
    {
        Color32[] pixels = atlas.texture.GetPixels32();
        bool[] visited = new bool[pixels.Length];

        if (atlas.simpleSprites.Length > 0)
        {
            simpleSpritesList = atlas.simpleSprites.ToList();
        }
        else
        {
            simpleSpritesList = new List<SimpleSprite>();
        }
        motionSpritesList = new List<MotionSprite>();
        slicedSpritesList = new List<SliceSprite>();

        int curSpriteIndex = 0;
        for (int y = atlas.texture.height - 1; y >= 0; y--)
        {
            for (int x = 0; x < atlas.texture.width; x++)
            {
                int index = x + y * atlas.texture.width;

                if (visited[index]) continue;

                if (pixels[index].a == 0) continue;

                List<Vector2Int> pixelPositions = FloodFill(x, y, atlas.texture.width, atlas.texture.height, visited, pixels);

                if (pixelPositions.Count < 10) continue;
                
                CreateAtlasSprite(pixelPositions, atlas.texture.width, atlas.texture.height, pixels, curSpriteIndex);
                curSpriteIndex++;
            }
        }

        atlas.simpleSprites = simpleSpritesList.ToArray();
        atlas.motionSprites = motionSpritesList.ToArray();
        atlas.slicedSprites = slicedSpritesList.ToArray();
        SortAtlasSprites();
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
    private void CreateAtlasSprite(List<Vector2Int> pixelPositions, float texWidth, float texHeight, Color32[] pixelColors, int index)
    {

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
        
        List<MarkerPosition> spriteMarkers = new List<MarkerPosition>();
        Vector2 pivot = new Vector2(minX, minY);
        Vector2[] slices = new Vector2[2];
        bool foundPivot = false;
        int slicesFound = 0;

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
                if (slicesFound < 2 && pixelColor.r == atlas.sliceColor.r && pixelColor.g == atlas.sliceColor.g && pixelColor.b == atlas.sliceColor.b)
                {
                    slices[slicesFound].x = x;
                    slices[slicesFound].y = y;
                    slicesFound++;
                }

                for (int j = 0; j < atlas.markers.Length; j++)
                {
                    MarkerKey atlasMarker = atlas.markers[j];

                    if (atlasMarker.color.r != pixelColor.r || atlasMarker.color.g != pixelColor.g || atlasMarker.color.b != pixelColor.b) continue;
                    MarkerPosition newSpriteMarker = new MarkerPosition();
                    newSpriteMarker.type = atlasMarker.type;
                    newSpriteMarker.objectPos.x = (x - minX) / PIXELS_PER_UNIT;
                    newSpriteMarker.objectPos.y = (y - minY) / PIXELS_PER_UNIT;
                    spriteMarkers.Add(newSpriteMarker);
                }
            }
        }

        SimpleSprite newSimpleSprite = new SimpleSprite();


        float spriteWidth = maxX - minX + 1;
        float spriteHeight = maxY - minY + 1;

        newSimpleSprite.uvPos.x = minX / texWidth;
        newSimpleSprite.uvPos.y = minY / texHeight;
        newSimpleSprite.uvSize.x = spriteWidth / texWidth;
        newSimpleSprite.uvSize.y = spriteHeight / texHeight;

        if (foundPivot)
        {
            MotionSprite newMotionSprite = new MotionSprite();
            newMotionSprite.markers = spriteMarkers.ToArray();
            newMotionSprite.sprite = newSimpleSprite;
            motionSpritesList.Add(newMotionSprite);
            newSimpleSprite.uvPivot.x = (pivot.x - minX) / spriteWidth;
            newSimpleSprite.uvPivot.y = (pivot.y - minY) / spriteHeight;
        }
        else if (slicesFound >  0)
        {
            SliceSprite sliceSprite = new SliceSprite();
            sliceSprite.slices = slices;
            slicedSpritesList.Add(sliceSprite);
        }
        else
        {
            if (index < simpleSpritesList.Count)
            {
                newSimpleSprite.uvPivot = simpleSpritesList[index].uvPivot;
                simpleSpritesList[index] = newSimpleSprite;
            }
            else
            {
                simpleSpritesList.Add(newSimpleSprite);
            }
        }
    }
    private void SortAtlasSprites()
    {
        if (atlas.simpleSprites.Length > 1)
        {
            Array.Sort(atlas.simpleSprites, (a, b) =>
            {
                if (Mathf.Abs(b.uvPos.y - a.uvPos.y) > spriteOrderTolerance) return b.uvPos.y.CompareTo(a.uvPos.y);

                return a.uvPos.x.CompareTo(b.uvPos.x);
            });
            for (int i = 0; i < atlas.simpleSprites.Length; i++)
            {
                atlas.simpleSprites[i].index = i;
            }
        }

        if (atlas.motionSprites.Length > 1)
        {
            Array.Sort(atlas.motionSprites, (a, b) =>
            {
                if (Mathf.Abs(b.sprite.uvPos.y - a.sprite.uvPos.y) > spriteOrderTolerance) return b.sprite.uvPos.y.CompareTo(a.sprite.uvPos.y);

                return a.sprite.uvPos.x.CompareTo(b.sprite.uvPos.x);
            });
            for (int i = 0; i < atlas.motionSprites.Length; i++)
            {
                atlas.motionSprites[i].sprite.index = i;
            }
        }
        if (atlas.slicedSprites.Length > 1)
        {
            Array.Sort(atlas.slicedSprites, (a, b) =>
            {
                if (Mathf.Abs(b.sprite.uvPos.y - a.sprite.uvPos.y) > spriteOrderTolerance) return b.sprite.uvPos.y.CompareTo(a.sprite.uvPos.y);

                return a.sprite.uvPos.x.CompareTo(b.sprite.uvPos.x);
            });
            for (int i = 0; i < atlas.slicedSprites.Length; i++)
            {
                atlas.slicedSprites[i].sprite.index = i;
            }
        }
    }
    private void MakeTextureReadable(Texture texture)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(path);
        if (textureImporter.isReadable) return;

        textureImporter.isReadable = true;
        textureImporter.SaveAndReimport();

    }
    private void GetMotionData()
    {
        motionSelections = new int[atlas.motionSprites.Length];
        holdTimeValues = new int[atlas.motionSprites.Length];
        for (int i = 0; i < atlas.motionSprites.Length; i++)
        {
            for (int j = 0; j < atlas.clips.Length; j++)
            {
                AtlasClip clip = atlas.clips[j];

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
    private void DrawAtlasSprite(SimpleSprite atlasSprite, int gridIndex, MotionSprite? motionSpriteNullable = null, SliceSprite? slicedSprite = null)
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

        GUI.depth = 1;
        if (selectedIndex != gridIndex && GUI.Button(spriteRect, GUIContent.none, GUIStyle.none))
        {
            selectedIndex = gridIndex;
            previewSprite = atlasSprite;
            curFrameIndex = 0;
        }

        if (selectedIndex == gridIndex)
        {
            Handles.DrawSolidRectangleWithOutline(spriteRect, Color.clear, Color.blueViolet);
        }

        if (motionSpriteNullable.HasValue)
        {
            MotionSprite motionSprite = motionSpriteNullable.Value;

            if (selectedIndex == gridIndex)
            {
                selectedMotionIndex = motionSelections[motionSprite.sprite.index];
            }

            for (int i = 0; i < motionSprite.markers.Length; i++)
            {
                MarkerPosition marker = motionSprite.markers[i];

                for (int j = 0; j < atlas.markers.Length; j++)
                {
                    MarkerKey atlasMarker = atlas.markers[j];
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

            float spriteDataRectHeight = 16;
            float clipDataRectWidth = cellSize * 0.5f;
            float clipDataRectYPos = gridRect.yMax + 2;
            float minRectX = spriteRect.center.x - clipDataRectWidth;

            Rect animTypeRect = new Rect(minRectX, clipDataRectYPos, clipDataRectWidth, spriteDataRectHeight);
            Rect holdTimeRect = new Rect(spriteRect.center.x, clipDataRectYPos, clipDataRectWidth, spriteDataRectHeight);

            EditorGUI.BeginChangeCheck();

            int prevMotion = motionSelections[motionSprite.sprite.index];

            switch (atlas.entityMotionType)
            {
                case EntityMotionType.NPC:
                {
                    NPCMotion nextNPCMotion = (NPCMotion)EditorGUI.EnumPopup(animTypeRect, (NPCMotion)prevMotion);
                    motionSelections[motionSprite.sprite.index] = (int)nextNPCMotion;
                }
                break;
                case EntityMotionType.Spy:
                {
                    SpyMotion nextSpyMotion = (SpyMotion)EditorGUI.EnumPopup(animTypeRect, (SpyMotion)prevMotion);
                    motionSelections[motionSprite.sprite.index] = (int)nextSpyMotion;
                }
                break;
            }

            int prevHoldTimeValue = holdTimeValues[motionSprite.sprite.index];
            int selectedHoldTimeValue = EditorGUI.IntField(holdTimeRect, prevHoldTimeValue);
            holdTimeValues[motionSprite.sprite.index] = selectedHoldTimeValue;

            if (EditorGUI.EndChangeCheck())
            {
                if (prevMotion == motionSelections[motionSprite.sprite.index] && prevHoldTimeValue == selectedHoldTimeValue) return;
                selectedIndex = gridIndex;
                curFrameIndex = 0;

                bool foundKeyframe = false;
                int selectedClipIndex = -1;
                int prevClipIndex = -1;
                for (int i = 0; i < atlas.clips.Length; i++)
                {
                    AtlasClip clip = atlas.clips[i];

                    if (clip.motionIndex == motionSelections[motionSprite.sprite.index])
                    {
                        for (int j = 0; j < clip.keyFrames.Length; j++)
                        {
                            AtlasKeyframe keyframe = clip.keyFrames[j];
                            if (keyframe.spriteIndex == motionSprite.sprite.index)
                            {
                                foundKeyframe = true;
                                if (selectedHoldTimeValue > -1)
                                {
                                    atlas.clips[i].keyFrames[j].holdTime = selectedHoldTimeValue;
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
                    if (motionSelections[motionSprite.sprite.index] > 0 && selectedHoldTimeValue > -1)
                    {
                        List<AtlasClip> clipsList = atlas.clips.ToList();

                        AtlasClip newClip = new AtlasClip();

                        newClip.motionIndex = motionSelections[motionSprite.sprite.index];

                        switch (atlas.entityMotionType)
                        {
                            case EntityMotionType.NPC:
                            {
                                newClip.clipName = ((NPCMotion)motionSelections[motionSprite.sprite.index]).ToString();
                            }
                            break;

                            case EntityMotionType.Spy:
                            {
                                newClip.clipName = ((SpyMotion)motionSelections[motionSprite.sprite.index]).ToString();

                            }
                            break;
                        }

                        newClip.keyFrames = new AtlasKeyframe[1];
                        newClip.keyFrames[0].spriteIndex = motionSprite.sprite.index;
                        newClip.keyFrames[0].holdTime = selectedHoldTimeValue;
                        clipsList.Add(newClip);
                        atlas.clips = clipsList.ToArray();
                    }
                }
                else if (!foundKeyframe && selectedHoldTimeValue > -1) // Creating new frame
                {
                    List<AtlasKeyframe> selectedClipKeyframesList = atlas.clips[selectedClipIndex].keyFrames.ToList();
                    AtlasKeyframe newKeyframe = new AtlasKeyframe();
                    newKeyframe.spriteIndex = motionSprite.sprite.index;
                    newKeyframe.holdTime = selectedHoldTimeValue;
                    selectedClipKeyframesList.Add(newKeyframe);
                    atlas.clips[selectedClipIndex].keyFrames = selectedClipKeyframesList.ToArray();
                }

                if (prevClipIndex != -1)
                {
                    AtlasClip curClip = atlas.clips[prevClipIndex];
                    List<AtlasKeyframe> keyframesList = curClip.keyFrames.ToList();
                    for (int i = 0; i < keyframesList.Count; i++)
                    {
                        if (keyframesList[i].spriteIndex == motionSprite.sprite.index)
                        {
                            keyframesList.RemoveAt(i);
                            break;
                        }
                    }

                    if (keyframesList.Count == 0) // remove empty clips
                    {
                        List<AtlasClip> clipList = atlas.clips.ToList();
                        clipList.RemoveAt(prevClipIndex);
                        atlas.clips = clipList.ToArray();

                    }
                    else
                    {
                        atlas.clips[prevClipIndex].keyFrames = keyframesList.ToArray();
                    }
                }

                editorTimeDelta = 0;
                prevFrameIndex = 0;
                curFrameIndex = 0;
                atlas.UpdateClipDictionary();
                EditorUtility.SetDirty(atlas);
                AssetDatabase.SaveAssets();
            }
        }
        else if (slicedSprite.HasValue)
        {
            Vector2[] slicePositions = slicedSprite.Value.slices;

            for (int i = 0; i < slicePositions.Length; i++)
            {
                Vector2 bottomSlicePos = slicePositions[i];
                Vector2 topSlicePos = new Vector2(bottomSlicePos.x, pivotRect.yMax);
                Handles.DrawLine(bottomSlicePos, topSlicePos);
            }
        }
        else
        {
            if (selectedIndex == gridIndex)
            {
                float rectSize = cellSize * 0.05f;
                for (int i = 0; i < simplePivots.Length; i++)
                {
                    Vector2 curPivot = simplePivots[i];
                    Vector2 rectOffset = spriteRect.size * new Vector2(curPivot.x, 1 - curPivot.y);
                    Vector2 buttonOffset = new Vector2(rectSize, rectSize) * new Vector2(1 - curPivot.x, curPivot.y);

                    Rect buttonPos = new Rect(spriteRect.min + rectOffset - buttonOffset, new Vector2(rectSize, rectSize));

                    GUI.depth = 0;
                    if (GUI.Button(buttonPos, GUIContent.none))
                    {
                        atlas.simpleSprites[selectedIndex].uvPivot = curPivot;
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
        GUI.Label(new Rect(indexPos, new Vector2(20, 20)), atlasSprite.index.ToString(), indexStyle);
    }
    private void DrawPreview()
    {

        if (previewRT == null || previewRT.width != previewRect.width)
        {
            previewRect = new Rect(halfWindowWidth + 50, 200, 700, 700);
            previewRT = new RenderTexture((int)previewRect.width, (int)previewRect.height, 16, RenderTextureFormat.ARGB32);
            previewRT.Create();
            previewRT.filterMode = FilterMode.Point;
        }

        RenderTexture.active = previewRT;
        GL.Clear(true, true, previewBGColor);

        float spritePixelHeight = previewSprite.uvSize.y * atlas.texture.height;

        float aspect = position.width / position.height;
        float scaledWidth = atlas.texture.width * previewScale * aspect;
        float scaledHeight = spritePixelHeight * previewScale;

        float scaledSpriteWidth = scaledWidth * previewSprite.uvSize.x;

        // Pivot offset in pixels
        float pivotOffsetX = previewSprite.uvPivot.x * scaledSpriteWidth;
        float pivotOffsetY = previewSprite.uvPivot.y * scaledHeight;

        float posX = pivotOffsetX * -flip;

        if (!previewClip.Equals(default(AtlasClip)))
        {
            float maxClipOffset = float.MinValue;
            for (int i = 0; i < previewClip.keyFrames.Length; i++)
            {
                SimpleSprite curSprite = atlas.motionSprites[previewClip.keyFrames[i].spriteIndex].sprite;
                float pivotWithFlip = flip == 1 ? curSprite.uvPivot.x : 1 - curSprite.uvPivot.x;
                float curSpritePivotPixelLength = pivotWithFlip * curSprite.uvSize.x * scaledWidth;

                if (curSpritePivotPixelLength > maxClipOffset)
                {
                    maxClipOffset = curSpritePivotPixelLength;
                }
            }

            posX += maxClipOffset;
        }
        else
        {
            float pivotWithFlip = flip == 1 ? previewSprite.uvPivot.x : 1 - previewSprite.uvPivot.x;
            posX += pivotWithFlip * scaledSpriteWidth;
        }
            float posY = (previewRT.height - scaledHeight) + pivotOffsetY;

        Rect destRect = new Rect(posX, posY, scaledSpriteWidth * flip, scaledHeight);
        Rect uvRect = new Rect(previewSprite.uvPos.x, previewSprite.uvPos.y, previewSprite.uvSize.x, previewSprite.uvSize.y);

        Graphics.DrawTexture(destRect, atlas.texture, uvRect, 0, 0, 0, 0);

        RenderTexture.active = null;
        GUI.DrawTexture(previewRect, previewRT);
    }
    private void UpdateMotionPreview()
    {
        if (atlas == null || atlas.motionSprites == null || atlas.clipDict == null) return;
        double now = EditorApplication.timeSinceStartup;
        float delta = (float)(now - lastEditorTime);
        lastEditorTime = now;
        editorTimeDelta += delta;

        if (startMotion)
        {
            if (!atlas.clipDict.TryGetValue(selectedMotionIndex, out AtlasClip clip)) return;

            SetNextFrameIndex(clip, ref editorTimeDelta, ref curFrameIndex, ref prevFrameIndex);

            if (curFrameIndex != prevFrameIndex)
            {
                previewSprite = atlas.motionSprites[clip.keyFrames[curFrameIndex].spriteIndex].sprite;
                Repaint();
            }
        }
    }
}
