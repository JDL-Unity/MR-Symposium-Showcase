using System;
using UnityEngine;

public class DebugCube : MonoBehaviour
{
    public float height;

    public void Awake()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
        }
    }
}