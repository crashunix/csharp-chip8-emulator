namespace Chip8.Console;

using System;
using Chip8.Core.CPU;
using Chip8.Core.Execution;
using Chip8.Console.IO;
using Chip8.Core.Interfaces;
using Chip8.Infrastructure.IO;

class Program
{
    static void Main(string[] args)
    {
        // 1. Montar os componentes para a interface de console
        var uiImplementation = new ConsoleRenderer();
        IRenderer renderer = uiImplementation;
        IInputProvider inputProvider = uiImplementation;

        var cpu = new Chip8(renderer);
        ICartridge cartridge = new FileSystemCartridge(); // Use FileSystemCartridge from Infrastructure

        try
        {
            // 2. Carregar a ROM
            Console.WriteLine("Initializing CHIP-8 Emulator (Console Mode)...");
            cartridge.Flash(args.Length > 0 ? args[0] : "br8kout");
            cpu.Start(cartridge.Dump());

            // 3. Montar e executar o Oscillator, que agora contém o loop principal
            var oscillator = new Oscillator(cpu, inputProvider);
            oscillator.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            // 4. Limpeza
            renderer.Dispose();
            Console.WriteLine("Emulator stopped.");
        }

        Console.WriteLine("Emulator has shut down. Press any key to exit.");
        Console.ReadKey();
    }
}
