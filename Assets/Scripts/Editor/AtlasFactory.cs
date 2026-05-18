using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Atlas;

public class AtlasFactory : EditorWindow
{
    static Color selectedColor = Color.red;
    static Color unselectedColor = new Color(0, 0.2f, 0.2f);

    const float HEADER_COL_HEIGHT = 20;
    const float PADDING = 100;
    const float MARKER_SIZE = 4f;

    static GUILayoutOption[] headerGUI;
    static GUILayoutOption[] horizontalGUI;
    static GUILayoutOption[] inputGUI;
    static GUILayoutOption[] buttonGUI;

    
    static Vector2 scroll;
    
    public AtlasSO atlas;
    public Texture2D markerTexture;
    public bool generateSprites;

    public float cellSize;
    public int selectedIndex;
    

    //Preview
    private int selectedMotionIndex;

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

    private int[] motionSelections;
    private int[] holdTimeValues;
    private float editorTimeDelta;
    private double lastEditorTime;
    private int curFrameIndex;
    private int prevFrameIndex;
    private bool startMotion;
    private int simpleSpritesFound;
    private int motionSpritesFound;
    private int sliceSpritesFound;

    const float padding = 50f;
    const float GUIHeaderColoumnWidth = 300;


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
        horizontalGUI = new GUILayoutOption[] { GUILayout.Height(HEADER_COL_HEIGHT) };

