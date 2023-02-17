using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CameraDepthTexture : MonoBehaviour
{
    void Awake()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
    }
}