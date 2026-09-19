using UnityEngine;


#if UNITY_EDITOR
using UnityEditor.IMGUI.Controls;
using UnityEditor;
#endif

public class CarriageMap : MonoBehaviour
{

    public AtlasRenderer backgroundRenderer;

    public UIData uiData;
    public Options options;

    [Header("Generated")]
    public StationLineMap[] stationLineMaps;
    public StationLineMap curStationLineMap;

    public Vector3 startCorner;
    public float verticalSegment;

    public int curStationLineMapIndex;

    private void Start()
    {
        Init();   
    }

    private void Update()
    {
        
    }

    private void Init()
    {
        stationLineMaps = new StationLineMap[options.curTrip.stationsDataArray.Length];
        for (int i = 0; i < options.curTrip.stationsDataArray.Length; i++)
        {
            StationLineMap stationLineMap = Instantiate(uiData.stationLineMapPrefrab, transform);
            stationLineMap.SetText(options.curTrip.stationsDataArray[i].name);

            Vector3 localPos = new Vector3();
            localPos.x = startCorner.x;
            localPos.y = startCorner.y - (verticalSegment * i);
            localPos.z = startCorner.z;

            stationLineMap.transform.localPosition = localPos;
            stationLineMaps[i] = stationLineMap;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CarriageMap))]
public class CarriageMapEditor : Editor
{
    private void OnSceneGUI()
    {
        CarriageMap carriageMap = (CarriageMap)target;

        Vector3 activeStartCorner = carriageMap.transform.TransformPoint(carriageMap.startCorner);

        Vector3 stationLineBoundsSize = carriageMap.uiData.stationLineMapPrefrab.textRenderer.bounds.size;
        float stationLineMapHeight = stationLineBoundsSize.y + carriageMap.verticalSegment;

        EditorGUI.BeginChangeCheck();

        Vector3 startCornerPosHandle = Handles.PositionHandle(activeStartCorner, carriageMap.transform.rotation);

        Vector3 endCornerPos = new Vector3();
        endCornerPos.x = startCornerPosHandle.x;
        endCornerPos.z = startCornerPosHandle.z;
        endCornerPos.y = startCornerPosHandle.y - (carriageMap.verticalSegment * carriageMap.options.curTrip.stationsDataArray.Length); 

        Vector3 endCornerPosHandle = Handles.PositionHandle(endCornerPos, carriageMap.transform.rotation);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(carriageMap, "Change Map Bounds");
            carriageMap.startCorner = carriageMap.transform.InverseTransformPoint(startCornerPosHandle);
            carriageMap.verticalSegment = (startCornerPosHandle.y - endCornerPosHandle.y) / (float)carriageMap.options.curTrip.stationsDataArray.Length;
            endCornerPos.x = startCornerPosHandle.x;
            endCornerPos.y = endCornerPosHandle.y;
            endCornerPos.z = startCornerPosHandle.z;
            EditorUtility.SetDirty(carriageMap);
        }

        Handles.color = Color.cyan;
        
        Handles.DrawLine(startCornerPosHandle, endCornerPosHandle);

        Vector3 startStationLineMapPos = new Vector3();
        startStationLineMapPos.x = startCornerPosHandle.x;
        startStationLineMapPos.z = startCornerPosHandle.z;
        
        Vector3 endStationLineMapPos = new Vector3();
        endStationLineMapPos.x = startCornerPosHandle.x + stationLineBoundsSize.x;
        endStationLineMapPos.z = startCornerPosHandle.z;

        for (int i = 0; i < carriageMap.options.curTrip.stationsDataArray.Length; i++)
        {
            float curYPos = startCornerPosHandle.y - (carriageMap.verticalSegment * i); 
            startStationLineMapPos.y = curYPos;
            endStationLineMapPos.y = curYPos;
            Handles.color = Color.cyan;
            Handles.DrawLine(startStationLineMapPos, endStationLineMapPos);
            
            float stationLineBoundsYPos = curYPos - stationLineBoundsSize.y;
            startStationLineMapPos.y = stationLineBoundsYPos;
            endStationLineMapPos.y = stationLineBoundsYPos;
            Handles.color = carriageMap.verticalSegment < stationLineBoundsSize.y ? Color.red : Color.yellow;
            Handles.DrawDottedLine(startStationLineMapPos, endStationLineMapPos, 1);
        }

        GUIContent startCornerContent = new GUIContent("Start Corner");
        Handles.Label(startCornerPosHandle, startCornerContent);
    }
}
#endif
