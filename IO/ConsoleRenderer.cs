public class ConsoleRenderer : IRenderer
{
  private bool[,] Display = new bool[64, 32];
  public void Clear()
  {
    Array.Clear(Display, 0, Display.Length);
  }

  public bool GetPixel(int x, int y)
  {
    return Display[x, y];
  }

  public void FlipPixel(int x, int y)
  {
    Display[x, y] ^= true;
  }

  public void RenderFrame()
  {
    Console.Clear();
    for (int y = 0; y < Display.GetLength(1); y++)
    {
      for (int x = 0; x < Display.GetLength(0); x++)
      {
        Console.Write(Display[x, y] ? "█" : " ");
      }
      Console.WriteLine();
    }
  }
}