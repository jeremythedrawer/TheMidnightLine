using System;
using UnityEngine;

public class Clipboard : MonoBehaviour
{
    [Serializable] public struct ComponentData
    {
        public RectTransform rectTransform;
        public Canvas canvas;
    }
    [SerializeField] ComponentData components;
    [Serializable] public struct SOData
    { 
        public ClipboardSO clipboard;
        public SpyInputsSO inputs;
    }
    [SerializeField] SOData soData;
    [Serializable] public struct Stats
    {
        internal float startYPos;
        internal bool active;
    }
    [SerializeField] Stats stats;
    private void Start()
    {
        stats.startYPos = components.rectTransform.localPosition.y;
    }
    private void Update()
    {
        if (soData.inputs.clipboard) stats.active = stats.active ? false : true;
        float targetYPos = stats.active ? soData.clipboard.onScreenYPos : stats.startYPos;
        float curRectYPos = Mathf.Lerp(components.rectTransform.localPosition.y, targetYPos, soData.clipboard.moveTime * Time.deltaTime);
        components.rectTransform.localPosition = new Vector3(components.rectTransform.localPosition.x, curRectYPos, components.rectTransform.localPosition.z);
    }

    private void OnDrawGizmosSelected()
    {
        
    }
}
