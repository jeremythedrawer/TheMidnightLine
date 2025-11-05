using Proselyte.Sigils;
using System;
using UnityEngine;

public class CarriageChairs : MonoBehaviour
{
    [Serializable] public struct ComponentData
    {
        public SpriteRenderer spriteRenderer;
    }
    [SerializeField] ComponentData componentData;

    [Serializable] public struct SettingsData
    {
        public int chairAmount;
    }
    [SerializeField] SettingsData settingsData;

    [Serializable] public struct GameEventData
    {
        public GameEvent OnUnlockSlideDoors;
    }
    [SerializeField] GameEventData gameEventData;

    [Serializable] public struct ChairData
    {
        internal float chairXPos;
        internal bool filled;
    }
    internal ChairData[] chairData;

    private void Awake()
    {
        chairData = new ChairData[settingsData.chairAmount];

    }

    private void OnEnable()
    {
        gameEventData.OnUnlockSlideDoors.RegisterListener(SetChairData);
    }

    private void OnDisable()
    {
        gameEventData.OnUnlockSlideDoors.UnregisterListener(SetChairData);
    }
    private void SetChairData()
    {
        float chairLength = componentData.spriteRenderer.sprite.bounds.size.x / settingsData.chairAmount;
        float firstChairPos = transform.position.x + (chairLength * 0.5f);
        for (int i = 0; i < chairData.Length; i++)
        {
            chairData[i].chairXPos = firstChairPos + (chairLength * i);
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (componentData.spriteRenderer == null || settingsData.chairAmount == 0 || chairData == null) return;
        Gizmos.color = Color.red;
        for (int i = 0; i < chairData.Length; i++)
        {
            Gizmos.DrawLine(new Vector2(chairData[i].chairXPos + componentData.spriteRenderer.bounds.min.x, componentData.spriteRenderer.bounds.min.y), new Vector2(chairData[i].chairXPos + componentData.spriteRenderer.bounds.min.x, componentData.spriteRenderer.bounds.max.y));
        }
    }

}
