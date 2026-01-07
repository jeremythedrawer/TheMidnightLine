using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class Phone : MonoBehaviour
{
    [SerializeField] SpriteRenderer phoneRenderer;
    [SerializeField] SpyStatsSO spyStats;
    [SerializeField] PlayerInputsSO playerInputs;
    [SerializeField] PhoneSO phone;
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] SpriteRenderer ringRenderer;
    [SerializeField] SpriteRenderer cordRenderer;
    [SerializeField] BoxCollider2D boxCollider;
    [SerializeField] LayerSettingsSO layerSettings;
    MaterialPropertyBlock phoneMPB;
    MaterialPropertyBlock cordMPB;
    float curDistFromSpy;
    internal bool canHover;
    static bool playedTutorial;
    private void Awake()
    {
        phoneMPB = new MaterialPropertyBlock();
        cordMPB = new MaterialPropertyBlock();
        phone.materialIDs.hoveredID = Shader.PropertyToID("_Hovered");
        phone.materialIDs.selectedID = Shader.PropertyToID("_Selected");
        phone.materialIDs.targetPositionID = Shader.PropertyToID("_TargetPosition");
    }
    private void Start()
    {
        ringRenderer.gameObject.SetActive(false);
        cordRenderer.gameObject.SetActive(false);
    }

    private void Update()
    {
        curDistFromSpy = Mathf.Abs(transform.position.x - spyStats.curWorldPos.x);
        canHover = curDistFromSpy < phone.interactDistance;
    }

    private void FixedUpdate()
    {
        RingPhone();
    }
    public void HoverColor()
    {
        if (spyStats.onPhone) return;
        phoneMPB.SetFloat(phone.materialIDs.hoveredID, 1);
        phoneRenderer.SetPropertyBlock(phoneMPB);
    }
    public void ExitColor()
    {
        if (spyStats.onPhone) return;
        phoneMPB.SetFloat(phone.materialIDs.hoveredID, 0);
        phoneMPB.SetFloat(phone.materialIDs.selectedID, 0);
        phoneRenderer.SetPropertyBlock(phoneMPB);
    }
    public void UsePhone()
    {
        spyStats.onPhone = true;
        phoneRenderer.gameObject.SetActive(false);
        ringRenderer.gameObject.SetActive(false);
        cordRenderer.gameObject.SetActive(true);
        Vector2 targetCordPosition = spyStats.curWorldPos;
        targetCordPosition.x += spyStats.spriteFlip ? -spyStats.phonePosition.x : spyStats.phonePosition.x;
        targetCordPosition.y += spyStats.phonePosition.y;
        cordMPB.SetVector(phone.materialIDs.targetPositionID, targetCordPosition);
        cordRenderer.SetPropertyBlock(cordMPB);
        gameEventData.OnStartTutorial.Raise();
    }

    private void RingPhone()
    {
        if (spyStats.curGroundLayer == layerSettings.trainLayerStruct.ground && curDistFromSpy < phone.ringDistance && !ringRenderer.gameObject.activeInHierarchy)
        {
            ringRenderer.gameObject.SetActive(true);
        }
        else if (ringRenderer.gameObject.activeInHierarchy && curDistFromSpy > phone.ringDistance)
        {
            ringRenderer.gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, phone.ringDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, phone.interactDistance);
    }
}
