using UnityEngine;

/// <summary>
/// Cache to store and retrieve re-usable Texture2D references
/// </summary>
public class RenderTexturePool : IFactoryPool<RenderTexturePool, RenderTexture>
{
  private int _width;
  private int _height;
  private RenderTextureFormat _format;
  private int _depth;
  private int _mipCount;
  private static readonly FactoryPool<RenderTexturePool, RenderTexture> _Pools = new();

  public RenderTexture Get(int width, int height, int mipCount, int depth, RenderTextureFormat format)
  {
    this._width = width;
    this._height = height;
    this._format = format;
    this._depth = depth;
    this._mipCount = mipCount;
    return _Pools.Get(this);
  }

  public RenderTexture Create()
  {
    var rt = new RenderTexture(this._width, this._height, this._depth, this._format, this._mipCount)
    {
    };
    rt.name = $"RenderTexturePool::Create:rt";
    return rt;
  }

  public bool Equals(RenderTexturePool rtt)
  {
    return this.GetHashCode() == rtt.GetHashCode();
  }
  public override int GetHashCode()
  {
    unchecked // Overflow is fine, just wrap
    {
      int hash = 17;
      hash = (hash * 23) + this._width.GetHashCode();
      hash = (hash * 23) + this._height.GetHashCode();
      hash = (hash * 23) + this._format.GetHashCode();
      hash = (hash * 23) + this._depth.GetHashCode();
      hash = (hash * 23) + this._mipCount.GetHashCode();
      return hash;
    }
  }

  public void Release(RenderTexture obj) => _Pools.Release(obj, this);
  public RenderTexture Get() => throw new System.NotImplementedException();
}
