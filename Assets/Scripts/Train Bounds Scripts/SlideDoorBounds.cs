using UnityEngine;

public class SlideDoorBounds : Bounds
{
    public LayerMask chairsLayer;
    private BoxCollider2D Collider2D;

    private Component leftComponentBounds;
    private Component rightComponentBounds;
    public ChairBounds leftChairBounds {  get; private set; }
    public ChairBounds rightChairBounds { get; private set; }
    void Start()
    {
        Collider2D = this.GetComponent<BoxCollider2D>();

        SetNeighbouringBounds(Collider2D, 5, chairsLayer, typeof(ChairBounds), ref leftComponentBounds, ref rightComponentBounds);
        leftChairBounds = leftComponentBounds as ChairBounds;
        rightChairBounds = rightComponentBounds as ChairBounds;
    }
}
