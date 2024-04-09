using UnityEngine;
using UnityEngine.UI;

public class WebCamImage : MonoBehaviour
{
  [SerializeField] private Image _cameraImage;
  [SerializeField] private bool _isFrontFacing = false;

  private WebCamTexture tex;

  private void OnEnable()
  {
    if (WebCamTextureUtil.GetWebCamTexture(out tex, 1920, 1080, _isFrontFacing))
    {
      _cameraImage.material.mainTexture = tex;
      tex.Play();
    }
    OrientWebCamTexture();
  }

  private void OnDisable()
  {
    if (WebCamTextureUtil.GetWebCamTexture(out var tex, 1920, 1080, _isFrontFacing))
    {
      tex.Stop();
      _cameraImage.material.mainTexture = null;
    }
  }

  private void OrientWebCamTexture()
  {
    _cameraImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -tex.videoRotationAngle);

    float mirrored = tex.videoVerticallyMirrored ? -1f : 1f;
    if (tex.videoRotationAngle != 0f || tex.videoRotationAngle != 180f)
    {
      _cameraImage.rectTransform.sizeDelta = new Vector2(tex.height, tex.width);
      _cameraImage.rectTransform.localScale = new Vector3(mirrored, 1f, 1f);
    }
    else
    {
      _cameraImage.rectTransform.sizeDelta = new Vector2(tex.width, tex.height);
      _cameraImage.rectTransform.localScale = new Vector3(1f, mirrored, 1f);
    }
  }
}
