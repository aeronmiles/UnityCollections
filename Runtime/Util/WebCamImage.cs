using UnityEngine;
using UnityEngine.UI;

public class WebCamImage : MonoBehaviour
{
    [SerializeField] Image m_cameraImage;
    [SerializeField] bool m_isFrontFacing = false;

    private WebCamTexture tex;

    private void OnEnable()
    {
        if (WebCamTextureUtil.GetWebCamTexture(out tex, 1920, 1080, m_isFrontFacing))
        {
            m_cameraImage.material.mainTexture = tex;
            tex.Play();
        }
        OrientWebCamTexture();
    }

    private void OnDisable()
    {
        if (WebCamTextureUtil.GetWebCamTexture(out var tex, 1920, 1080, m_isFrontFacing))
        {
            tex.Stop();
            m_cameraImage.material.mainTexture = null;
        }
    }

    void OrientWebCamTexture()
    {
        m_cameraImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -tex.videoRotationAngle);

        float mirrored = tex.videoVerticallyMirrored ? -1f : 1f;
        if (tex.videoRotationAngle != 0f || tex.videoRotationAngle != 180f)
        {
            m_cameraImage.rectTransform.sizeDelta = new Vector2(tex.height, tex.width);
            m_cameraImage.rectTransform.localScale = new Vector3(mirrored, 1f, 1f);
        }
        else
        {
            m_cameraImage.rectTransform.sizeDelta = new Vector2(tex.width, tex.height);
            m_cameraImage.rectTransform.localScale = new Vector3(1f, mirrored, 1f);
        }
    }
}
