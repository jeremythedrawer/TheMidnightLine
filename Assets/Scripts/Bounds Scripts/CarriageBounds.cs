using UnityEngine;

public class CarriageBounds : OldBounds
{
    [Header("References")]
    public InsideBounds insideBounds;
    public enum CarriageType { backCarriage, middleCarriage, frontCarriage }
    public CarriageType carriageType;

    public UnityEngine.Bounds bounds => insideBounds.objectBounds;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
