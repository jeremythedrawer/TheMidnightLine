using UnityEngine;
using System;

public class CarriageCamBounds : MonoBehaviour
{
    public static CarriageCamBounds Instance { get; private set; }

    private BoxCollider2D Collider2D;

    public bool activeBoundary {  get; private set; }
    public float leftEdge {  get; private set; }
    public float rightEdge { get; private set; }


    private void Start()
    {
        Collider2D = this.GetComponent<BoxCollider2D>();

        activeBoundary = false;
        leftEdge = Collider2D.bounds.min.x;
        rightEdge = Collider2D.bounds.max.x;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Instance = this;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            activeBoundary = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            activeBoundary = false;
            Instance = null;
        }
    }
}
