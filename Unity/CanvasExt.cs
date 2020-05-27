using System.Collections.Generic;
using UnityEngine;

public static class CanvasExt
{
    public static Canvas[] Enabled(this Canvas[] canvases, bool enabled)
    {
        int l = canvases.Length;
        for (int i = 0; i < l; i++)
        {
            canvases[i].enabled = enabled;
        }

        return canvases;
    }

    public static Vector2 WorldToCanvas(this Canvas canvas, Vector3 world_position, Camera camera = null)
    {
        if (camera == null)
        {
            camera = Camera.main;
        }

        var viewport_position = camera.WorldToViewportPoint(world_position);
        var canvas_rect = canvas.GetComponent<RectTransform>();

        return new Vector2((viewport_position.x * canvas_rect.sizeDelta.x) - (canvas_rect.sizeDelta.x * 0.5f),
                            (viewport_position.y * canvas_rect.sizeDelta.y) - (canvas_rect.sizeDelta.y * 0.5f));
    }

}