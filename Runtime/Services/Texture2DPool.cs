using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Cache to store and retrieve re-usable Texture2D references
/// </summary>
public class Texture2DPool : IFactoryPool<Texture2DPool, Texture2D>
{
  private int _width;
  private int _height;
  private TextureFormat _format;
  private bool _mipChain;
  private bool _linear;

  private static FactoryPool<Texture2DPool, Texture2D> _Pools = new();

  public Texture2DPool(RenderTexture rt, bool linear)
  {
    this._width = rt.width;
    this._height = rt.height;
    this._format = rt.ToTextureFormat();
    this._mipChain = rt.useMipMap;
    this._linear = linear;
  }

  public Texture2DPool(int width, int height, TextureFormat format, bool mipChain, bool linear)
  {
    this._width = width;
    this._height = height;
    this._format = format;
    this._mipChain = mipChain;
    this._linear = linear;
  }

  public Texture2D Create()
  {
    var tex = new Texture2D(this._width, this._height, this._format, this._mipChain, this._linear);
    // tex.name = $"Texture2DPool::Create::tex";
    return tex;
  }

  public Texture2D Get() => _Pools.Get(this);

  public void Release(Texture2D texture)
  {
    _Pools.Release(texture, this);
  }

  public bool Equals(Texture2DPool other)
  {
    return this.GetHashCode() == other.GetHashCode();
  }

  public override int GetHashCode()
  {
    unchecked // Overflow is fine, just wrap
    {
      int hash = 17;
      hash = (hash * 23) + this._width.GetHashCode();
      hash = (hash * 23) + this._height.GetHashCode();
      hash = (hash * 23) + this._format.GetHashCode();
      hash = (hash * 23) + this._mipChain.GetHashCode();
      hash = (hash * 23) + this._linear.GetHashCode();
      return hash;
    }
  }
}

