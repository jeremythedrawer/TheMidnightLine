using System;
using UnityEngine;
using UnityEngine.UI;

public class Clipboard : MonoBehaviour
{
    [Serializable] public struct ComponentData
    {
        public RectTransform rectTransform;
        public RectTransform imagesRectTransform;
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
        soData.stats.startYPos = components.imagesRectTransform.localPosition.y;
        soData.stats.hoverYPos = soData.stats.startYPos + ((soData.settings.onScreenYPos - soData.stats.startYPos) * 0.5f);
        soData.stats.active = false;
        CreateProfilePages();
    }
    private void Update()
    {
        if (soData.inputs.clipboard == 1)
        {
            soData.stats.active = true;
        }
        else if (soData.inputs.clipboard == -1)
        {
            soData.stats.active = false;
        }

        if (soData.stats.active)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(components.rectTransform, soData.inputs.mouseScreenPos, Camera.main, out Vector2 localPoint))
            {
                if (localPoint.x > components.imagesRectTransform.rect.xMin && localPoint.y > components.rectTransform.localPosition.y)
                {
                    soData.stats.targetYPos = soData.stats.hoverYPos;
                }
                else
                {
                    soData.stats.targetYPos = soData.settings.onScreenYPos;
                }
            }
        }
        else
        {
            soData.stats.targetYPos = soData.stats.startYPos;
        }
        float curRectYPos = Mathf.Lerp(components.imagesRectTransform.localPosition.y, soData.stats.targetYPos, soData.settings.moveTime * Time.deltaTime);
        components.imagesRectTransform.localPosition = new Vector3(components.imagesRectTransform.localPosition.x, curRectYPos, components.imagesRectTransform.localPosition.z);
    }

    private void OnApplicationQuit()
    {
        soData.stats.profilePageList.Clear();
    }
    private void CreateProfilePages()
    {
        for (int i = 0; i < soData.stats.profilePageList.Count; i++)
        {
            ProfilePage page = Instantiate(soData.settings.profilePagePrefab, components.profilePagesTransform);
            page.stats.pageIndex = i;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Vector3[] corners = new Vector3[4];
        components.imagesRectTransform.GetWorldCorners(corners);

        float yOffset = soData.settings.onScreenYPos;
        Vector3 worldOffset = components.imagesRectTransform.TransformVector(new Vector3(0f, soData.settings.onScreenYPos, 0f));

        for (int i = 0; i < 4; i++) corners[i] += worldOffset;

        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
    }
}
