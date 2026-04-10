using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static AtlasSpawn;
public class AtlasTripEditor : EditorWindow
{
    const float HEADER_COL_WIDTH = 300;
    const float HEADER_COL_HEIGHT = 20;
    const float PADDING = 100;
    const float ZONE_PADDING = 10;
    const float STATION_RECT_SIZE = 20;
    const float STATION_RECT_Y_OFFSET = 25;

    public TripSO trip;
    public TrainStatsSO trainStats;


    private int selectedIndex_zone;
    private int selectedIndex_zoneSpawner;
    private int dragOffsetZoneMetersLength;
    private int dragOffsetZoneMetersStart;

    private int selectedIndex_station;
    private int dragOffsetStation;

    private bool isAdjustingMetersStart;
    [MenuItem("Tools/Atlas Trip Editor")]
    private static void Open()
    {
        GetWindow<AtlasTripEditor>("Atlas Trip Editor");
    }

    private void Update()
    {
        Repaint();
    }
    private void OnGUI()
    {
        GUILayoutOption[] GUIWidth = { GUILayout.Width(HEADER_COL_WIDTH), GUILayout.Height(HEADER_COL_HEIGHT) };

        EditorGUILayout.BeginHorizontal();
        trip = (TripSO)EditorGUILayout.ObjectField("Trip", trip, typeof(TripSO), allowSceneObjects: false, GUIWidth);
        trainStats = (TrainStatsSO)EditorGUILayout.ObjectField("Train Stats", trainStats, typeof(TrainStatsSO), allowSceneObjects: false, GUIWidth);
        EditorGUILayout.EndHorizontal();

        if (trip == null) return;

        EditorGUILayout.BeginHorizontal();
        DrawGraph();
        EditorGUILayout.EndHorizontal();
    }
    private void DrawGraph()
    {
        Vector2 graphSize = new Vector2(position.width - PADDING * 2, position.height - PADDING * 2 - HEADER_COL_HEIGHT);
        Vector2 graphPos = new Vector2(PADDING, PADDING + HEADER_COL_HEIGHT);
        Rect graphRect = new Rect(graphPos, graphSize);
        Handles.BeginGUI();
        Handles.DrawSolidRectangleWithOutline(graphRect, Color.clear, Color.white);

        GUIStyle spawnerLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.UpperLeft, normal = { textColor = Color.white } };
        GUIStyle stationLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.UpperLeft, normal = { textColor = Color.black } };
        
        Event e = Event.current;


        trip.totalTicketsToCheck = 0;
        for(int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO station = trip.stationsDataArray[i];
            trip.totalTicketsToCheck += station.ticketsToCheckBeforeSpawn;
        }

        int stationIndex = 0;
        int ticketChecks = 0;
        for (int i = 0; i <= trip.totalTicketsToCheck; i++)
        {
            float t = (i) / (float)trip.totalTicketsToCheck;
            float posX = graphRect.xMin + t * graphRect.width;

            Vector2 p1 = new Vector2(posX, graphRect.yMin);
            Vector2 p2 = new Vector2(posX, graphRect.yMax);
            Handles.DrawLine(p1, p2);

            StationSO station = trip.stationsDataArray[stationIndex];

            if (station.ticketsToCheckBeforeSpawn == ticketChecks)
            {
                float posY = graphRect.yMin - STATION_RECT_Y_OFFSET;
                Rect stationRect = new Rect(posX, posY, STATION_RECT_SIZE, STATION_RECT_SIZE);
                Color zoneColor = (selectedIndex_station == i) ? Color.orangeRed : Color.lawnGreen;
                Handles.DrawSolidRectangleWithOutline(stationRect, zoneColor, Color.black);

                Rect stationLabelRect = new Rect(stationRect.xMin, stationRect.yMin - HEADER_COL_HEIGHT, 200, HEADER_COL_HEIGHT);
                GUI.Label(stationLabelRect, station.station_prefab.name, stationLabelStyle);
                stationIndex++;

                ticketChecks = 0;
            }
            ticketChecks++;
        }

        for (int i = 0; i < ZONE_SPAWNER_COUNT - 1; i++)
        {
            float t = (i + 1) / (float)ZONE_SPAWNER_COUNT;
            float curHeight = graphRect.yMin + t * graphRect.height;

            Vector2 p1 = new Vector2(graphRect.xMin, curHeight);
            Vector2 p2 = new Vector2(graphRect.xMax, curHeight);
            Handles.DrawLine(p1, p2);

            float yOffset = (1 / (float)ZONE_SPAWNER_COUNT) * graphRect.height * 0.5f;
            float t1 = i / (float)ZONE_SPAWNER_COUNT;
            float curX = graphRect.xMin - (PADDING * 0.5f);
            float curY = (graphRect.yMin + t1 * graphRect.height) + yOffset;
            Rect spawnerLabelRect = new Rect(curX, curY, 20, 200);
            GUI.Label(spawnerLabelRect, ((ZoneArea)i).ToString()[0].ToString(), spawnerLabelStyle);
        }

        for (int i = 0; i < trip.zoneSpawnerData.Length; i++)
        {
            ZoneSpawnerData zoneSpawnerData = trip.zoneSpawnerData[i];
            for (int j = 0; j < zoneSpawnerData.zones.Length; j++)
            {
                ref Zone zone = ref zoneSpawnerData.zones[j];

                if (zone.atlas == null) continue;

                float zoneTicketsToCheck = (float)(zone.ticketCheckEnd - zone.ticketCheckStart);
                float zoneWidth = (zoneTicketsToCheck / (float)trip.totalTicketsToCheck) * graphRect.width;
                float zoneHeight = (graphRect.height / (float)ZONE_SPAWNER_COUNT) - (ZONE_PADDING * 2);

                float zoneX = graphRect.xMin + ((float)zone.ticketCheckStart / (float)trip.totalTicketsToCheck) * graphRect.width;
                float zoneY = (graphRect.yMin + ((float)zoneSpawnerData.area / ZONE_SPAWNER_COUNT) * graphRect.height) + ZONE_PADDING;

                Rect zoneRect = new Rect(zoneX, zoneY, zoneWidth, zoneHeight);

                Color zoneColor = (selectedIndex_zoneSpawner == i && selectedIndex_zone == j) ? Color.hotPink : Color.lightSeaGreen;
                Handles.DrawSolidRectangleWithOutline(zoneRect, zoneColor, Color.black);
                Rect zoneLabelRect = new Rect(zoneRect.xMin, zoneRect.yMin, 200, 20);
                GUI.Label(zoneLabelRect, zone.atlas.zoneType.ToString(), stationLabelStyle);


                if (e.type == EventType.MouseDown && zoneRect.Contains(e.mousePosition))
                {
                    selectedIndex_station = -1;
                    selectedIndex_zoneSpawner = i;
                    selectedIndex_zone = j;
                    int mouseMeters = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.totalTicketsToCheck);
                    dragOffsetZoneMetersStart = mouseMeters - zone.ticketCheckStart;
                    dragOffsetZoneMetersLength = mouseMeters - (int)zoneTicketsToCheck;
                    isAdjustingMetersStart = e.mousePosition.x < zoneRect.center.x;
                }
                if (selectedIndex_zoneSpawner == i && selectedIndex_zone == j && e.type == EventType.MouseDrag)
                {
                    int mouseT = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.totalTicketsToCheck);
                    if (isAdjustingMetersStart)
                    {
                        zone.ticketCheckStart = mouseT - dragOffsetZoneMetersStart;
                    }
                    else
                    {
                        zone.ticketCheckEnd = zone.ticketCheckStart + (mouseT - dragOffsetZoneMetersLength);
                    }

                    
                    EditorUtility.SetDirty(trip);
                    Repaint();
                }

                if (e.type == EventType.MouseUp)
                {
                    zone.zoneUVSizeAndPosArray = new Vector4[zone.atlas.simpleSprites.Length];
                    zone.zoneWorldSizesArray = new Vector2[zone.atlas.simpleSprites.Length];
                    for (int k = 0; k < zone.atlas.simpleSprites.Length; k++)
                    {
                        zone.zoneUVSizeAndPosArray[k] = zone.atlas.simpleSprites[k].uvSizeAndPos;
                        zone.zoneWorldSizesArray[k] = zone.atlas.simpleSprites[k].worldSize;
                    }

                    if (selectedIndex_zone == i)
                    {
                        selectedIndex_zone = -1;
                        e.Use();
                    }
                }
                zone.ticketCheckStart = Mathf.Clamp(zone.ticketCheckStart, 0, zone.ticketCheckEnd - 1);
                zone.ticketCheckEnd = Mathf.Clamp(zone.ticketCheckEnd, zone.ticketCheckStart + 1, trip.totalTicketsToCheck);
            }
        }
        float ticketsCheckedGraphPosX = graphRect.xMin + ((trip.ticketsCheckedSinceStart / trip.totalTicketsToCheck) * graphRect.width);
        Vector2 ticketsCheckedTop = new Vector2(ticketsCheckedGraphPosX, graphRect.yMin);
        Vector2 ticketsCheckedBottom = new Vector2(ticketsCheckedGraphPosX, graphRect.yMax);
        Handles.color = Color.yellow;
        Handles.DrawLine(ticketsCheckedTop, ticketsCheckedBottom);

        Handles.EndGUI();
    }
}

