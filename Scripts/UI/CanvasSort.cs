using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasSort : MonoBehaviour
{
    private Canvas canvas;
    [SerializeField] private int sortingLayer;
    [SerializeField] private bool dontDestroyOnLoad = false;

    private void Awake()
    {
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void Start()
    {
        canvas = GetComponent<Canvas>();
        canvas.sortingOrder = sortingLayer;
    }
}
