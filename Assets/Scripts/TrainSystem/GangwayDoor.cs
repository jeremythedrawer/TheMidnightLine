using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;

public class GangwayDoor : MonoBehaviour
{
    [SerializeField] BoxCollider2D boxCollider;
    [SerializeField] Animator animator;
    [SerializeField] LayerSettingsSO layerSettings;
    [Serializable] public struct AnimClipData
    {
        internal int openHash;
        internal int closeHash;
        public AnimationClip openClip;
    }
    [SerializeField] internal AnimClipData animClipData;

    [Serializable] public struct StatData
    {
        internal bool canClose;
    }
    [SerializeField] StatData statData;

    private void Awake()
    {
        animClipData.openHash = Animator.StringToHash("Open");
        animClipData.closeHash = Animator.StringToHash("Close");

        AnimationUtilities.SetAnimationEvent(animClipData.openClip, nameof(EnableCloseDoor));
    }

    private void FixedUpdate()
    {     
        if (statData.canClose)
        {
            Collider2D spyHit = Physics2D.OverlapBox(boxCollider.bounds.center, boxCollider.bounds.size, 0, layerSettings.spy);

            if (spyHit == null)
            {
                animator.Play(animClipData.closeHash);
                boxCollider.isTrigger = false;
                statData.canClose = false;
            }
        }
    }
    public void OpenDoor()
    {
        boxCollider.isTrigger = true;
        animator.Play(animClipData.openHash);
    }

    private void EnableCloseDoor()
    {
        statData.canClose = true;
    }
}
