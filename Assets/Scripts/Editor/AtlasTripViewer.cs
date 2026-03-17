using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static AtlasSpawn;
public class AtlasTripViewer : EditorWindow
{
    public TripSO trip;
    public TrainStatsSO trainStats;
    const float HEADER_COL_WIDTH = 300;
    const float HEADER_COL_HEIGHT = 20;
    const float padding = 100;
    const float zonePadding = 10;

    private int selectedIndex_zone;
    private int selectedIndex_zoneSpawner;
    private int dragOffsetZoneMetersLength;
    private int dragOffsetZoneMetersStart;

    private int selectedIndex_station;
    private int dragOffsetStation;

    private bool isAdjustingMetersStart;
    [MenuItem("Tools/Atlas Trip Viewer")]
    private static void Open()
    {
        GetWindow<AtlasTripViewer>("Atlas Trip Viewer");
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
        Vector2 graphSize = new Vector2(position.width - padding * 2, position.height - padding * 2 - HEADER_COL_HEIGHT);
        Vector2 graphPos = new Vector2(padding, padding + HEADER_COL_HEIGHT);
        Rect graphRect = new Rect(graphPos, graphSize);
        Handles.BeginGUI();
        Handles.DrawSolidRectangleWithOutline(graphRect, Color.clear, Color.white);

        for (int i = 0; i < ZONE_SPAWNER_COUNT - 1; i++)
        {
            float t = (i + 1) / (float)ZONE_SPAWNER_COUNT;
            float curHeight = graphRect.yMin + t * graphRect.height;

            Vector2 p1 = new Vector2(graphRect.xMin, curHeight);
            Vector2 p2 = new Vector2(graphRect.xMax, curHeight);
            Handles.DrawLine(p1, p2);
        }

        GUIStyle spawnerLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.UpperLeft, normal = { textColor = Color.white } };
        for (int i = 0; i < ZONE_SPAWNER_COUNT; i++)
        {
            float yOffset = (1 / (float)ZONE_SPAWNER_COUNT) * graphRect.height * 0.5f;
            float t = i / (float)ZONE_SPAWNER_COUNT;
            float curX = graphRect.xMin - (padding * 0.5f);
            float curY = (graphRect.yMin + t * graphRect.height) + yOffset;
            Rect spawnerLabelRect = new Rect(curX, curY, 20, 200);
            GUI.Label(spawnerLabelRect, ((AtlasSpawn.ZoneArea)i).ToString()[0].ToString(), spawnerLabelStyle);
        }

        GUIStyle zoneLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.UpperLeft, normal = { textColor = Color.black } };
        Event e = Event.current;
        for (int i = 0; i < trip.zoneSpawnerData.Length; i++)
        {
            ZoneSpawnerData zoneSpawnerData = trip.zoneSpawnerData[i];
            for (int j = 0; j < zoneSpawnerData.zones.Length; j++)
            {
                ref Zone zone = ref zoneSpawnerData.zones[j];

                if (zone.atlas == null) continue;
                float zoneMetersLength = (float)(zone.metersEnd - zone.metersStart);
                float zoneWidth = (zoneMetersLength / (float)trip.tripMeters) * graphRect.width;
                float zoneHeight = (graphRect.height / (float)ZONE_SPAWNER_COUNT) - (zonePadding * 2);

                float zoneX = graphRect.xMin + ((float)zone.metersStart / (float)trip.tripMeters) * graphRect.width;
                float zoneY = (graphRect.yMin + ((float)zoneSpawnerData.area / ZONE_SPAWNER_COUNT) * graphRect.height) + zonePadding;

                Rect zoneRect = new Rect(zoneX, zoneY, zoneWidth, zoneHeight);

                Color zoneColor = (selectedIndex_zoneSpawner == i && selectedIndex_zone == j) ? Color.hotPink : Color.lightSeaGreen;
                Handles.DrawSolidRectangleWithOutline(zoneRect, zoneColor, Color.black);
                Rect zoneLabelRect = new Rect(zoneRect.xMin, zoneRect.yMin, 200, 20);
                GUI.Label(zoneLabelRect, zone.atlas.zoneType.ToString(), zoneLabelStyle);


                if (e.type == EventType.MouseDown && zoneRect.Contains(e.mousePosition))
                {
                    selectedIndex_station = -1;
                    selectedIndex_zoneSpawner = i;
                    selectedIndex_zone = j;
                    int mouseMeters = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.tripMeters);
                    dragOffsetZoneMetersStart = mouseMeters - zone.metersStart;
                    dragOffsetZoneMetersLength = mouseMeters - (int)zoneMetersLength;
                    isAdjustingMetersStart = e.mousePosition.x < zoneRect.center.x;
                }
                if (selectedIndex_zoneSpawner == i && selectedIndex_zone == j && e.type == EventType.MouseDrag)
                {
                    int mouseMeters = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.tripMeters);
                    if (isAdjustingMetersStart)
                    {
                        zone.metersStart = mouseMeters - dragOffsetZoneMetersStart;
                        zone.metersStart = Mathf.Clamp(zone.metersStart, 0, zone.metersEnd);
                    }
                    else
                    {
                        zone.metersEnd = zone.metersStart + (mouseMeters - dragOffsetZoneMetersLength);
                        zone.metersEnd = Mathf.Clamp(zone.metersEnd, zone.metersStart, trip.tripMeters);
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
            }

        }

        for (int i = 0; i < trip.stations.Length; i++)
        {
            StationSO selectedStation = trip.stations[i];
            float rectSize = 20;
            float posX = graphRect.xMin + ((float)selectedStation.metersPosition / (float)trip.tripMeters) * graphRect.width;
            float posY = graphRect.yMin - 20;
            Rect stationRect = new Rect(posX, posY, rectSize, rectSize);
            Color zoneColor = (selectedIndex_station == i) ? Color.orangeRed : Color.lawnGreen;
            Handles.DrawSolidRectangleWithOutline(stationRect, zoneColor, Color.black);

            Rect stationLabelRect = new Rect(stationRect.xMin, stationRect.yMin, 200, 20);
            GUI.Label(stationLabelRect, selectedStation.station_prefab.name, zoneLabelStyle);
            if (e.type == EventType.MouseDown && stationRect.Contains(e.mousePosition))
            {
                selectedIndex_zone = -1;
                selectedIndex_station = i;
                int mouseMeters = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.tripMeters);
                dragOffsetStation = mouseMeters - selectedStation.metersPosition;
            }
            if (selectedIndex_station == i && e.type == EventType.MouseDrag)
            {
                int mouseMeters = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.tripMeters);
                selectedStation.metersPosition = mouseMeters - dragOffsetStation;
                selectedStation.metersPosition = Mathf.Clamp(selectedStation.metersPosition, 0, trip.tripMeters);

                EditorUtility.SetDirty(trip);
                Repaint();
            }

            if (e.type == EventType.MouseUp && selectedIndex_station == i)
            {
                trip.stations =  trip.stations.OrderBy(station => station.metersPosition).ToArray();
                selectedIndex_station = -1;
                e.Use();
            }
        }
        float metersTravelledGraphPosX = graphRect.xMin + ((trainStats.metersTravelled / trip.tripMeters) * graphRect.width);
        Vector2 meterTravelledTop = new Vector2(metersTravelledGraphPosX, graphRect.yMin);
        Vector2 meterTravelledBottom = new Vector2(metersTravelledGraphPosX, graphRect.yMax);
        Handles.color = Color.yellow;
        Handles.DrawLine(meterTravelledTop, meterTravelledBottom);

        Handles.EndGUI();
    }
}

