using UnityEngine;
using UnityEngine.UI;

public class GameCursor : MonoBehaviour
{
    [SerializeField] PlayerInputsSO spyInputs;
    [SerializeField] SpyStatsSO spyStats;
    [SerializeField] ClipboardStatsSO clipboardStats;
    [SerializeField] Texture2D cursorTexture;
    [SerializeField] LayerSettingsSO layerSettings;
    NPCBrain curNPC; 
    NPCBrain prevNPC;
    Phone curPhone;

    int prevPageIndex;
    bool prevClipboardActive;
    private void Start()
    {
        //Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
    }

    private void Update()
    {
        UpdateNPCColor();
        UpdatePhoneColor();
    }

    private void LateUpdate()
    {
        prevNPC = curNPC;
        prevPageIndex = clipboardStats.activePageIndex;
        prevClipboardActive = clipboardStats.active;
    }

    private void UpdateNPCColor()
    {
        if (spyStats.curGroundLayer != layerSettings.trainLayerStruct.ground) return;
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

    private void UpdatePhoneColor()
    {
        if (spyStats.curGroundLayer != layerSettings.trainLayerStruct.ground) return;
        RaycastHit2D phoneHit = Physics2D.Raycast(spyInputs.mouseWorldPos, Vector2.zero, Mathf.Infinity, layerSettings.phone);
        if (phoneHit.collider != null)
        {
            curPhone = phoneHit.collider.GetComponent<Phone>();
        }
        else
        {
            curPhone?.ExitColor();
            curPhone = null;
        }

        if (curPhone != null && curPhone.canHover)
        {
            if (spyInputs.mouseLeftDown)
            {
                curPhone.SelectColor();
            }
            else
            {
                curPhone.HoverColor();
            }
        }
    }
}
