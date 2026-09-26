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
    }

    private void Update()
    {
        
    }

}
