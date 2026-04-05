using UnityEditor;
using UnityEngine;

public class Ticket : MonoBehaviour
{
    public AtlasRenderer ticket_renderer;
    public AtlasRenderer boardingStation_renderer;
    public AtlasRenderer disembarkingStation_renderer;


    public Bounds totalBounds;
    private void OnValidate()
    {
        SetTotalBounds();
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
    private void SetTotalBounds()
    {
        if (ticket_renderer == null) return;
        totalBounds = ticket_renderer.bounds;
    }

    public void SetText(string boardingStation, string disembarkingStation)
    {
        boardingStation_renderer.SetText(boardingStation);
        disembarkingStation_renderer.SetText(disembarkingStation);
    }
}
