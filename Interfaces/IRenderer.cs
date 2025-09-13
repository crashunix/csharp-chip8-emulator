public interface IRenderer
{
  ushort Width => 64;
  ushort Height => 32;

  void Clear();
  bool GetPixel(int x, int y);
  void FlipPixel(int x, int y);
  void RenderFrame();
}