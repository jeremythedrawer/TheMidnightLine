using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Atlas;

public class AtlasFactory : EditorWindow
{
    static Color selectedColor = Color.red;
    static Color unselectedColor = new Color(0, 0.5f, 0.5f);

    const float HEADER_COL_HEIGHT = 20;
    const float PADDING = 100;
    const float MARKER_SIZE = 2f;

    static GUILayoutOption[] headerLayout;
    static GUILayoutOption[] inputLayout;
    static GUILayoutOption[] buttonLayout;

    static GUILayoutOption[] clipButtonLayout;
    static GUILayoutOption[] clipToggleLayout;
    static GUILayoutOption[] clipEnumLayout;
    static GUILayoutOption[] clipIntLayout;

    static Vector2 directoryScroll;
    static Vector2 clipScroll;

    public AtlasSO atlas;
    public Texture2D markerTexture;
    public bool generateSprites;

    public float cellSize;
    public int selectedGridIndex;
    

    //Preview
    private int selectedMotionIndex;
    private bool[] enabledClipsCheckboxes;
    private Array atlasEnum_array;

    private Rect previewRect;
    private SimpleSprite previewSprite;
    private int flip;
    private Color32 previewBGColor;

    List<SimpleSprite> newSimpleSprites;
    List<MotionSprite> newMotionSprites;
    List<SliceSprite> newSliceSprites;

    //Simple
    private Vector2[] simplePivots = new Vector2[]
    {
        new Vector2(0.0f, 1.0f), new Vector2(0.5f, 1.0f), new Vector2(1.0f, 1.0f),
        new Vector2(0.0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1.0f, 0.5f),
        new Vector2(0.0f, 0.0f), new Vector2(0.5f, 0.0f), new Vector2(1.0f, 0.0f),
    };

    //Motion
    private AtlasClip previewClip;
    private float editorTimeDelta;
    private double lastEditorTime;
    private int curFrameIndex;
    private int prevFrameIndex;
    private bool startMotion;
    private int simpleSpritesFound;
    private int motionSpritesFound;
    private int sliceSpritesFound;

    GUILayoutOption[] spriteDirectoryLayout;
    GUILayoutOption[] previewLayout;
    GUILayoutOption[] overviewLayout;

