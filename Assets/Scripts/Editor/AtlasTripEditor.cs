using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static AtlasSpawn;
using System;

public class AtlasTripEditor : EditorWindow
{
    const float HEADER_COL_HEIGHT = 20;
    const float PADDING = 100;
    const float STATION_RECT_Y_OFFSET = 25;

    static GUILayoutOption[] headerGUI;
    static GUILayoutOption[] horizontalGUI;
    static GUILayoutOption[] inputGUI;
    static GUILayoutOption[] buttonGUI;

    static GUIStyle stationLabel;
    static GUIStyle depthLabel;
    static GUIStyle particleAvailCountLabel;

    static Rect fullRect;
    static Rect scrollViewRect;

    static Rect graphRect;

    static Vector2 graphPos;
    static Vector2 graphSize;
    static Vector2 scroll;

    static int[] particleAvailCount_array;

    static int totalTicketChecks;
    static int selectedPosDataIndex;
    static int selectedParticlAtlasIndex;
    static int dragOffsetWidth;
    static int dragOffsetHeight;
    static int dragOffsetStartX;
    static int dragOffsetStartY;

    static float stationRectPosY;
    static float graphRowHeight;

    private bool isAdjustingMetersStart;
    private bool isAdjustingDepthSize;

    public TripSO trip;
    public ParticleAtlas selectedParticleAtlas;
    public int selectedSpriteIndex;
    
    public int selectedParticleCount;

    public ParticleWidthType selectedScrollSpriteType;

    [MenuItem("Tools/Atlas Trip Editor")]
    private static void Open()
    {
        AtlasTripEditor tripEditor = GetWindow<AtlasTripEditor>("Atlas Trip Editor");
        inputGUI = new GUILayoutOption[] { GUILayout.Width(300) };
        buttonGUI = new GUILayoutOption[] { GUILayout.Width(150) };
        tripEditor.Init();
    }
    private void OnGUI()
    {
        Init();
        GUISetUp();
        DrawFirstHeader();
        DrawSecondHeader();
        DrawThirdHeader();
        DrawGraph();
    }
    public void Init()
    {
        fullRect = new Rect(0, 0, position.width, 10000);

        graphPos = new Vector2(PADDING, 100 + HEADER_COL_HEIGHT);
        graphSize = new Vector2(fullRect.width - PADDING * 2, fullRect.height);

        fullRect.height += PADDING * 4;

        scrollViewRect = new Rect(0, graphPos.y + 50, position.width, position.height);
        graphRect = new Rect(graphPos, graphSize);
        
        stationRectPosY = graphPos.y - STATION_RECT_Y_OFFSET;
        graphRowHeight = graphRect.height / FAR_CLIP;

        horizontalGUI = new GUILayoutOption[] { GUILayout.Height(HEADER_COL_HEIGHT) };

        stationLabel = new GUIStyle();
        stationLabel.alignment = TextAnchor.MiddleCenter;
        stationLabel.normal.textColor = Color.white;
        stationLabel.fontSize = 16;
        stationLabel.fontStyle = FontStyle.Bold;

        depthLabel = new GUIStyle();
        depthLabel.alignment = TextAnchor.MiddleRight;
        depthLabel.normal.textColor = Color.white;
        depthLabel.fontSize = 16;

        particleAvailCountLabel = new GUIStyle();
        particleAvailCountLabel.alignment = TextAnchor.LowerLeft;
        particleAvailCountLabel.normal.textColor = Color.white;
        particleAvailCountLabel.fontSize = 16;
        particleAvailCountLabel.fontStyle = FontStyle.Normal;
    }
    private void GUISetUp()
    {
        if (trip == null) return;
        totalTicketChecks = 0;
        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO station = trip.stationsDataArray[i];
            totalTicketChecks += station.ticketsToCheckBeforeSpawn;
        }

        particleAvailCount_array = new int[totalTicketChecks];

        Array.Fill(particleAvailCount_array, ZONE_PARTICLE_COUNT);

