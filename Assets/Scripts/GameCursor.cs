using UnityEngine;
using UnityEngine.UI;

public class GameCursor : MonoBehaviour
{
    [SerializeField] SpyInputsSO spyInputs;
    [SerializeField] ClipboardStatsSO clipboardStats;
    [SerializeField] Texture2D cursorTexture;
    [SerializeField] LayerSettingsSO layerSettings;
    NPCBrain curNPC; 
    NPCBrain prevNPC;

    int prevPageIndex;
    bool prevClipboardActive;
    private void Start()
    {
        //Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
    }

    private void Update()
    {
        RaycastHit2D npcHit = Physics2D.Raycast(spyInputs.mouseWorldPos, Vector2.zero, Mathf.Infinity, layerSettings.npc);

        if (npcHit.collider != null)
        {

            curNPC = npcHit.collider.GetComponent<NPCBrain>();
            if (spyInputs.mouseLeftDown)
            {
                curNPC.SelectColor();
            }
        }
        else
        {
            curNPC = null;
        }

        if (prevNPC != null && curNPC != prevNPC)
        {
            prevNPC.ExitColor();
        }

        if ((curNPC != null && curNPC != prevNPC) || clipboardStats.activePageIndex != prevPageIndex || clipboardStats.active != prevClipboardActive)
        {
            curNPC?.HoverColor();
        }

    }

    private void LateUpdate()
    {
        prevNPC = curNPC;
        prevPageIndex = clipboardStats.activePageIndex;
        prevClipboardActive = clipboardStats.active;
    }
}
