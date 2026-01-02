using System;
using UnityEngine;

public class Phone : MonoBehaviour
{
    [SerializeField] SpriteRenderer phoneRenderer;
    [SerializeField] SpyStatsSO spyStats;
    [SerializeField] PlayerInputsSO playerInputs;
    [SerializeField] PhoneSO phone;
    [SerializeField] GameEventDataSO gameEventData;
    MaterialPropertyBlock phoneMPB;
    
    float curDistFromSpy;
    internal bool canHover;
    static bool playedTutorial;
    private void Awake()
    {
        phoneMPB = new MaterialPropertyBlock();
        phone.materialIDs.hoveredID = Shader.PropertyToID("_Hovered");
        phone.materialIDs.selectedID = Shader.PropertyToID("_Selected");
    }

    private void Update()
    {
        curDistFromSpy = Mathf.Abs(transform.position.x - spyStats.curWorldPos.x);
        canHover = curDistFromSpy < phone.interactDistance;
    }

    public void HoverColor()
    {
        if (phone.spyOnPhone) return;
        phoneMPB.SetFloat(phone.materialIDs.hoveredID, 1);
        phoneRenderer.SetPropertyBlock(phoneMPB);
    }

    public void ExitColor()
    {
        if (phone.spyOnPhone) return;
        phoneMPB.SetFloat(phone.materialIDs.hoveredID, 0);
        phoneMPB.SetFloat(phone.materialIDs.selectedID, 0);
        phoneRenderer.SetPropertyBlock(phoneMPB);
    }

    public void SelectColor()
    {
        phoneMPB.SetFloat(phone.materialIDs.selectedID, 1);
        phoneRenderer.SetPropertyBlock(phoneMPB);
        phone.spyOnPhone = true;
        if (!playedTutorial)
        {
            gameEventData.OnStartTutorial.Raise();
            playedTutorial = true;
        }
    }
}