    private float directoryWidth = 0.33f;
    private float previewWidth = 0.33f;
    private float overviewWidth = 0.33f;

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
        Init();
        DrawWindowOptions();
        DrawFirstHeader();
        EditorGUILayout.BeginHorizontal();
            DrawSpriteDirectorySection();
            DrawPreviewSection();
            DrawOverviewSection();
        EditorGUILayout.EndHorizontal();
    }
    private void Init()
    {
        spriteDirectoryLayout = new GUILayoutOption[] { GUILayout.Width(directoryWidth * position.width) };
        previewLayout = new GUILayoutOption[] { GUILayout.Width(previewWidth * position.width) };
        overviewLayout = new GUILayoutOption[] { GUILayout.Width(overviewWidth * position.width) };

        clipButtonLayout = new GUILayoutOption[] { GUILayout.Width(140f) };
        clipEnumLayout = new GUILayoutOption[] { GUILayout.Width(90f) };
        clipToggleLayout = new GUILayoutOption[] { GUILayout.Width(50f) };
        clipIntLayout = new GUILayoutOption[] { GUILayout.Width(30f) };
    }
    private void DrawWindowOptions()
    {
        EditorGUILayout.BeginHorizontal();
        {

            EditorGUILayout.LabelField("Directory Width", GUILayout.Width(140));
            directoryWidth = GUILayout.HorizontalSlider(directoryWidth, 0.1f, 1f);

            EditorGUILayout.LabelField("Preview Width", GUILayout.Width(120));
            previewWidth = GUILayout.HorizontalSlider(previewWidth, 0.1f, 1f);

            EditorGUILayout.LabelField("Atlas Overview Width", GUILayout.Width(160));
            overviewWidth = GUILayout.HorizontalSlider(overviewWidth, 0.1f, 1f);

        }
        EditorGUILayout.EndHorizontal();
    }
    private void DrawFirstHeader()
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Inputs", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            atlas = (AtlasSO)EditorGUILayout.ObjectField("Atlas", atlas, typeof(AtlasSO), allowSceneObjects: false, inputLayout);
            markerTexture = (Texture2D)EditorGUILayout.ObjectField("Marker Texture", markerTexture, typeof(Texture2D), allowSceneObjects: false, inputLayout);
            generateSprites = GUILayout.Button("Generate Sprites", buttonLayout);

        }
        EditorGUILayout.EndHorizontal();
        
        if (atlas == null || atlas.texture == null) return;

        if (EditorGUI.EndChangeCheck())
        {
            OnAtlasChange();
        }

        if (generateSprites)
        {
            simpleSpritesFound = 0;
            motionSpritesFound = 0;
            sliceSpritesFound = 0;

            previewClip = default;
            selectedGridIndex = 0;
            GenerateSprites();
            atlas.UpdateClipDictionary();

            flip = 1;
            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
    private void OnAtlasChange()
    {
        if (motionEnumDictionary.TryGetValue(atlas.entityMotionType, out Type enumType))
        {
            atlasEnum_array = Enum.GetValues(enumType);
            enabledClipsCheckboxes = new bool[atlasEnum_array.Length];

            for (int i = 0; i < atlas.clips.Length; i++)
            {
                int motionIndex = atlas.clips[i].motionIndex;
                enabledClipsCheckboxes[motionIndex] = true;
            }
        }
    }
    private void DrawSpriteDirectorySection()
    {
        if (atlas == null) return;

        EditorGUILayout.BeginVertical(spriteDirectoryLayout);
        {
            cellSize = EditorGUILayout.FloatField("Cell Size", cellSize, headerLayout);
            directoryScroll = EditorGUILayout.BeginScrollView(directoryScroll);


            int columns = Mathf.CeilToInt(((directoryWidth * position.width) - cellSize) / cellSize);
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

                DrawAtlasSprite(atlas.slicedSprites[i].sprite, gridIndex, slicedSpriteNullable: atlas.slicedSprites[i]);
                gridIndex++;

                if (i % columns == columns - 1 || i == atlas.slicedSprites.Length - 1) EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawPreviewSection()
    {
        if (atlas == null) return;
        EditorGUILayout.BeginVertical(previewLayout);
        {
            if (motionEnumDictionary.TryGetValue(atlas.entityMotionType, out Type enumType))
            {
                Enum motionEnum = (Enum)Enum.ToObject(enumType, selectedMotionIndex);
                EditorGUILayout.LabelField($"Preview Motion: {motionEnum}", headerLayout);
            }

            if (atlas.clipDict == null)
            {
                previewClip = default;
                EditorGUILayout.EndVertical(); 
                return; 
            }

            if (atlas.clipDict.TryGetValue(selectedMotionIndex, out AtlasClip clip))
            {
                previewClip = clip;
            }

            if (GUILayout.Button("Play", headerLayout))
            {
                startMotion = true;
                editorTimeDelta = 0;
                curFrameIndex = previewClip.keyframeStartIndex;
            }
            if (GUILayout.Button("Stop", headerLayout))
            {
                startMotion = false;
            }
            if (GUILayout.Button("Flip", headerLayout))
            {
                flip = flip == 1 ? -1 : 1;
            }
            DrawPreview();
            if (atlas.entityMotionType != EntityMotionType.None)
            {
                DrawClipOptions();
            }
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawClipOptions()
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Motions", clipButtonLayout);
            EditorGUILayout.LabelField("Enabled", clipToggleLayout);
            EditorGUILayout.LabelField("Clip Type", clipEnumLayout);
            EditorGUILayout.LabelField("Start", clipIntLayout);
            EditorGUILayout.LabelField("End", clipIntLayout);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        {
            clipScroll = EditorGUILayout.BeginScrollView(clipScroll);

            int validClipIndex = 0;
            for (int i = 0; i < enabledClipsCheckboxes.Length; i++)
            {
                Enum enumValue = (Enum)atlasEnum_array.GetValue(i);

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(enumValue.ToString(), clipButtonLayout))
                    {
                        if (enabledClipsCheckboxes[i])
                        {
                            selectedMotionIndex = i;
                            if (atlas.clipDict.TryGetValue(selectedMotionIndex, out AtlasClip clip))
                            {
                                curFrameIndex = clip.keyframeStartIndex;
                            }
                        }
                    }

                    EditorGUI.BeginChangeCheck();

                    enabledClipsCheckboxes[i] = EditorGUILayout.Toggle(enabledClipsCheckboxes[i], clipToggleLayout);

                    if (EditorGUI.EndChangeCheck())
                    {
                        List<AtlasClip> atlasClips_list = atlas.clips.ToList();

                        if (enabledClipsCheckboxes[i])
                        {
                            AtlasClip newClip = new AtlasClip();
                            newClip.motionIndex = i;
                            newClip.clipType = ClipType.Loop;
                            newClip.clipName = enumValue.ToString();
                            atlasClips_list.Add(newClip);
                            atlasClips_list = atlasClips_list.OrderBy(clip => clip.motionIndex).ToList();
                        }
                        else
                        {
                            atlasClips_list.RemoveAt(validClipIndex);
                        }
                        atlas.clips = atlasClips_list.ToArray();
                        atlas.UpdateClipDictionary();
                    }
                    if (enabledClipsCheckboxes[i] && atlas.clips.Length > 0)
                    {
                        EditorGUI.BeginChangeCheck();

                        atlas.clips[validClipIndex].clipType = (ClipType)EditorGUILayout.EnumPopup(atlas.clips[validClipIndex].clipType, clipEnumLayout);
                        atlas.clips[validClipIndex].keyframeStartIndex =  EditorGUILayout.IntField(atlas.clips[validClipIndex].keyframeStartIndex, clipIntLayout);
                        atlas.clips[validClipIndex].keyframeEndIndex = EditorGUILayout.IntField(atlas.clips[validClipIndex].keyframeEndIndex, clipIntLayout);
                        validClipIndex++;

                        if (EditorGUI.EndChangeCheck())
                        {
                            atlas.UpdateClipDictionary();
                        }
                    }

                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndHorizontal();
    }
    private void DrawOverviewSection()
    {
        EditorGUILayout.BeginVertical(overviewLayout);
        {
            DrawAtlasOverview();
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawPreview()
    {
        float previewSize = previewWidth * position.width;

        Rect containerRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

        if (Event.current.type != EventType.Repaint) return;

        float spritePixelWidth = previewSprite.uvSizeAndPos.x * atlas.texture.width;
        float spritePixelHeight = previewSprite.uvSizeAndPos.y * atlas.texture.height;

        float scale = Mathf.Min(containerRect.width / spritePixelWidth, containerRect.height / spritePixelHeight);

        float drawWidth = spritePixelWidth * scale;
        float drawHeight = spritePixelHeight * scale;

        Vector2 pivot = new Vector2(containerRect.center.x, containerRect.y);
        float flipUVPivot = flip == 1 ? previewSprite.uvPivot.x : 1 - previewSprite.uvPivot.x;
        float pivotX = flipUVPivot * drawWidth;
        float pivotY = (1 - previewSprite.uvPivot.y) * drawHeight;

        previewRect = new Rect(pivot.x - pivotX, pivot.y - (drawHeight - pivotY), drawWidth, drawHeight);

        Rect uvRect;

        if (flip == 1)
        {
            uvRect = new Rect(
                previewSprite.uvSizeAndPos.z,
                previewSprite.uvSizeAndPos.w,
                previewSprite.uvSizeAndPos.x,
                previewSprite.uvSizeAndPos.y
            );
        }
        else
        {
            uvRect = new Rect(
                previewSprite.uvSizeAndPos.z + previewSprite.uvSizeAndPos.x,
                previewSprite.uvSizeAndPos.w,
                -previewSprite.uvSizeAndPos.x,
                previewSprite.uvSizeAndPos.y
            );
        }


        Rect imageRect = new Rect();
        
        float spriteAspect = previewSprite.uvSizeAndPos.x / previewSprite.uvSizeAndPos.y;


        float maxWidth = 0;
        float maxHeight = 0;

        if (atlas.clipDict.TryGetValue(selectedMotionIndex, out AtlasClip clip))
        {
            for (int i = clip.keyframeStartIndex; i <= clip.keyframeEndIndex; i++)
            {
                MotionSprite motionSprite = atlas.motionSprites[i];

                maxWidth = Mathf.Max(maxWidth, motionSprite.sprite.uvSizeAndPos.x);
                maxHeight = Mathf.Max(maxHeight, motionSprite.sprite.uvSizeAndPos.y);
            }
        }
        float clipScale = Mathf.Min(containerRect.width / maxWidth, containerRect.height / maxHeight);

        imageRect.width = previewSprite.uvSizeAndPos.x * clipScale;
        imageRect.height = previewSprite.uvSizeAndPos.y * clipScale;

        imageRect.x = containerRect.center.x - previewSprite.uvPivot.x * imageRect.width;
        imageRect.y = containerRect.yMax - (1 - previewSprite.uvPivot.y) * imageRect.height;

        GUI.DrawTextureWithTexCoords(imageRect, atlas.texture, uvRect);
        Vector2 indexPos = new Vector2(containerRect.xMin, containerRect.yMin);
        GUIStyle indexStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(indexPos, new Vector2(20, 20)), curFrameIndex.ToString(), indexStyle);
        Handles.BeginGUI();
        {
            Handles.DrawSolidRectangleWithOutline(containerRect, Color.clear, selectedColor);
        }
        Handles.EndGUI();
    }
    private void GenerateSprites()
    {
        Color32[] atlasPixels = atlas.texture.GetPixels32();
        Color32[] markerPixels = new Color32[0];
        if (markerTexture != null)
        {
            markerPixels = markerTexture.GetPixels32();
        }
        bool[] visited = new bool[atlasPixels.Length];

        if (atlas.simpleSprites.Length > 0)
        {
            newSimpleSprites = atlas.simpleSprites.ToList();
        }
        else
        {
            newSimpleSprites = new List<SimpleSprite>();
        }

        if (atlas.motionSprites.Length > 0)
        {

            newMotionSprites = atlas.motionSprites.ToList();
        }
        else
        {
            newMotionSprites = new List<MotionSprite>();
        }

        if (atlas.slicedSprites.Length > 0)
        {
            newSliceSprites = atlas.slicedSprites.ToList();
        }
        else
        {
            newSliceSprites = new List<SliceSprite>();

        }

        int curSpriteIndex = 0;
        for (int y = atlas.texture.height - 1; y >= 0; y--)
        {
            for (int x = 0; x < atlas.texture.width; x++)
            {
                int index = x + y * atlas.texture.width;
                List<Vector2Int> pixelPositions = new List<Vector2Int>();

                if (visited[index]) continue;

                if (markerTexture == null)
                {
                    if (atlasPixels[index].a == 0) continue;
                    pixelPositions = FloodFill(x, y, atlas.texture.width, atlas.texture.height, ref visited, atlasPixels);
                }
                else
                {
                    if (atlasPixels[index].a == 0 && markerPixels[index].a == 0) continue;
                    pixelPositions = FloodFill(x, y, atlas.texture.width, atlas.texture.height, ref visited, atlasPixels, markerPixels);
                }
                if (pixelPositions.Count < 10) continue;
                
                CreateAtlasSprite(pixelPositions, atlas.texture.width, atlas.texture.height, atlasPixels, curSpriteIndex);
                curSpriteIndex++;
            }
        }

        SortAtlasSprites();
        SetSpriteArray();
    }
    private List<Vector2Int> FloodFill(int startX, int startY, int width, int height, ref bool[] visited, Color32[] atlasPixels, Color32[] markerPixels = null)
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

            bool isSolid = atlasPixels[index].a > 0;

            bool isMarker = false;
            if (markerPixels != null)
            {
                Color32 m = markerPixels[index];
                isMarker =
                    m.r == atlas.spriteConnectColor.r &&
                    m.g == atlas.spriteConnectColor.g &&
                    m.b == atlas.spriteConnectColor.b &&
                    m.a == 255;
            }

            if (!isSolid && !isMarker) continue;
            visited[index] = true;

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
        
        List<MarkerPosition> spriteMarkersList = new List<MarkerPosition>();
        Vector2 pivot = new Vector2(minX, minY);
        Vector2[] slices = new Vector2[2];
        bool foundPivot = false;
        int slicesFound = 0;

        if (markerTexture != null)
        {
            Color32[] markerPixels = markerTexture.GetPixels32();

            for (int x = (int)minX; x <= maxX; x++)
            {
                for (int y = (int)minY; y <= maxY; y++)
                {
                    int pixelIndex = x + y * (int)texWidth;
                    Color32 pixelColor = markerPixels[pixelIndex];
                    if (pixelColor.a == 0) continue;

                    if (!foundPivot && pixelColor.r == atlas.pivotColor.r && pixelColor.g == atlas.pivotColor.g && pixelColor.b == atlas.pivotColor.b)
                    {
                        pivot.x = x;
                        pivot.y = y;
                        foundPivot = true;
                    }
                    if (slicesFound < slices.Length && pixelColor.r == atlas.sliceColor.r && pixelColor.g == atlas.sliceColor.g && pixelColor.b == atlas.sliceColor.b)
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
                        spriteMarkersList.Add(newSpriteMarker);
                    }
                }
            }
        }

        SimpleSprite newSimpleSprite = new SimpleSprite();

        float spriteWidth = maxX - minX + 1;
        float spriteHeight = maxY - minY + 1;

        newSimpleSprite.uvSizeAndPos.x = spriteWidth / texWidth;
        newSimpleSprite.uvSizeAndPos.y = spriteHeight / texHeight;
        newSimpleSprite.uvSizeAndPos.z = (minX / texWidth);
        newSimpleSprite.uvSizeAndPos.w = (minY / texHeight);

        newSimpleSprite.worldSize.x = spriteWidth / PIXELS_PER_UNIT;
        newSimpleSprite.worldSize.y = spriteHeight / PIXELS_PER_UNIT;
        newSimpleSprite.worldSize.z = 1f;

        if (foundPivot)
        {
            MotionSprite newMotionSprite = new MotionSprite();

            newSimpleSprite.uvPivot.x = (pivot.x - minX) / spriteWidth;
            newSimpleSprite.uvPivot.y = (pivot.y - minY) / spriteHeight;
            newMotionSprite.markers = spriteMarkersList.ToArray();
            for (int i = 0; i < newMotionSprite.markers.Length; i++)
            {
                newMotionSprite.markers[i].objectPos.x -= (newSimpleSprite.uvPivot.x * newSimpleSprite.worldSize.x);
            }
            newMotionSprite.sprite = newSimpleSprite;

            if (motionSpritesFound < newMotionSprites.Count)
            {
                newMotionSprites[motionSpritesFound] = newMotionSprite;
            }
            else
            {
                newMotionSprites.Add(newMotionSprite);
            }
            motionSpritesFound++;
        }
        else if (slicesFound >  0)
        {
            SliceSprite slicedSprite = new SliceSprite();
            slicedSprite.sprite = newSimpleSprite;
            slicedSprite.slice.x = (slices[0].x - minX) / spriteWidth;
            slicedSprite.slice.y = (slices[1].x - minX) / spriteWidth;
            slicedSprite.slice.z = (slices[0].y - minY) / spriteHeight;
            slicedSprite.slice.w = (slices[1].y - minY) / spriteHeight;


            Vector2 centerSizes = new Vector2(slicedSprite.slice.y - slicedSprite.slice.x, slicedSprite.slice.w - slicedSprite.slice.z);
            Vector2 topRightSizes = new Vector2(1 - slicedSprite.slice.y, 1 -  slicedSprite.slice.w);
            
            float sizeX0 = slicedSprite.slice.x * newSimpleSprite.uvSizeAndPos.x;
            float sizeX1 = centerSizes.x * newSimpleSprite.uvSizeAndPos.x;
            float sizeX2 = topRightSizes.x * newSimpleSprite.uvSizeAndPos.x;

            float sizeY0 = slicedSprite.slice.z * newSimpleSprite.uvSizeAndPos.y;
            float sizeY1 = centerSizes.y * newSimpleSprite.uvSizeAndPos.y;
            float sizeY2 = topRightSizes.y * newSimpleSprite.uvSizeAndPos.y;

            float posX0 = newSimpleSprite.uvSizeAndPos.z;
            float posX1 = posX0 + sizeX0;
            float posX2 = posX1 + sizeX1;
            
            float posY0 = newSimpleSprite.uvSizeAndPos.w;
            float posY1 = posY0 + sizeY0;
            float posY2 = posY1 + sizeY1;


            slicedSprite.uvSizeAndPos = new Vector4[]
            {
                new Vector4(sizeX0, sizeY0, posX0, posY0),
                new Vector4(sizeX1, sizeY0, posX1, posY0),
                new Vector4(sizeX2, sizeY0, posX2, posY0),

                new Vector4(sizeX0, sizeY1, posX0, posY1),
                new Vector4(sizeX1, sizeY1, posX1, posY1),
                new Vector4(sizeX2, sizeY1, posX2, posY1),

                new Vector4(sizeX0, sizeY2, posX0, posY2),
                new Vector4(sizeX1, sizeY2, posX1, posY2),
                new Vector4(sizeX2, sizeY2, posX2, posY2),
            };

            float worldLeftSlice = (slicedSprite.slice.x * newSimpleSprite.uvSizeAndPos.x * atlas.texture.width) / PIXELS_PER_UNIT;
            float worldRightSlice = ((1 - slicedSprite.slice.y) * newSimpleSprite.uvSizeAndPos.x * atlas.texture.width) / PIXELS_PER_UNIT;
            float worldBottomSlice = (slicedSprite.slice.z * newSimpleSprite.uvSizeAndPos.y * atlas.texture.height) / PIXELS_PER_UNIT;
            float worldTopSlice = ((1 - slicedSprite.slice.w) * newSimpleSprite.uvSizeAndPos.y * atlas.texture.height) / PIXELS_PER_UNIT;

            slicedSprite.worldSlices.x = worldLeftSlice;
            slicedSprite.worldSlices.y = worldRightSlice;
            slicedSprite.worldSlices.z = worldBottomSlice;
            slicedSprite.worldSlices.w = worldTopSlice;

            if (sliceSpritesFound < newSliceSprites.Count)
            {
                newSliceSprites[sliceSpritesFound] = slicedSprite;
            }
            else
            {
                newSliceSprites.Add(slicedSprite);
            }
            sliceSpritesFound++;
        }
        else
        {
            if (simpleSpritesFound < newSimpleSprites.Count)
            {
                newSimpleSprite.uvPivot = newSimpleSprites[simpleSpritesFound].uvPivot;
                newSimpleSprites[simpleSpritesFound] = newSimpleSprite;
            }
            else
            {
                newSimpleSprites.Add(newSimpleSprite);
            }
            simpleSpritesFound++;
        }
    }
    private void SortAtlasSprites()
    {
        if (newSimpleSprites.Count > 1)
        {
            newSimpleSprites = newSimpleSprites.OrderByDescending(s => s.uvSizeAndPos.w).ThenBy(s => s.uvSizeAndPos.z).ToList();
        }
        if (newMotionSprites.Count > 1)
        {
            newMotionSprites = newMotionSprites.OrderByDescending(s => s.sprite.uvSizeAndPos.w).ThenBy(s => s.sprite.uvSizeAndPos.z).ToList();
        }
        if (newSliceSprites.Count > 1)
        {
           newSliceSprites = newSliceSprites.OrderByDescending(s => s.sprite.uvSizeAndPos.w).ThenBy(s => s.sprite.uvSizeAndPos.z).ToList();
        }
    }
    private void SetSpriteArray()
    {
        for(int i = newSimpleSprites.Count - 1; i >= 0; i--)
        {
            SimpleSprite newSprite = newSimpleSprites[i];
            Rect newRect = new Rect(newSprite.uvSizeAndPos.z, newSprite.uvSizeAndPos.w, newSprite.uvSizeAndPos.x, newSprite.uvSizeAndPos.y);

            for (int j = 0; j < atlas.simpleSprites.Length; j++)
            {
                SimpleSprite oldSprite = atlas.simpleSprites[j];

                Rect oldRect = new Rect(oldSprite.uvSizeAndPos.z, oldSprite.uvSizeAndPos.w, oldSprite.uvSizeAndPos.x,oldSprite.uvSizeAndPos.y);

                if (oldRect.Overlaps(newRect))
                {
                    newSprite.uvPivot = oldSprite.uvPivot;
                    atlas.simpleSprites[j] = newSprite;
                    newSimpleSprites.RemoveAt(i);
                    break;
                }
            }
        }

        List<SimpleSprite> simpleSpritesTemp = atlas.simpleSprites.ToList();
        simpleSpritesTemp.AddRange(newSimpleSprites);

        for (int i = 0; i < simpleSpritesTemp.Count; i++)
        {
            SimpleSprite sprite = simpleSpritesTemp[i];
            sprite.index = i;
            simpleSpritesTemp[i] = sprite;
        }
        atlas.simpleSprites = simpleSpritesTemp.ToArray();

        for (int i = newMotionSprites.Count - 1; i >= 0; i--)
        {
            SimpleSprite newSprite = newMotionSprites[i].sprite;
            Rect newRect = new Rect(newSprite.uvSizeAndPos.z, newSprite.uvSizeAndPos.w, newSprite.uvSizeAndPos.x, newSprite.uvSizeAndPos.y);
            for (int j = 0; j < atlas.motionSprites.Length; j++)
            {
                MotionSprite oldMotionSprite = atlas.motionSprites[j];
                SimpleSprite oldSprite = oldMotionSprite.sprite;
                Rect oldRect = new Rect(oldSprite.uvSizeAndPos.z, oldSprite.uvSizeAndPos.w, oldSprite.uvSizeAndPos.x, oldSprite.uvSizeAndPos.y);
                if (oldRect.Overlaps(newRect))
                {
                    atlas.motionSprites[j] = newMotionSprites[i];
                    atlas.motionSprites[j].holdFrames = oldMotionSprite.holdFrames;
                    newMotionSprites.RemoveAt(i);
                    break;
                }
            }
        }
        List<MotionSprite> motionSpritesTemp = atlas.motionSprites.ToList();
        motionSpritesTemp.AddRange(newMotionSprites);

        for (int i = 0; i < motionSpritesTemp.Count; i++)
        {
            MotionSprite motionSprite = motionSpritesTemp[i];
            motionSprite.sprite.index = i;
            motionSpritesTemp[i] = motionSprite;
        }
        atlas.motionSprites = motionSpritesTemp.ToArray();


        for (int i = newSliceSprites.Count - 1; i >= 0; i--)
        {
            SimpleSprite newSprite = newSliceSprites[i].sprite;
            Rect newRect = new Rect(newSprite.uvSizeAndPos.z, newSprite.uvSizeAndPos.w, newSprite.uvSizeAndPos.x, newSprite.uvSizeAndPos.y);
            for (int j = 0; j < atlas.slicedSprites.Length; j++)
            {
                SimpleSprite oldSprite = atlas.slicedSprites[j].sprite;
                Rect oldRect = new Rect(oldSprite.uvSizeAndPos.z, oldSprite.uvSizeAndPos.w, oldSprite.uvSizeAndPos.x, oldSprite.uvSizeAndPos.y);
                if (oldRect.Overlaps(newRect))
                {
                    atlas.slicedSprites[j] = newSliceSprites[i];
                    newSliceSprites.RemoveAt(i);
                    break;
                }
            }
        }
        List<SliceSprite> sliceSpritesTemp = atlas.slicedSprites.ToList();
        sliceSpritesTemp.AddRange(newSliceSprites);

        for (int i = 0; i < sliceSpritesTemp.Count; i++)
        {
            SliceSprite sliceSprite = sliceSpritesTemp[i];
            sliceSprite.sprite.index = i;
            sliceSpritesTemp[i] = sliceSprite;
        }
        atlas.slicedSprites = sliceSpritesTemp.ToArray();
    }
    private void DrawAtlasSprite(SimpleSprite atlasSprite, int gridIndex, MotionSprite? motionSpriteNullable = null, SliceSprite? slicedSpriteNullable = null)
    {
        Rect gridRect = GUILayoutUtility.GetRect(cellSize, cellSize, GUILayout.ExpandWidth(false));

        Vector2 uvSize = new Vector2(atlasSprite.uvSizeAndPos.x, atlasSprite.uvSizeAndPos.y);
        Vector2 uvPos = new Vector2(atlasSprite.uvSizeAndPos.z, atlasSprite.uvSizeAndPos.w);
        
        Rect uvRect = new Rect(uvPos, uvSize);

        float spritePixelWidth = atlasSprite.uvSizeAndPos.x * atlas.texture.width;
        float spritePixelHeight = atlasSprite.uvSizeAndPos.y * atlas.texture.height;

        float aspectRatio = Mathf.Min(gridRect.width / spritePixelWidth, gridRect.height / spritePixelHeight);
        Vector2 drawSize = new Vector2(spritePixelWidth * aspectRatio, spritePixelHeight * aspectRatio);
        Rect spriteRect = new Rect(gridRect.xMin, gridRect.yMin, drawSize.x * 0.9f, drawSize.y * 0.9f);

        GUI.DrawTextureWithTexCoords(spriteRect, atlas.texture, uvRect);

        Vector2 pivotPos = new Vector2(Mathf.Lerp(spriteRect.xMin, spriteRect.xMax, atlasSprite.uvPivot.x),Mathf.Lerp(spriteRect.yMax, spriteRect.yMin, atlasSprite.uvPivot.y));
        Rect pivotRect = new Rect(pivotPos - Vector2.one * MARKER_SIZE, Vector2.one * MARKER_SIZE);

        Handles.BeginGUI();
        Handles.color = atlas.pivotColor;
        Handles.DrawWireDisc(pivotPos, Vector3.forward, MARKER_SIZE);

        GUIStyle spriteTextStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = Color.white }
        };

        Vector2 spriteTextSize = new Vector2(gridRect.width - spriteRect.width, 20);

        Vector2 spriteIndexTextPos = new Vector2(spriteRect.xMax, spriteRect.yMin);
        Vector2 clipNameTextPos = new Vector2(spriteRect.xMax, spriteIndexTextPos.y + spriteTextSize.y);

        Rect spriteIndexTextRect = new Rect(spriteIndexTextPos, spriteTextSize);
        Rect clipNameRect = new Rect(clipNameTextPos, spriteTextSize);

        GUI.Label(spriteIndexTextRect, "Index: " + atlasSprite.index.ToString(), spriteTextStyle);

        Color defaultColor = Color.grey;

        Handles.color = selectedGridIndex == gridIndex ? selectedColor : defaultColor;
        Handles.DrawSolidRectangleWithOutline(gridRect, Color.clear, Handles.color);

        Event e = Event.current;

        if (gridRect.Contains(e.mousePosition) && selectedGridIndex != gridIndex)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                selectedGridIndex = gridIndex;
                previewSprite = atlasSprite;
                e.Use();
            }
        }

        Vector2 spriteButtonSize = new Vector2(cellSize * 0.05f, cellSize * 0.05f);

        if (motionSpriteNullable.HasValue)
        {
            MotionSprite motionSprite = motionSpriteNullable.Value;
            for (int i = 0; i < atlas.clips.Length; i++)
            {
                AtlasClip atlasClip = atlas.clips[i];

                if (atlasSprite.index >= atlasClip.keyframeStartIndex && atlasSprite.index <= atlasClip.keyframeEndIndex)
                {
                    GUI.Label(clipNameRect, "Clip: " + atlasClip.clipName, spriteTextStyle);
                    defaultColor = unselectedColor;
                    break;
                }
            }
            for (int i = 0; i < motionSprite.markers.Length; i++)
            {
                MarkerPosition marker = motionSprite.markers[i];

                for (int j = 0; j < atlas.markers.Length; j++)
                {
                    MarkerKey atlasMarker = atlas.markers[j];
                    if ((marker.type & atlasMarker.type) != 0)
                    {
                        Vector2 markerPixelPos = marker.objectPos * PIXELS_PER_UNIT;
                        Vector2 markerNormalized = new Vector2(markerPixelPos.x / spritePixelWidth, markerPixelPos.y / spritePixelHeight);
                        markerNormalized.x += atlasSprite.uvPivot.x;
                        markerNormalized.y += atlasSprite.uvPivot.y;
                        Vector2 markerRectPos =  new Vector2(Mathf.Lerp(spriteRect.xMin, spriteRect.xMax, markerNormalized.x), Mathf.Lerp(spriteRect.yMax, spriteRect.yMin, markerNormalized.y));
                        Rect markerRect = new Rect(markerRectPos - Vector2.one * MARKER_SIZE, Vector2.one * (MARKER_SIZE * 2));
                        Handles.color = atlasMarker.color;
                        Handles.DrawWireDisc(markerRectPos, Vector3.forward, MARKER_SIZE);
                        break;
                    }
                }
            }

            Rect holdTimeRect = new Rect(spriteRect.xMin, spriteRect.yMax, spriteRect.width, 16);

            EditorGUI.BeginChangeCheck();

            atlas.motionSprites[motionSprite.sprite.index].holdFrames = EditorGUI.IntField(holdTimeRect, atlas.motionSprites[motionSprite.sprite.index].holdFrames);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(atlas);
                AssetDatabase.SaveAssetIfDirty(atlas);
            }


        }
        else if (slicedSpriteNullable.HasValue)
        {
            if (selectedGridIndex == gridIndex)
            {
                SliceSprite slicedSprite = slicedSpriteNullable.Value;
                Handles.color = Color.orange;
                float leftPos = (slicedSprite.slice.x * spriteRect.width) + spriteRect.xMin;
                float rightPos = (slicedSprite.slice.y * spriteRect.width) + spriteRect.xMin;
                float bottomPos = spriteRect.yMax - (slicedSprite.slice.z * spriteRect.height);
                float topPos = spriteRect.yMax - (slicedSprite.slice.w * spriteRect.height);

                Handles.DrawLine(new Vector3(leftPos, spriteRect.yMin), new Vector3(leftPos, spriteRect.yMax));
                Handles.DrawLine(new Vector3(rightPos, spriteRect.yMin), new Vector3(rightPos, spriteRect.yMax));

                Handles.DrawLine(new Vector3(spriteRect.xMin, bottomPos), new Vector3(spriteRect.xMax, bottomPos));
                Handles.DrawLine(new Vector3(spriteRect.xMin, topPos), new Vector3(spriteRect.xMax, topPos));

                for (int i = 0; i < simplePivots.Length; i++)
                {
                    Vector2 curPivot = simplePivots[i];
                    Vector2 rectOffset = gridRect.size * new Vector2(curPivot.x, 1 - curPivot.y);
                    Vector2 buttonOffset = spriteButtonSize * new Vector2(curPivot.x, 1 - curPivot.y);

                    Rect buttonRect = new Rect(gridRect.min + rectOffset - buttonOffset, spriteButtonSize);

                    GUI.depth = 0;
                    if (GUI.Button(buttonRect, GUIContent.none))
                    {
                        atlas.slicedSprites[slicedSprite.sprite.index].sprite.uvPivot = curPivot;

                        EditorUtility.SetDirty(atlas);
                        AssetDatabase.SaveAssetIfDirty(atlas);
                    }
                }
            }
        }
        else
        {
            if (selectedGridIndex == gridIndex)
            {
                for (int i = 0; i < simplePivots.Length; i++)
                {
                    Vector2 curPivot = simplePivots[i];
                    Vector2 rectOffset = gridRect.size * new Vector2(curPivot.x, 1 - curPivot.y);
                    Vector2 buttonOffset = spriteButtonSize * new Vector2(curPivot.x, 1- curPivot.y);

                    Rect buttonRect = new Rect(gridRect.min + rectOffset - buttonOffset, spriteButtonSize);

                    GUI.depth = 0;
                    if (GUI.Button(buttonRect, GUIContent.none))
                    {
                        atlas.simpleSprites[selectedGridIndex].uvPivot = curPivot;
                        EditorUtility.SetDirty(atlas);
                        AssetDatabase.SaveAssetIfDirty(atlas);
                    }
                }
            }
        }
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.LeftArrow)
        {
            if (selectedGridIndex == 0) return;
            selectedGridIndex--;
            if (motionSpriteNullable.HasValue)
            {
                int motionIndex = selectedGridIndex - atlas.simpleSprites.Length;
                previewSprite = atlas.motionSprites[motionIndex].sprite;
            }
            else if (slicedSpriteNullable.HasValue)
            {
                int sliceIndex = selectedGridIndex - atlas.simpleSprites.Length - atlas.motionSprites.Length;
                previewSprite = atlas.motionSprites[sliceIndex].sprite;
            }
            else
            {
               previewSprite = atlas.simpleSprites[selectedGridIndex];
            }
            e.Use();
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.RightArrow)
        {
            if (motionSpriteNullable.HasValue)
            {
                int motionIndex = selectedGridIndex - atlas.simpleSprites.Length;
                if (motionIndex == atlas.motionSprites.Length - 1) return;
                selectedGridIndex++;
                motionIndex = selectedGridIndex - atlas.simpleSprites.Length;

                previewSprite = atlas.motionSprites[motionIndex].sprite;
            }
            else if (slicedSpriteNullable.HasValue)
            {
                int sliceIndex = selectedGridIndex - atlas.simpleSprites.Length - atlas.motionSprites.Length;
                if (sliceIndex == atlas.slicedSprites.Length - 1) return;
                selectedGridIndex++;
                sliceIndex = selectedGridIndex - atlas.simpleSprites.Length - atlas.motionSprites.Length;

                previewSprite = atlas.motionSprites[sliceIndex].sprite;
            }
            else
            {
                if (selectedGridIndex == atlas.simpleSprites.Length - 1) return;
                selectedGridIndex++;

                previewSprite = atlas.simpleSprites[selectedGridIndex];
            }
            e.Use();
        }

        if (selectedGridIndex == gridIndex)
        {
            float quarterWidth = spriteRect.width * 0.25f;
            Vector2 shiftLeftPos = new Vector2(gridRect.center.x - quarterWidth, gridRect.center.y);
            Vector2 shiftRightPos = new Vector2(gridRect.center.x + quarterWidth, gridRect.center.y);
            Rect shiftLeftRect = new Rect(shiftLeftPos, spriteButtonSize);
            Rect shiftRightRect = new Rect(shiftRightPos, spriteButtonSize);

            Handles.color = Color.yellowNice;
            Handles.DrawSolidRectangleWithOutline(shiftLeftRect, Color.yellowNice, Color.black);
            Handles.DrawSolidRectangleWithOutline(shiftRightRect, Color.yellowNice, Color.black);

            
            if ((shiftLeftRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0))
            {
                HandleShiftLeft();
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Q)
            {
                HandleShiftLeft();
                e.Use();
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.E)
            {
                HandleShiftRight();
                e.Use();
            }
            
            if ((shiftRightRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0))
            {
                HandleShiftRight();
            }

            void HandleShiftRight()
            {
                if (motionSpriteNullable.HasValue)
                {
                    int motionIndex = selectedGridIndex - atlas.simpleSprites.Length;
                    if (motionIndex == atlas.motionSprites.Length - 1) return;

                    atlas.motionSprites[motionIndex] = atlas.motionSprites[motionIndex + 1];
                    atlas.motionSprites[motionIndex].sprite.index = motionIndex;

                    atlas.motionSprites[motionIndex + 1] = motionSpriteNullable.Value;
                    atlas.motionSprites[motionIndex + 1].sprite.index = motionIndex + 1;

                    selectedGridIndex++;
                }
                else if (slicedSpriteNullable.HasValue)
                {
                    int sliceIndex = selectedGridIndex - atlas.simpleSprites.Length - atlas.motionSprites.Length;
                    if (sliceIndex == atlas.slicedSprites.Length - 1) return;

                    atlas.slicedSprites[sliceIndex] = atlas.slicedSprites[sliceIndex + 1];
                    atlas.slicedSprites[sliceIndex].sprite.index = sliceIndex;

                    atlas.slicedSprites[sliceIndex + 1] = slicedSpriteNullable.Value;
                    atlas.slicedSprites[sliceIndex + 1].sprite.index = sliceIndex + 1;

                    selectedGridIndex++;
                }
                else
                {
                    if (selectedGridIndex == atlas.simpleSprites.Length - 1) return;

                    atlas.simpleSprites[selectedGridIndex] = atlas.simpleSprites[selectedGridIndex + 1];
                    atlas.simpleSprites[selectedGridIndex].index = selectedGridIndex;

                    atlas.simpleSprites[selectedGridIndex + 1] = atlasSprite;
                    atlas.simpleSprites[selectedGridIndex + 1].index = selectedGridIndex + 1;

                    selectedGridIndex++;
                }

                EditorUtility.SetDirty(atlas);
                AssetDatabase.SaveAssets();
            }
            void HandleShiftLeft()
            {
                if (selectedGridIndex == 0) return;
                if (motionSpriteNullable.HasValue)
                {
                    int motionIndex = selectedGridIndex - atlas.simpleSprites.Length;

                    atlas.motionSprites[motionIndex] = atlas.motionSprites[motionIndex - 1];
                    atlas.motionSprites[motionIndex].sprite.index = motionIndex;


                    atlas.motionSprites[motionIndex - 1] = motionSpriteNullable.Value;
                    atlas.motionSprites[motionIndex - 1].sprite.index = motionIndex - 1;
                }
                else if (slicedSpriteNullable.HasValue)
                {
                    int sliceIndex = selectedGridIndex - atlas.simpleSprites.Length - atlas.motionSprites.Length;

                    atlas.slicedSprites[sliceIndex] = atlas.slicedSprites[sliceIndex - 1];
                    atlas.slicedSprites[sliceIndex].sprite.index = sliceIndex;

                    atlas.slicedSprites[sliceIndex - 1] = slicedSpriteNullable.Value;
                    atlas.slicedSprites[sliceIndex - 1].sprite.index = sliceIndex - 1;
                }
                else
                {
                    atlas.simpleSprites[selectedGridIndex] = atlas.simpleSprites[selectedGridIndex - 1];
                    atlas.simpleSprites[selectedGridIndex].index = selectedGridIndex;

                    atlas.simpleSprites[selectedGridIndex - 1] = atlasSprite;
                    atlas.simpleSprites[selectedGridIndex - 1].index = selectedGridIndex - 1;
                }
                selectedGridIndex--;
                
                EditorUtility.SetDirty(atlas);
                AssetDatabase.SaveAssets();
            }
        }
        Handles.EndGUI();
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
            previewClip = clip;

            if (previewClip.keyframeEndIndex - previewClip.keyframeStartIndex <= 0) return;

            previewSprite = AtlasRendering.GetNextKeyframeIndexEditor(atlas, previewClip, ref editorTimeDelta, ref curFrameIndex, ref prevFrameIndex).sprite;
            if (curFrameIndex != prevFrameIndex)
            {
                Repaint();
            }
        }
    }
    private void DrawAtlasOverview()
    {
        if (atlas == null || atlas.texture == null) return;

        float atlasSize = overviewWidth * position.width;

        Rect atlasRect = GUILayoutUtility.GetRect( atlasSize, atlasSize, GUILayout.Width(atlasSize), GUILayout.Height(atlasSize));

        float texAspect = (float)atlas.texture.width / atlas.texture.height;

        if (texAspect > 1f)
        {
            float height = atlasRect.width / texAspect;
            atlasRect.y += (atlasRect.height - height) * 0.5f;
            atlasRect.height = height;
        }
        else
        {
            float width = atlasRect.height * texAspect;
            atlasRect.x += (atlasRect.width - width) * 0.5f;
            atlasRect.width = width;
        }

        GUI.DrawTexture(atlasRect, atlas.texture, ScaleMode.StretchToFill, true);
        Handles.BeginGUI();
        {
            int index = 0;
            for (int i = 0; i < atlas.simpleSprites.Length; i++)
            {
                DrawSpriteRect( atlasRect, atlas.simpleSprites[i], selectedGridIndex == index ? selectedColor : unselectedColor);
                index++;
            }

            for (int i = 0; i < atlas.motionSprites.Length; i++)
            {
                DrawSpriteRect(atlasRect, atlas.motionSprites[i].sprite, selectedGridIndex == index ? selectedColor : unselectedColor);
                index++;
            }

            for (int i = 0; i < atlas.slicedSprites.Length; i++)
            {
                DrawSpriteRect(atlasRect, atlas.slicedSprites[i].sprite, selectedGridIndex == index? selectedColor : unselectedColor );
                index++;
            }
        }
        Handles.EndGUI();
    }
    void DrawSpriteRect(Rect atlasRect, SimpleSprite sprite, Color color)
    {
        Rect r = new Rect(
            atlasRect.x + sprite.uvSizeAndPos.z * atlasRect.width,
            atlasRect.y + (1f - sprite.uvSizeAndPos.w - sprite.uvSizeAndPos.y) * atlasRect.height,
            sprite.uvSizeAndPos.x * atlasRect.width,
            sprite.uvSizeAndPos.y * atlasRect.height
        );
        Handles.color = color;
        Handles.DrawSolidRectangleWithOutline(r, Color.clear, Handles.color);
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        AtlasFactory atlasFactory = Resources.FindObjectsOfTypeAll<AtlasFactory>().FirstOrDefault();

        if (atlasFactory != null && atlasFactory.atlas != null)
        {
            atlasFactory.atlas.UpdateClipDictionary();
            atlasFactory.OnAtlasChange();
            atlasFactory.Repaint();
        }

    }

}

//    ))
//  .-#-------.
// /___________\ My cozy little house protected from the monstrosity above. VISITS: 1
//  | [] _ [] |
//  |   |*|   |
//  |   | |   |
//  ^^^^^^^^^^^