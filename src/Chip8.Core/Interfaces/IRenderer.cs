namespace Chip8.Core.Interfaces;

using System;

public interface IRenderer : IDisposable
{
  static ushort Width => 64;
  static ushort Height => 32;

  void Clear();
  bool GetPixel(int x, int y);
  void FlipPixel(int x, int y);
  void RenderFrame();
}
