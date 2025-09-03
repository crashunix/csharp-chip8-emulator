public class ConsoleRenderer : IRenderer
{
  public void DrawChip8Screen(bool[,] screen)
  {
    Console.Clear();
    for (int y = 0; y < screen.GetLength(1); y++)
    {
      for (int x = 0; x < screen.GetLength(0); x++)
      {
        Console.Write(screen[x, y] ? "█" : " ");
      }
      Console.WriteLine();
    }
  }
}