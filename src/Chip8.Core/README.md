# Crashunix.Chip8.Core

A simple, dependency-free Chip-8 emulator core library for .NET.

This package provides the essential CPU logic for Chip-8 emulation. It is designed to be backend-agnostic, allowing you to integrate it into various host applications (Console, GUI, Web, etc.) by implementing a few simple interfaces.

## Usage

To run the emulator, you need to provide implementations for the `IRenderer` and `IInputProvider` interfaces. These are then passed to the `Chip8` CPU and the `Oscillator` class, which manages the main emulation loop.

### 1. Implement the Renderer

This class handles all the drawing logic for your specific application (e.g., writing to the console, drawing on a canvas).

```csharp
using Chip8.Core.Interfaces;
using System;

public class MyConsoleRenderer : IRenderer
{
    private bool[,] _display = new bool[IRenderer.Width, IRenderer.Height];

    public void Clear() => Array.Clear(_display, 0, _display.Length);
    public void FlipPixel(int x, int y) => _display[x, y] = !_display[x, y];
    public bool GetPixel(int x, int y) => _display[x, y];
    public void Dispose() { /* Cleanup, if needed */ }

    public void RenderFrame()
    {
        Console.SetCursorPosition(0, 0);
        for (int y = 0; y < IRenderer.Height; y++)
        {
            for (int x = 0; x < IRenderer.Width; x++)
            {
                Console.Write(_display[x, y] ? '█' : ' ');
            }
            Console.WriteLine();
        }
    }
}
```

### 2. Implement the Input Provider

This class is responsible for capturing user input and controlling the main loop. The `Oscillator` will call `ProcessInput` repeatedly and will stop when `IsActive` becomes `false`.

```csharp
using Chip8.Core.CPU;
using Chip8.Core.Interfaces;
using System;
using System.Collections.Generic;

public class MyConsoleInputProvider : IInputProvider
{
    public bool IsActive { get; private set; } = true;

    // Map PC keyboard keys to Chip-8's 16-key layout
    private static readonly Dictionary<ConsoleKey, int> KeyMap = new Dictionary<ConsoleKey, int>
    {
        { ConsoleKey.D1, 0x1 }, { ConsoleKey.D2, 0x2 }, { ConsoleKey.D3, 0x3 }, { ConsoleKey.D4, 0xC },
        { ConsoleKey.Q, 0x4 }, { ConsoleKey.W, 0x5 }, { ConsoleKey.E, 0x6 }, { ConsoleKey.R, 0xD },
        { ConsoleKey.A, 0x7 }, { ConsoleKey.S, 0x8 }, { ConsoleKey.D, 0x9 }, { ConsoleKey.F, 0xE },
        { ConsoleKey.Z, 0xA }, { ConsoleKey.X, 0x0 }, { ConsoleKey.C, 0xB }, { ConsoleKey.V, 0xF }
    };

    public void ProcessInput(Chip8 cpu)
    {
        if (!Console.KeyAvailable) return;

        var key = Console.ReadKey(true).Key;

        if (key == ConsoleKey.Escape)
        {
            IsActive = false;
            return;
        }

        cpu.ResetKeys();
        if (KeyMap.TryGetValue(key, out int chip8Key))
        {
            cpu.SetKey(chip8Key, true);
        }
    }
}
```

### 3. Initialize and Run the Emulator

Finally, instantiate your classes, load a ROM, and start the oscillator.

```csharp
using Chip8.Core.CPU;
using Chip8.Core.Execution;
using System.IO;

// 1. Create instances of your implementations
var renderer = new MyConsoleRenderer();
var inputProvider = new MyConsoleInputProvider();

// 2. Create the Chip-8 CPU instance
var chip8 = new Chip8(renderer);

// 3. Create the Oscillator to drive the emulation
// It will run the CPU at ~600Hz by default
var oscillator = new Oscillator(chip8, inputProvider);

// 4. Load a ROM from a file
byte[] rom = File.ReadAllBytes("path/to/your/rom.ch8");
chip8.Start(rom);

// 5. Run the emulation!
// The Oscillator's Run() method contains the entire loop
// and will exit when IsActive on the input provider is false.
Console.Clear();
oscillator.Run();

Console.WriteLine("Emulator stopped.");
```
