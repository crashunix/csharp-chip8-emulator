public interface IRenderer
{
  void Clear();
  bool GetPixel(int x, int y);
  void FlipPixel(int x, int y);
  void RenderFrame();
}