        spriteDirectoryLayout = new GUILayoutOption[] { GUILayout.Width(directoryWidth * position.width) };
        previewLayout = new GUILayoutOption[] { GUILayout.Width(previewWidth * position.width) };
        overviewLayout = new GUILayoutOption[] { GUILayout.Width(overviewWidth * position.width) };
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
            atlas = (AtlasSO)EditorGUILayout.ObjectField("Atlas", atlas, typeof(AtlasSO), allowSceneObjects: false, inputGUI);
            markerTexture = (Texture2D)EditorGUILayout.ObjectField("Marker Texture", markerTexture, typeof(Texture2D), allowSceneObjects: false, inputGUI);
            generateSprites = GUILayout.Button("Generate Sprites", buttonGUI);

        }
        EditorGUILayout.EndHorizontal();
        
        if (atlas == null || atlas.texture == null) return;

        if (generateSprites)
        {
            simpleSpritesFound = 0;
            motionSpritesFound = 0;
            sliceSpritesFound = 0;

            previewClip = default;
            selectedIndex = 0;
            GenerateSprites();
            atlas.UpdateClipDictionary();
            GetMotionData();

            flip = 1;
            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
    private void DrawSpriteDirectorySection()
    {
        if (atlas == null) return;

        EditorGUILayout.BeginVertical(spriteDirectoryLayout);
        {
            cellSize = EditorGUILayout.FloatField("Cell Size", cellSize, headerGUI);
            scroll = EditorGUILayout.BeginScrollView(scroll);

            float finalCellSize = cellSize + padding;
            int columns = Mathf.CeilToInt(((directoryWidth * position.width) - finalCellSize) / finalCellSize);
            columns = Mathf.Max(1, columns);
            int gridIndex = 0;

            for (int i = 0; i < atlas.simpleSprites.Length; i++)
            {
                if (i % columns == 0) EditorGUILayout.BeginHorizontal();

                DrawAtlasSprite(atlas.simpleSprites[i], gridIndex);
                gridIndex++;
                if (i % columns == columns - 1 || i == atlas.simpleSprites.Length - 1) EditorGUILayout.EndHorizontal();
            }

            if (motionSelections != null && motionSelections.Length > 0)
            {
                for (int i = 0; i < atlas.motionSprites.Length; i++)
                {
                    if (i % columns == 0) EditorGUILayout.BeginHorizontal();

                    DrawAtlasSprite(atlas.motionSprites[i].sprite, gridIndex, motionSpriteNullable: atlas.motionSprites[i]);
                    gridIndex++;

                    if (i % columns == columns - 1 || i == atlas.motionSprites.Length - 1) EditorGUILayout.EndHorizontal();
                }
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
            if (MotionEnumDictionary.TryGetValue(atlas.entityMotionType, out Type enumType))
            {
                Enum motionEnum = (Enum)Enum.ToObject(enumType, selectedMotionIndex);
                EditorGUILayout.LabelField($"Preview Motion: {motionEnum}", headerGUI);
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
                EditorGUI.BeginChangeCheck();

                previewClip.clipType = (ClipType)EditorGUILayout.EnumPopup("Clip Type", previewClip.clipType, headerGUI);

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

            if (GUILayout.Button("Play", headerGUI))
            {
                startMotion = true;
                editorTimeDelta = 0;
                curFrameIndex = 0;
            }
            if (GUILayout.Button("Stop", headerGUI))
            {
                startMotion = false;
            }
            if (GUILayout.Button("Flip", headerGUI))
            {
                flip = flip == 1 ? -1 : 1;
            }

            DrawPreview();
        }
        EditorGUILayout.EndVertical();
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

        Rect containterRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

        if (Event.current.type != EventType.Repaint) return;

        float spritePixelWidth = previewSprite.uvSizeAndPos.x * atlas.texture.width;
        float spritePixelHeight = previewSprite.uvSizeAndPos.y * atlas.texture.height;

        float scale = Mathf.Min(containterRect.width / spritePixelWidth, containterRect.height / spritePixelHeight);

        float drawWidth = spritePixelWidth * scale;
        float drawHeight = spritePixelHeight * scale;

        Vector2 pivot = new Vector2(containterRect.center.x, containterRect.yMin);
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

        GUI.DrawTextureWithTexCoords(previewRect, atlas.texture, uvRect);

        Handles.BeginGUI();
        {
            Handles.DrawSolidRectangleWithOutline(containterRect, Color.clear, selectedColor);
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
        SetPreviousKeyframeSprites();
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
                SimpleSprite oldSprite = atlas.motionSprites[j].sprite;
                Rect oldRect = new Rect(oldSprite.uvSizeAndPos.z, oldSprite.uvSizeAndPos.w, oldSprite.uvSizeAndPos.x, oldSprite.uvSizeAndPos.y);
                if (oldRect.Overlaps(newRect))
                {
                    atlas.motionSprites[j] = newMotionSprites[i];
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
    private void SetPreviousKeyframeSprites()
    {
        for (int i = 0; i < atlas.motionSprites.Length; i++)
        {
            for (int j = 0; j < atlas.clips.Length; j++)
            {
                for (int k = 0; k < atlas.clips[j].keyFrames.Length; k++)
                {
                    if (atlas.clips[j].keyFrames[k].motionSprite.sprite.index == i)
                    {
                        atlas.clips[j].keyFrames[k].motionSprite = atlas.motionSprites[i];
                    }
                }
            }
        }
    }
    private void GetMotionData()
    {
        motionSelections = new int[atlas.motionSprites.Length];
        holdTimeValues = new int[atlas.motionSprites.Length];
        for (int i = 0; i < atlas.motionSprites.Length; i++)
        {
            holdTimeValues[i] = 10;

            for (int j = 0; j < atlas.clips.Length; j++)
            {
                AtlasClip clip = atlas.clips[j];

                for (int k = 0; k < clip.keyFrames.Length; k++)
                {
                    AtlasKeyframe keyframe = clip.keyFrames[k];

                    if (keyframe.motionSprite.sprite.index == i)
                    {
                        motionSelections[i] = clip.motionIndex;
                        holdTimeValues[i] = keyframe.holdTime;
                        break;
                    }
                }
            }
        }
    }
    private void DrawAtlasSprite(SimpleSprite atlasSprite, int gridIndex, MotionSprite? motionSpriteNullable = null, SliceSprite? slicedSpriteNullable = null)
    {
        Rect gridRect = GUILayoutUtility.GetRect(cellSize + padding, cellSize + padding, GUILayout.ExpandWidth(false));
        gridRect = new Rect(gridRect.x + padding * 0.5f, gridRect.y + padding * 0.5f, cellSize, cellSize);

        Vector2 uvSize = new Vector2(atlasSprite.uvSizeAndPos.x, atlasSprite.uvSizeAndPos.y);
        Vector2 uvPos = new Vector2(atlasSprite.uvSizeAndPos.z, atlasSprite.uvSizeAndPos.w);
        Rect uvRect = new Rect(uvPos, uvSize);

        float spritePixelWidth = atlasSprite.uvSizeAndPos.x * atlas.texture.width;
        float spritePixelHeight = atlasSprite.uvSizeAndPos.y * atlas.texture.height;

        float aspectRatio = Mathf.Min(gridRect.width / spritePixelWidth, gridRect.height / spritePixelHeight);
        Vector2 drawSize = new Vector2(spritePixelWidth * aspectRatio, spritePixelHeight * aspectRatio);
        Rect spriteRect = new Rect(gridRect.center.x - drawSize.x * 0.5f, gridRect.center.y - drawSize.y * 0.5f, drawSize.x, drawSize.y);

        GUI.DrawTextureWithTexCoords(spriteRect, atlas.texture, uvRect, alphaBlend: true);

        Vector2 pivotPos = new Vector2(Mathf.Lerp(spriteRect.xMin, spriteRect.xMax, atlasSprite.uvPivot.x),Mathf.Lerp(spriteRect.yMax, spriteRect.yMin, atlasSprite.uvPivot.y));
        Rect pivotRect = new Rect(pivotPos - Vector2.one * MARKER_SIZE, Vector2.one * (MARKER_SIZE * 2));

        Handles.BeginGUI();
        Handles.DrawSolidRectangleWithOutline(spriteRect, Color.clear, Color.grey);
        Handles.DrawSolidRectangleWithOutline(pivotRect, Color.clear, atlas.pivotColor);

        GUI.depth = 1;
        Event e = Event.current;

        if (spriteRect.Contains(e.mousePosition) && selectedIndex != gridIndex)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                selectedIndex = gridIndex;
                previewSprite = atlasSprite;
                curFrameIndex = 0;
                e.Use();
            }
        }

        if (selectedIndex == gridIndex)
        {
            Handles.DrawSolidRectangleWithOutline(spriteRect, Color.clear, Color.blueViolet);
        }
        Vector2 rectSize = new Vector2(cellSize * 0.05f, cellSize * 0.05f);
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
                        Vector2 markerPixelPos = marker.objectPos * PIXELS_PER_UNIT;
                        Vector2 markerNormalized = new Vector2(markerPixelPos.x / spritePixelWidth, markerPixelPos.y / spritePixelHeight);
                        markerNormalized.x += atlasSprite.uvPivot.x;
                        markerNormalized.y += atlasSprite.uvPivot.y;
                        Vector2 markerRectPos =  new Vector2(Mathf.Lerp(spriteRect.xMin, spriteRect.xMax, markerNormalized.x), Mathf.Lerp(spriteRect.yMax, spriteRect.yMin, markerNormalized.y));
                        Rect markerRect = new Rect(markerRectPos - Vector2.one * MARKER_SIZE, Vector2.one * (MARKER_SIZE * 2));
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

            //Sprite Index Label
            Vector2 indexPos = new Vector2(spriteRect.xMin, spriteRect.yMin);
            GUIStyle indexStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(indexPos, new Vector2(20, 20)), atlasSprite.index.ToString(), indexStyle);

            EditorGUI.BeginChangeCheck();

            int prevMotion = motionSelections[motionSprite.sprite.index];

            if (MotionEnumDictionary.TryGetValue(atlas.entityMotionType, out Type enumType))
            {
                Enum currentEnum = (Enum)Enum.ToObject(enumType, prevMotion);
                Enum nextEnum = EditorGUI.EnumPopup(animTypeRect, currentEnum);
                motionSelections[motionSprite.sprite.index] = Convert.ToInt32(nextEnum);
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

                    if (selectedClipIndex == -1 && clip.motionIndex == motionSelections[motionSprite.sprite.index])
                    {
                        for (int j = 0; j < clip.keyFrames.Length; j++)
                        {
                            AtlasKeyframe keyframe = clip.keyFrames[j];
                            if (keyframe.motionSprite.sprite.index == motionSprite.sprite.index)
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
                    

                    if (clip.motionIndex == prevMotion)
                    {
                        prevClipIndex = i;
                    }
                }

                if (selectedClipIndex == -1) // Creating new clip
                {
                    if (motionSelections[motionSprite.sprite.index] > 0 && selectedHoldTimeValue > -1)
                    {
                        List<AtlasClip> clipsList = atlas.clips.ToList();

                        AtlasClip newClip = new AtlasClip();

                        newClip.motionIndex = motionSelections[motionSprite.sprite.index];

                        if (MotionEnumDictionary.TryGetValue(atlas.entityMotionType, out Type enumTypeForName))
                        {
                            Enum motionEnum = (Enum)Enum.ToObject(enumTypeForName, motionSelections[motionSprite.sprite.index]);
                            newClip.clipName = motionEnum.ToString();
                        }

                        newClip.keyFrames = new AtlasKeyframe[1];
                        newClip.keyFrames[0].motionSprite = motionSprite;
                        newClip.keyFrames[0].holdTime = selectedHoldTimeValue;
                        clipsList.Add(newClip);
                        atlas.clips = clipsList.ToArray();
                    }
                }
                else if (!foundKeyframe && selectedHoldTimeValue > -1) // Creating new frame
                {
                    List<AtlasKeyframe> selectedClipKeyframesList = atlas.clips[selectedClipIndex].keyFrames.ToList();
                    AtlasKeyframe newKeyframe = new AtlasKeyframe();
                    newKeyframe.motionSprite = motionSprite;
                    newKeyframe.holdTime = selectedHoldTimeValue;
                    selectedClipKeyframesList.Add(newKeyframe);
                    atlas.clips[selectedClipIndex].keyFrames = selectedClipKeyframesList.ToArray();
                }

                if (prevClipIndex != -1 && prevClipIndex != selectedClipIndex)
                {
                    AtlasClip prevClip = atlas.clips[prevClipIndex];
                    List<AtlasKeyframe> keyframesList = prevClip.keyFrames.ToList();
                    for (int i = 0; i < keyframesList.Count; i++)
                    {
                        if (keyframesList[i].motionSprite.sprite.index == motionSprite.sprite.index)
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
                    else // remove keyframe
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
        else if (slicedSpriteNullable.HasValue)
        {
            if (selectedIndex == gridIndex)
            {
                SliceSprite slicedSprite = slicedSpriteNullable.Value;
                Handles.color = Color.coral;
                float leftPos = (slicedSprite.slice.x * spriteRect.width) + spriteRect.xMin;
                float rightPos = (slicedSprite.slice.y * spriteRect.width) + spriteRect.xMin;
                float bottomPos = spriteRect.yMax - (slicedSprite.slice.z * spriteRect.height);
                float topPos = spriteRect.yMax - (slicedSprite.slice.w * spriteRect.height);

                Handles.DrawLine(new Vector3(leftPos, spriteRect.yMin), new Vector3(leftPos, spriteRect.yMax));
                Handles.DrawLine(new Vector3(rightPos, spriteRect.yMin), new Vector3(rightPos, spriteRect.yMax));

                Handles.DrawLine(new Vector3(spriteRect.xMin, bottomPos), new Vector3(spriteRect.xMax, bottomPos));
                Handles.DrawLine(new Vector3(spriteRect.xMin, topPos), new Vector3(spriteRect.xMax, topPos));
            }
        }
        else
        {
            if (selectedIndex == gridIndex)
            {
                for (int i = 0; i < simplePivots.Length; i++)
                {
                    Vector2 curPivot = simplePivots[i];
                    Vector2 rectOffset = spriteRect.size * new Vector2(curPivot.x, 1 - curPivot.y);
                    Vector2 buttonOffset = rectSize * new Vector2(1 - curPivot.x, curPivot.y);

                    Rect buttonRect = new Rect(spriteRect.min + rectOffset - buttonOffset, rectSize);

                    GUI.depth = 0;
                    if (GUI.Button(buttonRect, GUIContent.none))
                    {
                        atlas.simpleSprites[selectedIndex].uvPivot = curPivot;
                        EditorUtility.SetDirty(atlas);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.LeftArrow)
        {
            if (selectedIndex == 0) return;
            selectedIndex--;
            if (motionSpriteNullable.HasValue)
            {
                int motionIndex = selectedIndex - atlas.simpleSprites.Length;
                previewSprite = atlas.motionSprites[motionIndex].sprite;
            }
            else if (slicedSpriteNullable.HasValue)
            {
                int sliceIndex = selectedIndex - atlas.simpleSprites.Length - atlas.motionSprites.Length;
                previewSprite = atlas.motionSprites[sliceIndex].sprite;
            }
            else
            {
               previewSprite = atlas.simpleSprites[selectedIndex];
            }
            curFrameIndex = 0;
            e.Use();
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.RightArrow)
        {
            if (motionSpriteNullable.HasValue)
            {
                int motionIndex = selectedIndex - atlas.simpleSprites.Length;
                if (motionIndex == atlas.motionSprites.Length - 1) return;
                selectedIndex++;
                motionIndex = selectedIndex - atlas.simpleSprites.Length;

                previewSprite = atlas.motionSprites[motionIndex].sprite;
            }
            else if (slicedSpriteNullable.HasValue)
            {
                int sliceIndex = selectedIndex - atlas.simpleSprites.Length - atlas.motionSprites.Length;
                if (sliceIndex == atlas.slicedSprites.Length - 1) return;
                selectedIndex++;
                sliceIndex = selectedIndex - atlas.simpleSprites.Length - atlas.motionSprites.Length;

                previewSprite = atlas.motionSprites[sliceIndex].sprite;
            }
            else
            {
                if (selectedIndex == atlas.simpleSprites.Length - 1) return;
                selectedIndex++;

                previewSprite = atlas.simpleSprites[selectedIndex];
            }
            curFrameIndex = 0;
            e.Use();
        }

        if (selectedIndex == gridIndex)
        {
            float quarterWidth = spriteRect.width * 0.25f;
            Vector2 shiftLeftPos = new Vector2(spriteRect.center.x - quarterWidth, spriteRect.center.y);
            Vector2 shiftRightPos = new Vector2(spriteRect.center.x + quarterWidth, spriteRect.center.y);
            Rect shiftLeftRect = new Rect(shiftLeftPos, rectSize);
            Rect shiftRightRect = new Rect(shiftRightPos, rectSize);

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
                    int motionIndex = selectedIndex - atlas.simpleSprites.Length;
                    if (motionIndex == atlas.motionSprites.Length - 1) return;

                    atlas.motionSprites[motionIndex] = atlas.motionSprites[motionIndex + 1];
                    atlas.motionSprites[motionIndex].sprite.index = motionIndex;

                    atlas.motionSprites[motionIndex + 1] = motionSpriteNullable.Value;
                    atlas.motionSprites[motionIndex + 1].sprite.index = motionIndex + 1;

                    selectedIndex++;
                }
                else if (slicedSpriteNullable.HasValue)
                {
                    int sliceIndex = selectedIndex - atlas.simpleSprites.Length - atlas.motionSprites.Length;
                    if (sliceIndex == atlas.slicedSprites.Length - 1) return;

                    atlas.slicedSprites[sliceIndex] = atlas.slicedSprites[sliceIndex + 1];
                    atlas.slicedSprites[sliceIndex].sprite.index = sliceIndex;

                    atlas.slicedSprites[sliceIndex + 1] = slicedSpriteNullable.Value;
                    atlas.slicedSprites[sliceIndex + 1].sprite.index = sliceIndex + 1;

                    selectedIndex++;
                }
                else
                {
                    if (selectedIndex == atlas.simpleSprites.Length - 1) return;

                    atlas.simpleSprites[selectedIndex] = atlas.simpleSprites[selectedIndex + 1];
                    atlas.simpleSprites[selectedIndex].index = selectedIndex;

                    atlas.simpleSprites[selectedIndex + 1] = atlasSprite;
                    atlas.simpleSprites[selectedIndex + 1].index = selectedIndex + 1;

                    selectedIndex++;
                }
                EditorUtility.SetDirty(atlas);
                AssetDatabase.SaveAssets();
            }
            void HandleShiftLeft()
            {
                if (selectedIndex == 0) return;
                if (motionSpriteNullable.HasValue)
                {
                    int motionIndex = selectedIndex - atlas.simpleSprites.Length;

                    atlas.motionSprites[motionIndex] = atlas.motionSprites[motionIndex - 1];
                    atlas.motionSprites[motionIndex].sprite.index = motionIndex;


                    atlas.motionSprites[motionIndex - 1] = motionSpriteNullable.Value;
                    atlas.motionSprites[motionIndex - 1].sprite.index = motionIndex - 1;
                }
                else if (slicedSpriteNullable.HasValue)
                {
                    int sliceIndex = selectedIndex - atlas.simpleSprites.Length - atlas.motionSprites.Length;

                    atlas.slicedSprites[sliceIndex] = atlas.slicedSprites[sliceIndex - 1];
                    atlas.slicedSprites[sliceIndex].sprite.index = sliceIndex;

                    atlas.slicedSprites[sliceIndex - 1] = slicedSpriteNullable.Value;
                    atlas.slicedSprites[sliceIndex - 1].sprite.index = sliceIndex - 1;
                }
                else
                {
                    atlas.simpleSprites[selectedIndex] = atlas.simpleSprites[selectedIndex - 1];
                    atlas.simpleSprites[selectedIndex].index = selectedIndex;

                    atlas.simpleSprites[selectedIndex - 1] = atlasSprite;
                    atlas.simpleSprites[selectedIndex - 1].index = selectedIndex - 1;
                }
                selectedIndex--;
                EditorUtility.SetDirty(atlas);
                AssetDatabase.SaveAssets();
            }
        }
        Handles.EndGUI();
    }
    private void HandleShiftRight()
    {

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

            previewSprite = AtlasRendering.GetNextKeyframeSpriteEditor(ref previewClip, ref editorTimeDelta, ref curFrameIndex, ref prevFrameIndex);
            //previewSprite = previewClip.GetNextSprite(ref editorTimeDelta, ref curFrameIndex, ref prevFrameIndex); TODO // Make an editor animation function
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
                DrawSpriteRect( atlasRect, atlas.simpleSprites[i], selectedIndex == index ? selectedColor : unselectedColor);
                index++;
            }

            for (int i = 0; i < atlas.motionSprites.Length; i++)
            {
                DrawSpriteRect(atlasRect, atlas.motionSprites[i].sprite, selectedIndex == index ? selectedColor : unselectedColor);
                index++;
            }

            for (int i = 0; i < atlas.slicedSprites.Length; i++)
            {
                DrawSpriteRect(atlasRect, atlas.slicedSprites[i].sprite, selectedIndex == index? selectedColor : unselectedColor );
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

        Handles.DrawSolidRectangleWithOutline(r, Color.clear, color);
    }

}

//    ))
//  .-#-------.
// /___________\ My cozy little house protected from the monstrosity above. VISITS: 1
//  | [] _ [] |
//  |   |*|   |
//  |   | |   |
//  ^^^^^^^^^^^