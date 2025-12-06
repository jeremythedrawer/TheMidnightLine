using System;
using UnityEngine;

public class Clipboard : MonoBehaviour
{
    [Serializable] public struct ComponentData
    {
        public RectTransform rectTransform;
        public RectTransform profilePagesTransform;
    }
    [SerializeField] ComponentData components;
    [Serializable] public struct SOData
    { 
        public ClipboardSettingsSO settings;
        public ClipboardStatsSO stats;
        public SpyInputsSO inputs;
    }
    [SerializeField] SOData soData;
    private void Start()
    {
        soData.stats.startYPos = components.rectTransform.localPosition.y;
        soData.stats.active = false;
        CreateProfilePages();
    }
    private void Update()
    {
        if (soData.inputs.clipboard) soData.stats.active = soData.stats.active ? false : true;
        float targetYPos = soData.stats.active ? soData.settings.onScreenYPos : soData.stats.startYPos;
        float curRectYPos = Mathf.Lerp(components.rectTransform.localPosition.y, targetYPos, soData.settings.moveTime * Time.deltaTime);
        components.rectTransform.localPosition = new Vector3(components.rectTransform.localPosition.x, curRectYPos, components.rectTransform.localPosition.z);
    }

    private void OnApplicationQuit()
    {
        soData.stats.profilePageData.Clear();
    }
    private void CreateProfilePages()
    {
        for (int i = 0; i < soData.stats.profilePageData.Count; i++)
        {
            ProfilePage page = Instantiate(soData.settings.profilePagePrefab, components.profilePagesTransform);
            page.stats.pageIndex = i;
        }
    }
    private void OnDrawGizmosSelected()
    {
        
    }
}
