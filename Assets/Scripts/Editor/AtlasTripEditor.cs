using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static Atlas;
using static AtlasRendering;
using static AtlasSpawn;

public class AtlasTripEditor : EditorWindow
{
    const float HEADER_COL_HEIGHT = 20;
    const float PADDING = 100;
    const float STATION_RECT_Y_OFFSET = 25;

    static SpawnData spawnData;

    static GUILayoutOption[] headerGUI;
    static GUILayoutOption[] horizontalGUI;
    static GUILayoutOption[] inputGUI;
    static GUILayoutOption[] buttonGUI;

    static GUIStyle stationLabel;
    static GUIStyle depthLabel;
    static GUIStyle graphHeader_style;

    static Rect fullRect;
    static Rect scrollViewRect;

    static Rect graphRect;

    static Vector2 graphPos;
    static Vector2 graphSize;
    static Vector2 scroll;

    static int[] particleAvailCount_zone_array;
    static int[] particleAvailCount_scroll_array;

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
    public float[] selectedDayNightValues;
    public float selectedScaleY;
    public float selectedScaleX;
    
    public float selectedPosY;
    
    public float selectedRandScale;

    public ParticleWidthType selectedWidthType;

    [MenuItem("Tools/Atlas Trip Editor")]
    private static void Open()
    {
        AtlasTripEditor tripEditor = GetWindow<AtlasTripEditor>("Atlas Trip Editor");
        inputGUI = new GUILayoutOption[] { GUILayout.Width(300) };
        buttonGUI = new GUILayoutOption[] { GUILayout.Width(150) };
        string spawnDataPath = AssetDatabase.GUIDToAssetPath("cb76807a71f74704a94a0f5cfe3b2c50");
        if (Directory.Exists(spawnDataPath))
        {
            SpawnData foundSpawnData = AssetDatabase.LoadAssetAtPath<SpawnData>(spawnDataPath);
            if (foundSpawnData != null)
            {
                spawnData = foundSpawnData;
            }
        }

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
        if (spawnData == null)
        {
            string spawnDataPath = AssetDatabase.GUIDToAssetPath("cb76807a71f74704a94a0f5cfe3b2c50");
            
            if (!AssetDatabase.AssetPathExists(spawnDataPath)) return;

            SpawnData foundSpawnData = AssetDatabase.LoadAssetAtPath<SpawnData>(spawnDataPath);
            
            if (foundSpawnData == null) return;
            
            spawnData = foundSpawnData;
        }

        fullRect = new Rect(0, 0, position.width, 3000);

        graphPos = new Vector2(PADDING, 100 + HEADER_COL_HEIGHT);
        graphSize = new Vector2(fullRect.width - PADDING * 2, fullRect.height);

        fullRect.height += PADDING * 4;

        scrollViewRect = new Rect(0, graphPos.y + 100, position.width, position.height);
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

        graphHeader_style = new GUIStyle();
        graphHeader_style.alignment = TextAnchor.LowerLeft;
        graphHeader_style.normal.textColor = Color.white;
        graphHeader_style.fontSize = 16;
        graphHeader_style.fontStyle = FontStyle.Normal;
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

        particleAvailCount_zone_array = new int[totalTicketChecks];
        particleAvailCount_scroll_array = new int[totalTicketChecks];

        Array.Fill(particleAvailCount_zone_array, ZONE_PARTICLE_COUNT);
        Array.Fill(particleAvailCount_scroll_array, SCROLL_PARTICLE_COUNT);

        for (int i = 0; i < particleAvailCount_zone_array.Length; i++)
        {
            for (int j = 0; j < trip.particleAtlasArray.Length; j++)
            {
                ParticleAtlas particleAtlas = trip.particleAtlasArray[j];

                switch(particleAtlas.particleType)
                {
                    case ParticleType.Scroll:
                    {
                        for (int k = 0; k < particleAtlas.posData.Length; k++)
                        {
                            ParticlePosData posData = particleAtlas.posData[k];
                            if (posData.ticketCheckStart <= i && posData.ticketCheckEnd > i)
                            {
                                particleAvailCount_scroll_array[i] -= posData.particleCount;
                            }
                        }
                    }
                    break;

                    case ParticleType.Zone:
                    {
                        for (int k = 0; k < particleAtlas.posData.Length; k++)
                        {
                            ParticlePosData posData = particleAtlas.posData[k];
                            if (posData.ticketCheckStart <= i && posData.ticketCheckEnd > i)
                            {
                                particleAvailCount_zone_array[i] -= posData.particleCount;
                            }
                        }
                    }
                    break;
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

                    newParticleData.spriteIndex = 0;
                    newParticleData.scaleX = 1;
                    newParticleData.scaleY = 1;
                    newParticleData.randScale = 0;
                    newParticleData.widthType = ParticleWidthType.Simple;
                    
                    particleDataList.Add(newParticleData);

                    selectedParticleAtlas.posData = particleDataList.ToArray();

                    selectedPosDataIndex = selectedParticleAtlas.posData.Length - 1;

                    ParticlePosData posData = selectedParticleAtlas.posData[selectedPosDataIndex];

                    selectedSpriteIndex = posData.spriteIndex;
                    selectedParticleCount = posData.particleCount;
                    selectedScaleX = posData.scaleX;
                    selectedScaleY = posData.scaleY;
                    selectedRandScale = posData.randScale;
                    selectedPosY = posData.posY;
                    selectedWidthType = posData.widthType;
                }

                bool removeParticleData = GUILayout.Button("Remove Particle", buttonGUI);
                if (removeParticleData)
                {
                    List<ParticlePosData> particleDataList = selectedParticleAtlas.posData.ToList();
                    particleDataList.RemoveAt(selectedPosDataIndex);
                    selectedParticleAtlas.posData = particleDataList.ToArray();
                    selectedPosDataIndex = Mathf.Clamp(selectedPosDataIndex - 1, 0, selectedParticleAtlas.posData.Length - 1);

                    if (selectedParticleAtlas.posData.Length > 0)
                    {
                        ParticlePosData posData = selectedParticleAtlas.posData[selectedPosDataIndex];

                        selectedParticleCount = posData.particleCount;
                        selectedScaleY = posData.scaleY;
                        selectedPosY = posData.posY;
                        selectedSpriteIndex = posData.spriteIndex;
                    }
                }
            }

            GUILayout.Space(10);
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();
    }
    private void DrawThirdHeader()
    {
        if (trip == null || selectedParticleAtlas == null || selectedParticleAtlas.posData.Length == 0 || selectedPosDataIndex == -1) return;

        EditorGUI.BeginChangeCheck();

        ParticlePosData selectedPosData = selectedParticleAtlas.posData[selectedPosDataIndex];

        EditorGUILayout.BeginHorizontal(horizontalGUI);
        {
            switch(selectedParticleAtlas.particleType)
            {
                case ParticleType.Zone:
                {
                    selectedParticleCount = EditorGUILayout.IntSlider(new GUIContent("Particle Count") , selectedParticleCount, 1, particleAvailCount_zone_array[selectedPosData.ticketCheckStart], headerGUI);

                    switch(selectedPosData.widthType)
                    {
                        case ParticleWidthType.Tiled:
                        case ParticleWidthType.Sliced:
                        {
                            selectedScaleY = EditorGUILayout.Slider(new GUIContent("Sprite Scale Y"), selectedScaleY, 1, 10, headerGUI);
                            selectedScaleX = EditorGUILayout.Slider(new GUIContent("Sprite Scale X"), selectedScaleX, 1, 10, headerGUI);
                            selectedRandScale = EditorGUILayout.Slider(new GUIContent("Random Scale Factor"), selectedRandScale, 0, 10, headerGUI);
                        }
                        break;
                    }
                }
                break;

                case ParticleType.Scroll:
                {
                    selectedParticleCount = 2;
                    selectedSpriteIndex = EditorGUILayout.IntSlider(new GUIContent("Sprite Index"), selectedSpriteIndex, 0, selectedParticleAtlas.spriteCount, headerGUI);

                    switch (selectedPosData.widthType)
                    {
                        case ParticleWidthType.Tiled:
                        case ParticleWidthType.Sliced:
                        {
                            selectedScaleY = EditorGUILayout.Slider(new GUIContent("Sprite Scale Y"), selectedScaleY, 1, 10, headerGUI);
                        }
                        break;
                    }

                }
                break;
            }

            selectedPosY = EditorGUILayout.Slider(new GUIContent("Sprite Pos Y"), selectedPosY, spawnData.bounds.min.y, spawnData.bounds.max.y, headerGUI);

            if (selectedPosData.widthType != ParticleWidthType.Sliced)
            {
                selectedWidthType = (ParticleWidthType)EditorGUILayout.EnumPopup(new GUIContent("Width Type"), selectedWidthType, headerGUI);
            }

            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            ApplySelectedParticleChanges(ref selectedPosData, selectedParticleAtlas);
            SaveTrip();
        }
    }
    private void DrawGraph()
    {
        if (trip == null || trip.stationsDataArray.Length == 0) return;

        Event e = Event.current;
        bool clickedBar = false;

        Vector2 vertLineTop = new Vector2();
        Vector2 vertLineBottom = new Vector2();
        Vector2 horLineLeft = new Vector2();
        Vector2 horLineRight = new Vector2();

        vertLineTop.y = graphRect.yMin;
        vertLineBottom.y = graphRect.yMax;
        horLineLeft.x = graphRect.xMin;
        horLineRight.x = graphRect.xMax;

        float totalTicketCheckXIncrement = (1 / (float)totalTicketChecks) * graphSize.x;

        if (trip.dayNightValues == null)
        {
            trip.dayNightValues = new float[totalTicketChecks];
        }
        if (totalTicketChecks > trip.dayNightValues.Length )
        {
            List<float> dayNightValuesList = trip.dayNightValues.ToList();
            dayNightValuesList.AddRange(new float[totalTicketChecks - trip.dayNightValues.Length]);
            trip.dayNightValues = dayNightValuesList.ToArray();
        }
        else if (totalTicketChecks < trip.dayNightValues.Length)
        {
            List<float> dayNightValuesList = trip.dayNightValues.ToList();
            dayNightValuesList.RemoveRange(trip.dayNightValues.Length - 1, trip.dayNightValues.Length - totalTicketChecks);
            trip.dayNightValues = dayNightValuesList.ToArray();
        }

        selectedDayNightValues = trip.dayNightValues;
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
                        string particleAvailCount_zone_string = particleAvailCount_zone_array[i].ToString();
                        Vector2 particleAvailCountSize_zone = graphHeader_style.CalcSize(new GUIContent(particleAvailCount_zone_string));
                        Rect particleAvailCountRect_zone = new Rect(posX - (particleAvailCountSize_zone.x * 0.5f), vertLineTop.y + particleAvailCountSize_zone.y, particleAvailCountSize_zone.x, particleAvailCountSize_zone.y);
                        GUI.Label(particleAvailCountRect_zone, particleAvailCount_zone_string, graphHeader_style);

                        string particleAvailCount_scroll_string = particleAvailCount_scroll_array[i].ToString();
                        Vector2 particleAvailCountSize_scroll = graphHeader_style.CalcSize(new GUIContent(particleAvailCount_scroll_string));
                        Rect particleAvailCountRect_scroll = new Rect(posX - (particleAvailCountSize_scroll.x * 0.5f), vertLineTop.y + particleAvailCountSize_scroll.y + particleAvailCountSize_zone.y, particleAvailCountSize_scroll.x, particleAvailCountSize_scroll.y);
                        GUI.Label(particleAvailCountRect_scroll, particleAvailCount_scroll_string, graphHeader_style);


                        Rect dayNightSliderRect = new Rect(posX - 50, particleAvailCountRect_scroll.yMin + 30, 200, 18);
                        EditorGUI.BeginChangeCheck();
                        selectedDayNightValues[i] = EditorGUI.Slider(dayNightSliderRect, GUIContent.none, selectedDayNightValues[i], 0, 1);
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

                    if (i == totalTicketChecks)
                    {
                        string zoneParticlesLeft_string = "Zone Particles Left";
                        Vector2 zoneParticleAvailLabelSize = graphHeader_style.CalcSize(new GUIContent(zoneParticlesLeft_string));
                        Rect zoneParticleAvailLabelRect = new Rect(posX - (zoneParticleAvailLabelSize.x * 0.5f), vertLineTop.y + zoneParticleAvailLabelSize.y, zoneParticleAvailLabelSize.x, zoneParticleAvailLabelSize.y);
                        GUI.Label(zoneParticleAvailLabelRect, zoneParticlesLeft_string, graphHeader_style);

                        string scrollParticlesLeft_string = "Scroll Particles Left";
                        Vector2 scrollParticleAvailLabelSize = graphHeader_style.CalcSize(new GUIContent(scrollParticlesLeft_string));
                        Rect scrollParticleAvailLabelRect = new Rect(posX - (scrollParticleAvailLabelSize.x * 0.5f), vertLineTop.y + scrollParticleAvailLabelSize.y + zoneParticleAvailLabelSize.y, scrollParticleAvailLabelSize.x, scrollParticleAvailLabelSize.y);
                        GUI.Label(scrollParticleAvailLabelRect, scrollParticlesLeft_string, graphHeader_style);

                        string dayNightValue_string = "Day Night Values";
                        Vector2 dayNightLabelSize = graphHeader_style.CalcSize(new GUIContent(dayNightValue_string));
                        Rect dayNighteAvailLabelRect = new Rect(posX - (dayNightLabelSize.x * 0.5f), vertLineTop.y + dayNightLabelSize.y + scrollParticleAvailLabelSize.y + zoneParticleAvailLabelSize.y, dayNightLabelSize.x, dayNightLabelSize.y);
                        GUI.Label(dayNighteAvailLabelRect, dayNightValue_string, graphHeader_style);
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    trip.dayNightValues = selectedDayNightValues;
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

                        if (particleAtlas.spriteData.Length == 0) continue;

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
                                                ParticleSpritesData spriteData = particleAtlas.spriteData[j];
                                                scaledWidth = GetScaledWidth(spriteData, particleAtlas);
                                                scaledHeight = GetScaledHeight(spriteData, particleAtlas);
                                                int texSpriteIndex = 0;

                                                ParticleSpritesData s = particleAtlas.spriteData[texSpriteIndex % particleAtlas.spriteData.Length];

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
                                                ParticleSpritesData spriteData = particleAtlas.spriteData[posData.spriteIndex];
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
                                                    ParticleSpritesData spriteData = particleAtlas.spriteData[posData.spriteIndex + k];
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
                                                        ParticleSpritesData spriteData = particleAtlas.spriteData[posData.spriteIndex + sliceIndex];
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

                                clickedBar = true;

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
                                selectedScaleY = posData.scaleY;
                                selectedScaleX = posData.scaleX;
                                selectedRandScale = posData.randScale;

                                selectedPosY = posData.posY;
                                selectedWidthType = posData.widthType;
                            }

                            if (selectedParticlAtlasIndex != i || selectedPosDataIndex != j) continue;

                            Handles.DrawSolidRectangleWithOutline(barRect, Color.clear, Color.blueViolet);

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

                if (e.type == EventType.MouseDown && !clickedBar)
                {
                    selectedPosDataIndex = -1;
                    selectedParticlAtlasIndex = -1;
                }
                Handles.EndGUI();
            }
            GUI.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }
    private void ApplySelectedParticleChanges(ref ParticlePosData posData, ParticleAtlas particleAtlas)
    {
        ParticleWidthType widthType = posData.widthType;

        if (selectedSpriteIndex >= particleAtlas.atlas.simpleSprites.Length)
        {
            widthType = ParticleWidthType.Sliced;
        }
        else
        {
            widthType = selectedWidthType;
        }

        posData.widthType = widthType;

        switch (posData.widthType)
        {
            case ParticleWidthType.Simple:
            {
                posData.spritesPerParticle = 1;
            }
            break;
            case ParticleWidthType.Tiled:
            {
                posData.spritesPerParticle = 1;
            }
            break;
            case ParticleWidthType.Sliced:
            {
                posData.spritesPerParticle = 9;
            }
            break;
        }


        switch (posData.depth)
        {
            case TRAIN_LINE_DEPTH:
            {
                posData.posY = TRAIN_LINE_HEIGHT;
            }
            break;
            case TRAIN_TRACKS_DEPTH:
            {
                posData.posY = TRAIN_WHEEL_HEIGHT;
            }
            break;
            default:
            {
                posData.posY = selectedPosY;
            }
            break;
        }

        switch (particleAtlas.particleType)
        {
            case ParticleType.Scroll:
            {
                posData.particleCount = 2;
                posData.scaleY = selectedScaleY;

                switch (posData.widthType)
                {
                    case ParticleWidthType.Simple:
                    {
                        posData.spriteIndex = selectedSpriteIndex;

                        ParticleSpritesData newSpriteData = particleAtlas.spriteData[posData.spriteIndex];
                        SimpleSprite sprite = particleAtlas.atlas.simpleSprites[posData.spriteIndex];
                        newSpriteData.uvSizeAndPos = sprite.uvSizeAndPos;

                        float pivotX = -sprite.uvPivot.x * sprite.worldSize.x;
                        float pivotY = -sprite.uvPivot.y * sprite.worldSize.y;
                        newSpriteData.worldPivotAndSize = new Vector4(pivotX, pivotY, sprite.worldSize.x, sprite.worldSize.y);
                        posData.quadScales = new Vector4[] { new Vector4(1, posData.scaleY, 0, 0) };
                        particleAtlas.spriteData[posData.spriteIndex] = newSpriteData;
                    }
                    break;
                    case ParticleWidthType.Tiled:
                    {
                        posData.spriteIndex = selectedSpriteIndex;

                        ParticleSpritesData newSpriteData = particleAtlas.spriteData[posData.spriteIndex];
                        SimpleSprite sprite = particleAtlas.atlas.simpleSprites[posData.spriteIndex];
                        newSpriteData.uvSizeAndPos = sprite.uvSizeAndPos;

                        posData.scaleX = spawnData.bounds.size.x / newSpriteData.worldPivotAndSize.z;

                        posData.quadScales = new Vector4[] { new Vector4(posData.scaleX, posData.scaleY, 0, 0) };
                        float pivotX = -sprite.uvPivot.x * sprite.worldSize.x;
                        float pivotY = -sprite.uvPivot.y * sprite.worldSize.y;
                        newSpriteData.worldPivotAndSize = new Vector4(pivotX, pivotY, sprite.worldSize.x, sprite.worldSize.y);
                        particleAtlas.spriteData[posData.spriteIndex] = newSpriteData;
                    }
                    break;
                    case ParticleWidthType.Sliced:
                    {
                        posData.spriteIndex = particleAtlas.atlas.simpleSprites.Length + ((selectedSpriteIndex - particleAtlas.atlas.simpleSprites.Length) * 9);

                        ParticleSpritesData leftSprite = particleAtlas.spriteData[posData.spriteIndex + 3];
                        ParticleSpritesData centerSprite = particleAtlas.spriteData[posData.spriteIndex + 4];
                        ParticleSpritesData rightSprite = particleAtlas.spriteData[posData.spriteIndex + 5];
                        posData.scaleX = (spawnData.bounds.size.x - leftSprite.worldPivotAndSize.z - rightSprite.worldPivotAndSize.z) / centerSprite.worldPivotAndSize.z;

                        int sliceSpriteIndex = (posData.spriteIndex - particleAtlas.atlas.simpleSprites.Length) / 9;

                        SliceSprite sliceSprite = particleAtlas.atlas.slicedSprites[sliceSpriteIndex];
                        float centerWorldSliceWidth = sliceSprite.sprite.worldSize.x - sliceSprite.worldSlices.x - sliceSprite.worldSlices.y;
                        float centerWorldSliceHeight = sliceSprite.sprite.worldSize.y - sliceSprite.worldSlices.z - sliceSprite.worldSlices.w;

                        posData.quadScales = GetQuadScalesNineSlice(posData.scaleX, posData.scaleY, centerWorldSliceWidth, centerWorldSliceHeight);

                        float rightColPos = sliceSprite.worldSlices.x + centerWorldSliceWidth;
                        float topRowPos = sliceSprite.worldSlices.z + centerWorldSliceHeight;

                        Vector4[] worldPivotsAndSizes = new Vector4[]
                        {
                                                new Vector4(0, 0, sliceSprite.worldSlices.x, sliceSprite.worldSlices.z),
                                                new Vector4(sliceSprite.worldSlices.x, 0, centerWorldSliceWidth, sliceSprite.worldSlices.z),
                                                new Vector4(rightColPos, 0, sliceSprite.worldSlices.y, sliceSprite.worldSlices.z),

                                                new Vector4(0, sliceSprite.worldSlices.z, sliceSprite.worldSlices.x, centerWorldSliceHeight),
                                                new Vector4(sliceSprite.worldSlices.x, sliceSprite.worldSlices.z, centerWorldSliceWidth, centerWorldSliceHeight),
                                                new Vector4(rightColPos, sliceSprite.worldSlices.z, sliceSprite.worldSlices.y, centerWorldSliceHeight),

                                                new Vector4(0, topRowPos, sliceSprite.worldSlices.x, sliceSprite.worldSlices.w),
                                                new Vector4(sliceSprite.worldSlices.x, topRowPos, centerWorldSliceWidth, sliceSprite.worldSlices.w),
                                                new Vector4(rightColPos, topRowPos, sliceSprite.worldSlices.y, sliceSprite.worldSlices.w),
                        };

                        for (int k = 0; k < posData.spritesPerParticle; k++)
                        {
                            ParticleSpritesData particleSpriteData = particleAtlas.spriteData[posData.spriteIndex + k];
                            ParticleSpritesData newSpriteData = new ParticleSpritesData();
                            newSpriteData.uvSizeAndPos = sliceSprite.uvSizeAndPos[k];
                            newSpriteData.worldPivotAndSize = worldPivotsAndSizes[k];

                            particleAtlas.spriteData[posData.spriteIndex + k] = newSpriteData;
                        }
                    }
                    break;
                }
            }
            break;
            case ParticleType.Zone:
            {
                posData.spriteIndex = 0;
                posData.particleCount = selectedParticleCount;

                switch (posData.widthType)
                {
                    case ParticleWidthType.Simple:
                    {
                        posData.scaleX = 1;
                        posData.scaleY = 1;
                        posData.quadScales = new Vector4[] { new Vector4(1, 1, 0, 0) };

                        for (int k = 0; k < particleAtlas.spriteData.Length; k++)
                        {
                            ParticleSpritesData newSpriteData = particleAtlas.spriteData[k];
                            SimpleSprite sprite = particleAtlas.atlas.simpleSprites[k];
                            newSpriteData.uvSizeAndPos = sprite.uvSizeAndPos;
                            float pivotX = -sprite.uvPivot.x * sprite.worldSize.x;
                            float pivotY = -sprite.uvPivot.y * sprite.worldSize.y;
                            newSpriteData.worldPivotAndSize = new Vector4(pivotX, pivotY, sprite.worldSize.x, sprite.worldSize.y);
                            particleAtlas.spriteData[k] = newSpriteData;
                        }
                    }
                    break;

                    case ParticleWidthType.Tiled:
                    {
                        posData.scaleX = selectedScaleX;
                        posData.scaleY = selectedScaleY;
                        posData.randScale = selectedRandScale;

                        List<Vector4> quadScales_list = new List<Vector4>();
                        for (int k = 0; k < posData.particleCount; k++)
                        {
                            float randScaleX = UnityEngine.Random.Range(-posData.randScale, posData.randScale);
                            float randScaleY = UnityEngine.Random.Range(-posData.randScale, posData.randScale);
                            randScaleX = Mathf.Max(posData.scaleX + randScaleX, 1);
                            randScaleY = Mathf.Max(posData.scaleY + randScaleY, 1);
                            quadScales_list.Add(new Vector4(randScaleX, randScaleY, 0, 0));
                        }
                        posData.quadScales = quadScales_list.ToArray();

                        for (int k = 0; k < particleAtlas.spriteData.Length; k++)
                        {
                            ParticleSpritesData newSpriteData = particleAtlas.spriteData[k];
                            SimpleSprite sprite = particleAtlas.atlas.simpleSprites[k];
                            newSpriteData.uvSizeAndPos = sprite.uvSizeAndPos;
                            float pivotX = -sprite.uvPivot.x * sprite.worldSize.x;
                            float pivotY = -sprite.uvPivot.y * sprite.worldSize.y;
                            newSpriteData.worldPivotAndSize = new Vector4(pivotX, pivotY, sprite.worldSize.x, sprite.worldSize.y);
                            particleAtlas.spriteData[k] = newSpriteData;
                        }
                    }
                    break;

                    case ParticleWidthType.Sliced:
                    {
                        posData.spriteIndex = 0;
                        posData.scaleX = selectedScaleX;
                        posData.scaleY = selectedScaleY;
                        posData.randScale = selectedRandScale;

                        List<Vector4> quadScales_list = new List<Vector4>();
                        for (int k = 0; k < posData.particleCount; k++)
                        {
                            float randScaleX = UnityEngine.Random.Range(-posData.randScale, posData.randScale);
                            float randScaleY = UnityEngine.Random.Range(-posData.randScale, posData.randScale);
                            randScaleX = Mathf.Max(posData.scaleX + randScaleX, 1);
                            randScaleY = Mathf.Max(posData.scaleY + randScaleY, 1);
                            int sliceSpriteIndex = (k % particleAtlas.atlas.slicedSprites.Length);
                            SliceSprite sliceSprite = particleAtlas.atlas.slicedSprites[sliceSpriteIndex];

                            float centerWorldSliceWidth = sliceSprite.sprite.worldSize.x - sliceSprite.worldSlices.x - sliceSprite.worldSlices.y;
                            float centerWorldSliceHeight = sliceSprite.sprite.worldSize.y - sliceSprite.worldSlices.z - sliceSprite.worldSlices.w;

                            quadScales_list.AddRange(GetQuadScalesNineSlice(randScaleX, randScaleY, centerWorldSliceWidth, centerWorldSliceHeight));
                        }
                        posData.quadScales = quadScales_list.ToArray();

                        for (int k = 0; k < particleAtlas.spriteData.Length; k += posData.spritesPerParticle)
                        {
                            int sliceSpriteIndex = Mathf.FloorToInt(k / 9);
                            SliceSprite sliceSprite = particleAtlas.atlas.slicedSprites[sliceSpriteIndex];
                            float centerWorldSliceWidth = sliceSprite.sprite.worldSize.x - sliceSprite.worldSlices.x - sliceSprite.worldSlices.y;
                            float centerWorldSliceHeight = sliceSprite.sprite.worldSize.y - sliceSprite.worldSlices.z - sliceSprite.worldSlices.w;

                            float rightColPos = sliceSprite.worldSlices.x + centerWorldSliceWidth;
                            float topRowPos = sliceSprite.worldSlices.z + centerWorldSliceHeight;

                            Vector4[] worldPivotsAndSizes = new Vector4[]
                            {
                                                    new Vector4(0, 0, sliceSprite.worldSlices.x, sliceSprite.worldSlices.z),
                                                    new Vector4(sliceSprite.worldSlices.x, 0, centerWorldSliceWidth, sliceSprite.worldSlices.z),
                                                    new Vector4(rightColPos, 0, sliceSprite.worldSlices.y, sliceSprite.worldSlices.z),

                                                    new Vector4(0, sliceSprite.worldSlices.z, sliceSprite.worldSlices.x, centerWorldSliceHeight),
                                                    new Vector4(sliceSprite.worldSlices.x, sliceSprite.worldSlices.z, centerWorldSliceWidth, centerWorldSliceHeight),
                                                    new Vector4(rightColPos, sliceSprite.worldSlices.z, sliceSprite.worldSlices.y, centerWorldSliceHeight),

                                                    new Vector4(0, topRowPos, sliceSprite.worldSlices.x, sliceSprite.worldSlices.w),
                                                    new Vector4(sliceSprite.worldSlices.x, topRowPos, centerWorldSliceWidth, sliceSprite.worldSlices.w),
                                                    new Vector4(rightColPos, topRowPos, sliceSprite.worldSlices.y, sliceSprite.worldSlices.w),
                            };

                            for (int l = 0; l < posData.spritesPerParticle; l++)
                            {
                                ParticleSpritesData particleSpriteData = particleAtlas.spriteData[k + l];
                                ParticleSpritesData newSpriteData = new ParticleSpritesData();
                                newSpriteData.uvSizeAndPos = sliceSprite.uvSizeAndPos[l];
                                newSpriteData.worldPivotAndSize = worldPivotsAndSizes[l];

                                particleAtlas.spriteData[k + l] = newSpriteData;
                            }
                        }


                    }
                    break;
                }
            }
            break;
        }

        particleAtlas.posData[selectedPosDataIndex] = posData;
    }
    private float GetScaledWidth(ParticleSpritesData spriteData, ParticleAtlas particleAtlas)
    {
        float spritePixelWidth = spriteData.uvSizeAndPos.x * particleAtlas.atlas.texture.width;
        float spritePixelHeight = spriteData.uvSizeAndPos.y * particleAtlas.atlas.texture.height;
        float scale = graphRowHeight / spritePixelHeight;
        return spritePixelWidth * scale;
    }
    private float GetScaledHeight(ParticleSpritesData spriteData, ParticleAtlas particleAtlas)
    {
        float spritePixelHeight = spriteData.uvSizeAndPos.y * particleAtlas.atlas.texture.height;
        float scale = graphRowHeight / spritePixelHeight;
        return spritePixelHeight * scale;
    }
    private void SaveTrip()
    {
        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];
            particleAtlas.posData = particleAtlas.posData.OrderBy(p => p.ticketCheckStart).ThenBy(p => p.depth).ToArray();
        }
        SetParticleIndices(ParticleType.Zone, ZONE_PARTICLE_COUNT);
        SetParticleIndices(ParticleType.Scroll, SCROLL_PARTICLE_COUNT);

        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];

            for (int j = 0; j < particleAtlas.posData.Length; j++)
            {
                ParticlePosData posData = particleAtlas.posData[j];
                posData.prevDepthIndices = new Vector2Int(-1, -1);
                int lastestTicketStart = 0;
                ParticlePosData prevPosData = default;
                for(int k = 0; k < trip.particleAtlasArray.Length;  k++)
                {
                    ParticleAtlas otherParticleAtlas = trip.particleAtlasArray[k];

                    for(int l = 0; l < otherParticleAtlas.posData.Length; l++)
                    {
                        ParticlePosData otherPosData = otherParticleAtlas.posData[l];

                        if (otherPosData.ticketCheckStart >= posData.ticketCheckStart) continue;
                        if (otherPosData.depth != posData.depth) continue;
                        if (otherPosData.ticketCheckStart < lastestTicketStart) continue;

                        lastestTicketStart = otherPosData.ticketCheckStart;
                        prevPosData = otherPosData;
                    }
                }
                posData.prevDepthIndices = new Vector2Int(prevPosData.minParticleIndex, prevPosData.maxParticleIndex);
                particleAtlas.posData[j] = posData;
            }
        }
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

                    posData.quadCount = (uint)(posData.particleCount * posData.spritesPerParticle);

                    if (posData.ticketCheckStart != i) continue;

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