        for (int i = 0; i < particleAvailCount_array.Length; i++)
        {
            for (int j = 0; j < trip.particleAtlasArray.Length; j++)
            {
                ParticleAtlas particleAtlas = trip.particleAtlasArray[j];

                for (int k = 0; k < particleAtlas.posData.Length; k++)
                {
                    ParticlePosData posData = particleAtlas.posData[k];

                    if (posData.ticketCheckStart <= i && posData.ticketCheckEnd > i)
                    {
                        particleAvailCount_array[i] -= posData.particleCount;
                    }
                }
            }
        }
    }
    private void DrawFirstHeader()
    {
        EditorGUILayout.BeginHorizontal(horizontalGUI);
        {
            EditorGUI.BeginChangeCheck();            
            trip = (TripSO)EditorGUILayout.ObjectField("Trip", trip, typeof(TripSO), allowSceneObjects: false, inputGUI);
        }
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            if (trip == null) return;
            totalTicketChecks = 0;
            for (int i = 0; i < trip.stationsDataArray.Length; i++)
            {
                StationSO station = trip.stationsDataArray[i];
                totalTicketChecks += station.ticketsToCheckBeforeSpawn;
            }
        }
    }
    private void DrawSecondHeader()
    {
        if (trip == null) return;

        EditorGUILayout.BeginHorizontal(horizontalGUI);
        {
            EditorGUILayout.LabelField("Atlas Options");
            string label = selectedParticleAtlas == null ? "Select Atlas" : selectedParticleAtlas.name;
            GUIContent particleAtlasSelectedContent = new GUIContent(label);

            bool selectedDropDown = EditorGUILayout.DropdownButton(particleAtlasSelectedContent, FocusType.Passive, headerGUI);

            if (selectedDropDown)
            {
                GenericMenu particleAtlasMenu = new GenericMenu();

                for (int i = 0; i < trip.particleAtlasArray.Length; i++)
                {
                    ParticleAtlas particleAtlas = trip.particleAtlasArray[i];
                    GUIContent particleAtlasContent = new GUIContent(particleAtlas.name);
                    GenericMenu.MenuFunction menuFunc = () => { selectedParticleAtlas = particleAtlas; };

                    particleAtlasMenu.AddItem(particleAtlasContent, selectedParticleAtlas == particleAtlas, menuFunc);
                    particleAtlasMenu.ShowAsContext();
                }
            }

            GUILayout.Space(10);

            if (selectedParticleAtlas != null)
            {
                bool addParticleData = GUILayout.Button("Add Particle", buttonGUI);
                if (addParticleData)
                {
                    List<ParticlePosData> particleDataList = selectedParticleAtlas.posData.ToList();
                    ParticlePosData newParticleData = new ParticlePosData();
                    newParticleData.ticketCheckStart = 0;
                    newParticleData.ticketCheckEnd = 1;
                    newParticleData.depth = 1;
                    newParticleData.depthSize = 1;
                    particleDataList.Add(newParticleData);
                    selectedParticleAtlas.posData = particleDataList.ToArray();
                }

                bool removeParticleData = GUILayout.Button("Remove Particle", buttonGUI);
                if (removeParticleData)
                {
                    List<ParticlePosData> particleDataList = selectedParticleAtlas.posData.ToList();
                    particleDataList.RemoveAt(selectedPosDataIndex);
                    selectedParticleAtlas.posData = particleDataList.ToArray();
                    selectedPosDataIndex = Mathf.Clamp(selectedPosDataIndex - 1, 0, selectedParticleAtlas.posData.Length - 1);
                }
            }

            GUILayout.Space(10);
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();
    }
    private void DrawThirdHeader()
    {
        if (trip == null || selectedParticleAtlas == null || selectedParticleAtlas.posData.Length == 0) return;

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginHorizontal(horizontalGUI);
        {
            ParticlePosData selectedPosData = selectedParticleAtlas.posData[selectedPosDataIndex];
            switch(selectedParticleAtlas.particleType)
            {
                case ParticleType.Zone:
                {
                    EditorGUILayout.LabelField("Particle Count");
                    selectedParticleCount = EditorGUILayout.IntSlider(selectedParticleCount, 1, particleAvailCount_array[selectedPosData.ticketCheckStart], headerGUI);
                }
                break;

                case ParticleType.Scroll:
                {
                    EditorGUILayout.LabelField("Sprite Index");
                    selectedParticleCount = 2;
                    selectedSpriteIndex = EditorGUILayout.IntSlider(selectedSpriteIndex, 0, selectedParticleAtlas.spriteCount, headerGUI);
                }
                break;
            }
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            SaveTrip();
        }
    }
    private void DrawGraph()
    {
        if (trip == null || trip.stationsDataArray.Length == 0) return;

        Event e = Event.current;

        Vector2 vertLineTop = new Vector2();
        Vector2 vertLineBottom = new Vector2();
        Vector2 horLineLeft = new Vector2();
        Vector2 horLineRight = new Vector2();

        vertLineTop.y = graphRect.yMin;
        vertLineBottom.y = graphRect.yMax;
        horLineLeft.x = graphRect.xMin;
        horLineRight.x = graphRect.xMax;

        float totalTicketCheckXIncrement = (1 / (float)totalTicketChecks) * graphSize.x;
        EditorGUILayout.BeginVertical();
        {
            Handles.BeginGUI();
            {
                int stationIndex = 0;
                int ticketChecks = 0;
                for (int i = 0; i <= totalTicketChecks; i++)
                {
                    float posX = graphPos.x + i * totalTicketCheckXIncrement;

                    if (i < totalTicketChecks)
                    {
                        string particleAvailCount_string = particleAvailCount_array[i].ToString();
                        Vector2 particleAvailLabelSize = particleAvailCountLabel.CalcSize(new GUIContent(particleAvailCount_string));
                        Rect particleAvailLabelRect = new Rect(posX - (particleAvailLabelSize.x * 0.5f), vertLineTop.y + particleAvailLabelSize.y, particleAvailLabelSize.x, particleAvailLabelSize.y);
                        GUI.Label(particleAvailLabelRect, particleAvailCount_string, particleAvailCountLabel);
                    }

                    StationSO station = trip.stationsDataArray[stationIndex];
                    if (station.ticketsToCheckBeforeSpawn == ticketChecks)
                    {

                        Vector2 stationLabelSize = stationLabel.CalcSize(new GUIContent(station.stationName));
                        Rect stationRect = new Rect(posX - (stationLabelSize.x * 0.5f), stationRectPosY, stationLabelSize.x, stationLabelSize.y);
                        Handles.DrawSolidRectangleWithOutline(stationRect, Color.black, Color.white);

                        Rect stationLabelRect = new Rect(stationRect.min, stationLabelSize);
                        GUI.Label(stationLabelRect, station.stationName, stationLabel);
                        
                        stationIndex++;
                        ticketChecks = 0;
                    }
                    ticketChecks++;
                }
            }
            Handles.EndGUI();

        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        {
            scroll = GUI.BeginScrollView(scrollViewRect, scroll, fullRect);
            {
                Handles.BeginGUI();
                {
                    Vector2 trainRectPos = new Vector2(graphRect.xMin, graphRect.yMin + ((MAIN_MIN - 1) * graphRowHeight));
                    Vector2 trainRectSize = new Vector2(graphRect.size.x,(MAIN_MAX - MAIN_MIN + 1) * graphRowHeight);
                    Rect trainRect = new Rect(trainRectPos, trainRectSize);
                    Handles.DrawSolidRectangleWithOutline(trainRect, Color.darkRed, Color.clear);

                    for (int i = 0; i <= totalTicketChecks;i++)
                    {
                        float posX = graphPos.x + i * totalTicketCheckXIncrement;
                        vertLineTop.x = posX;
                        vertLineBottom.x = posX;
                        Handles.DrawLine(vertLineTop, vertLineBottom);
                    }


                    for (int i = 0; i <= FAR_CLIP; i++)
                    {
                        float posY = graphRect.yMin + (i * graphRowHeight);
                        horLineLeft.y = posY;
                        horLineRight.y = posY;
                        Handles.DrawLine(horLineLeft, horLineRight);

                        if (i == 0) continue;

                        string depthString = (i).ToString();
                        
                        if (i == TRAIN_LINE_DEPTH)
                        {
                            depthString = "LINES";
                        }
                        else if (i == TRAIN_TRACKS_DEPTH)
                        {
                            depthString = "TRACKS";
                        }
                        Vector2 labelSize = depthLabel.CalcSize(new GUIContent(depthString));
                        Vector2 depthLabelPos = new Vector2(graphRect.xMin -  5, posY - (graphRowHeight - (labelSize.y * 0.5f)));
                        Handles.Label(depthLabelPos, depthString, depthLabel);
                    }


                    for (int i = 0; i < trip.particleAtlasArray.Length; i++)
                    {
                        ParticleAtlas particleAtlas = trip.particleAtlasArray[i];

                        for (int j = 0; j < particleAtlas.posData.Length; j++)
                        {
                            ParticlePosData posData = particleAtlas.posData[j];

                            float ticketCheckSize = posData.ticketCheckEnd - posData.ticketCheckStart;
                            float barWidth = ticketCheckSize * totalTicketCheckXIncrement;
                            float barHeight = Mathf.Max(graphRowHeight * posData.depthSize, graphRowHeight);
                            float barX = graphRect.xMin + posData.ticketCheckStart * totalTicketCheckXIncrement;


                            float graphDepth = (posData.depth - 1);
                            float barY = graphRect.yMin + graphDepth * graphRowHeight;
                            
                            Rect barRect = new Rect(barX, barY, barWidth, barHeight);

                            float scaledWidth = 1;
                            float scaledHeight = 1;
                            GUI.BeginGroup(barRect);
                            {
                                switch(particleAtlas.particleType)
                                {
                                    case ParticleType.Zone:
                                    {

                                        for (float x = 0; x < barWidth; x += scaledWidth)
                                        {
                                            for(float y = 0;  y < barHeight; y += scaledHeight)
                                            {
                                                ParticleSpriteData spriteData = particleAtlas.spriteData[j];
                                                scaledWidth = GetScaledWidth(spriteData, particleAtlas);
                                                scaledHeight = GetScaledHeight(spriteData, particleAtlas);
                                                int texSpriteIndex = 0;

                                                ParticleSpriteData s = particleAtlas.spriteData[texSpriteIndex % particleAtlas.spriteData.Length];

                                                Vector2 uvPos = new Vector2(s.uvSizeAndPos.z, s.uvSizeAndPos.w);
                                                Vector2 uvSize = new Vector2(s.uvSizeAndPos.x, s.uvSizeAndPos.y);
                                                Rect uvRect = new Rect(uvPos, uvSize);

                                                Rect spriteRect = new Rect(x, y, scaledWidth, scaledHeight);
                                                GUI.DrawTextureWithTexCoords(spriteRect, particleAtlas.atlas.texture, uvRect);
                                                
                                                texSpriteIndex++;
                                            }
                                        }
                                    }
                                    break;
                                    case ParticleType.Scroll:
                                    {
                                        switch(posData.widthType)
                                        {
                                            case ParticleWidthType.Simple:
                                            case ParticleWidthType.Tiled:
                                            {
                                                ParticleSpriteData spriteData = particleAtlas.spriteData[posData.spriteIndex];
                                                scaledWidth = GetScaledWidth(spriteData, particleAtlas);
                                                scaledHeight = GetScaledHeight(spriteData, particleAtlas);
                                                for (float x = 0; x < barWidth; x += scaledWidth)
                                                {
                                                    Vector2 uvPos = new Vector2(spriteData.uvSizeAndPos.z, spriteData.uvSizeAndPos.w);
                                                    Vector2 uvSize = new Vector2(spriteData.uvSizeAndPos.x, spriteData.uvSizeAndPos.y);
                                                    Rect uvRect = new Rect(uvPos, uvSize);

                                                    Rect spriteRect = new Rect(x, 0, scaledWidth, scaledHeight);
                                                    GUI.DrawTextureWithTexCoords(spriteRect,particleAtlas.atlas.texture, uvRect);
                                                }
                                            }
                                            break;

                                            case ParticleWidthType.Sliced:
                                            {

                                                float totalPixelHeight = 0;
                                                float totalPixelWidth = 0;

                                                for (int k = 0; k < 9; k++)
                                                {
                                                    ParticleSpriteData spriteData = particleAtlas.spriteData[posData.spriteIndex + k];
                                                    float spritePixelWidth = spriteData.uvSizeAndPos.x * particleAtlas.atlas.texture.width;
                                                    float spritePixelheight = spriteData.uvSizeAndPos.y * particleAtlas.atlas.texture.height;
                                                    totalPixelWidth += spritePixelWidth;
                                                    totalPixelHeight += spritePixelheight;
                                                }

                                                int sliceIndex = 0;
                                                float sliceYPos = barHeight;
                                                for (int y = 0; y < 3; y++)
                                                {
                                                    float sliceXPos = 0;
                                                    for (int x = 0; x < 3; x++)
                                                    {
                                                        ParticleSpriteData spriteData = particleAtlas.spriteData[posData.spriteIndex + sliceIndex];
                                                        float spritePixelWidth = spriteData.uvSizeAndPos.x * particleAtlas.atlas.texture.width;
                                                        float spritePixelheight = spriteData.uvSizeAndPos.y * particleAtlas.atlas.texture.height;

                                                        scaledWidth = (spritePixelWidth / totalPixelWidth) * barWidth * 3;
                                                        scaledHeight = (spritePixelheight / totalPixelHeight) * barHeight * 3;

                                                        Vector2 uvPos = new Vector2(spriteData.uvSizeAndPos.z, spriteData.uvSizeAndPos.w);
                                                        Vector2 uvSize = new Vector2(spriteData.uvSizeAndPos.x, spriteData.uvSizeAndPos.y);
                                                        Rect uvRect = new Rect(uvPos, uvSize);

                                                        Rect spriteRect = new Rect(sliceXPos, sliceYPos - scaledHeight, scaledWidth, scaledHeight);
                                                        GUI.DrawTextureWithTexCoords(spriteRect, particleAtlas.atlas.texture, uvRect);
                                                        sliceIndex++;
                                                        sliceXPos += scaledWidth;
                                                       // Handles.DrawSolidRectangleWithOutline(spriteRect, Color.clear, Color.green);
                                                    }
                                                    sliceYPos -= scaledHeight;
                                                }
                                            }
                                            break;
                                        }
                                    }
                                    break;
                                }

                            }
                            GUI.EndGroup();

                            if (e.type == EventType.MouseDown && barRect.Contains(e.mousePosition))
                            {
                                selectedParticlAtlasIndex = i;
                                selectedPosDataIndex = j;

                                int mouseTicketXPos = (int)((e.mousePosition.x - graphRect.xMin) / totalTicketCheckXIncrement);
                                int mouseDepthYPos = (int)((e.mousePosition.y - graphRect.yMin) / graphRowHeight);

                                dragOffsetStartX = mouseTicketXPos - posData.ticketCheckStart;
                                dragOffsetStartY = mouseDepthYPos - (int)posData.depth;

                                dragOffsetWidth = (int)(mouseTicketXPos - ticketCheckSize);
                                dragOffsetHeight = (int)(mouseDepthYPos - posData.depthSize);

                                isAdjustingMetersStart = e.mousePosition.x < barRect.center.x;
                                isAdjustingDepthSize = e.mousePosition.y > barRect.center.y && particleAtlas.particleType == ParticleType.Zone;

                                int baseIndex = particleAtlas.atlas.simpleSprites.Length;

                                selectedSpriteIndex = posData.widthType == ParticleWidthType.Sliced ? baseIndex + ((posData.spriteIndex - baseIndex) / 8) : posData.spriteIndex;
                                selectedParticleAtlas = trip.particleAtlasArray[selectedParticlAtlasIndex];
                                selectedParticleCount = posData.particleCount;

                            }

                            if (selectedParticlAtlasIndex != i || selectedPosDataIndex != j) continue;

                            Handles.DrawSolidRectangleWithOutline(barRect, Color.clear, Color.blueViolet);

                            int spriteIndex = selectedSpriteIndex;
                            int spriteCount = 1;
                            ParticleWidthType widthType = posData.widthType;
                            
                            if (selectedSpriteIndex >= particleAtlas.atlas.simpleSprites.Length)
                            {
                                widthType = ParticleWidthType.Sliced;
                                spriteCount = 9;
                                spriteIndex = particleAtlas.atlas.simpleSprites.Length + ((selectedSpriteIndex - particleAtlas.atlas.simpleSprites.Length) * 9);
                            }
                            else
                            {
                                widthType = ParticleWidthType.Tiled;
                            }


                            posData.spriteIndex = spriteIndex;
                            posData.spritesPerParticle = spriteCount;
                            posData.widthType = widthType;
                            posData.particleCount = selectedParticleCount;

                            if (e.type == EventType.MouseDrag)
                            {
                                int mouseTicketXPos = (int)((e.mousePosition.x - graphRect.xMin) / totalTicketCheckXIncrement);
                                int mouseDepthYPos = (int)((e.mousePosition.y - graphRect.yMin) / graphRowHeight);

                                if (isAdjustingMetersStart)
                                {
                                    int curTicketStart = mouseTicketXPos - dragOffsetStartX;
                                    posData.ticketCheckStart = Mathf.Clamp(curTicketStart, 0, posData.ticketCheckEnd - 1);
                                }
                                else
                                {
                                    int curTicketEnd = posData.ticketCheckStart + (mouseTicketXPos - dragOffsetWidth);
                                    posData.ticketCheckEnd = Mathf.Clamp(curTicketEnd, posData.ticketCheckStart + 1 , totalTicketChecks);
                                }
                                if (isAdjustingDepthSize)
                                {
                                    int curDepthSize = mouseDepthYPos - dragOffsetHeight;
                                    posData.depthSize = Mathf.Clamp(curDepthSize, 1, (int)FAR_CLIP - posData.depth);
                                }
                                else
                                {
                                    int curDepth = mouseDepthYPos - dragOffsetStartY;
                                    posData.depth = Mathf.Clamp(curDepth, 1, (int)FAR_CLIP);
                                }

                                switch (posData.depth)
                                {
                                    case TRAIN_LINE_DEPTH:
                                    {
                                        posData.height = TRAIN_LINE_HEIGHT;
                                    }
                                    break;
                                    case TRAIN_TRACKS_DEPTH:
                                    {
                                        posData.height = TRAIN_WHEEL_HEIGHT;
                                    }
                                    break;

                                    default:
                                    {
                                        posData.height = 0;
                                    }
                                    break;
                                }
                            }

                            particleAtlas.posData[j] = posData;


                            if (e.type == EventType.MouseUp)
                            {
                                SaveTrip();
                            }
                            Repaint();
                        }
                    }
                }
                Handles.EndGUI();
            }
            GUI.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    private float GetScaledWidth(ParticleSpriteData spriteData, ParticleAtlas particleAtlas)
    {
        float spritePixelWidth = spriteData.uvSizeAndPos.x * particleAtlas.atlas.texture.width;
        float spritePixelHeight = spriteData.uvSizeAndPos.y * particleAtlas.atlas.texture.height;
        float scale = graphRowHeight / spritePixelHeight;
        return spritePixelWidth * scale;
    }
    private float GetScaledHeight(ParticleSpriteData spriteData, ParticleAtlas particleAtlas)
    {
        float spritePixelHeight = spriteData.uvSizeAndPos.y * particleAtlas.atlas.texture.height;
        float scale = graphRowHeight / spritePixelHeight;
        return spritePixelHeight * scale;
    }
    private float GetScaledHeightSlice(ParticleSpriteData spriteData, ParticleAtlas particleAtlas)
    {
        float spritePixelHeight = spriteData.uvSizeAndPos.y * particleAtlas.atlas.texture.height;
        float scale = graphRowHeight / Mathf.Max(spritePixelHeight, 0.0001f);
        return spritePixelHeight * scale * 0.33333f;
    }

    private float GetScaledWidthSlice(ParticleSpriteData spriteData, ParticleAtlas particleAtlas)
    {
        float spritePixelWidth = spriteData.uvSizeAndPos.x * particleAtlas.atlas.texture.width;
        float spritePixelHeight = spriteData.uvSizeAndPos.y * particleAtlas.atlas.texture.height;
        float scale = graphRowHeight / Mathf.Max(spritePixelHeight, 0.0001f);
        return spritePixelWidth * scale * 0.333333f;
    }
    private void SaveTrip()
    {
        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];
            particleAtlas.posData = particleAtlas.posData.OrderBy(p => p.ticketCheckStart).ToArray();
        }
        SetParticleIndices(ParticleType.Zone, ZONE_PARTICLE_COUNT);
        SetParticleIndices(ParticleType.Scroll, SCROLL_PARTICLE_COUNT);
        AssetDatabase.SaveAssets();
    }
    private void SetParticleIndices(ParticleType particleType, int maxParticleCount)
    {
        int minParticle = 0;
        int maxParticle = -1;

        for (int i = 0; i < totalTicketChecks; i++)
        {
            for (int j = 0; j < trip.particleAtlasArray.Length; j++)
            {
                ParticleAtlas particleAtlas = trip.particleAtlasArray[j];

                if (particleAtlas.particleType != particleType) continue;

                for (int k = 0; k < particleAtlas.posData.Length; k++)
                {
                    ParticlePosData posData = particleAtlas.posData[k];

                    if (posData.ticketCheckStart != i) break;

                    maxParticle += (int)posData.particleCount;
                    maxParticle %= maxParticleCount;

                    posData.minParticleIndex = minParticle;
                    posData.maxParticleIndex = maxParticle;

                    particleAtlas.posData[k] = posData;
                    minParticle = maxParticle + 1;
                    minParticle %= maxParticleCount;
                }
            }
        }
    }
}