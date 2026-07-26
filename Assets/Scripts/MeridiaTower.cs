using UnityEngine;

public class MeridiaTower : MonoBehaviour
{
    public SpyStatsSO spyStats;
    public Room[] rooms;
    public AtlasRenderer[] elevatorScrollingRenderers;
    private void Start()
    {
        spyStats.curLocationBounds = rooms[0].bounds;
    }
    private void Update()
    {
        for (int i = 0; i < elevatorScrollingRenderers.Length; i++)
        {
            AtlasRenderer rend = elevatorScrollingRenderers[i];
            rend.custom.y += Time.deltaTime / rend.sprite.worldSize.y;
            if (rend.custom.y >= 1) rend.custom.y = 0;
        }
    }
}
