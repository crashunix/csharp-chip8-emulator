namespace Chip8.Console.IO;

using Chip8.Core.CPU;
using Chip8.Core.Interfaces;
using System;
using System.Collections.Generic;

public class ConsoleRenderer : IRenderer, IInputProvider
{
  private bool[,] Display = new bool[64, 32];

  private readonly Dictionary<ConsoleKey, int> _keyMap = new Dictionary<ConsoleKey, int>
  {
      { ConsoleKey.D1, 0x1 }, { ConsoleKey.D2, 0x2 }, { ConsoleKey.D3, 0x3 }, { ConsoleKey.D4, 0xC },
      { ConsoleKey.Q, 0x4 }, { ConsoleKey.W, 0x5 }, { ConsoleKey.E, 0x6 }, { ConsoleKey.R, 0xD },
      { ConsoleKey.A, 0x7 }, { ConsoleKey.S, 0x8 }, { ConsoleKey.D, 0x9 }, { ConsoleKey.F, 0xE },
      { ConsoleKey.Z, 0xA }, { ConsoleKey.X, 0x0 }, { ConsoleKey.C, 0xB }, { ConsoleKey.V, 0xF }
  };

  public bool IsActive => true;

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

  public void ProcessInput(Chip8 cpu)
  {
    if (Console.KeyAvailable)
    {
        var key = Console.ReadKey(true).Key;
        if (_keyMap.TryGetValue(key, out int chip8Key))
        {
            cpu.SetKey(chip8Key, true);
        }
    }
    else
    {
        cpu.ResetKeys();
    }
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

  public void Dispose()
  {
    // nada para fazer aqui papai
  }
}