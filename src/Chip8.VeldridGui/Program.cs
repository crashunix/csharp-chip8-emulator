namespace Chip8.VeldridGui;

using System;
using Chip8.Core.CPU;
using Chip8.Core.Execution;
using Chip8.VeldridGui.IO;
using Chip8.Infrastructure.IO;
using Chip8.Core.Interfaces;

class Program
{
    static void Main(string[] args)
    {
        // 1. Montar os componentes
        // Como VeldridRenderer (ou ConsoleRenderer) implementa ambas as interfaces, 
        // podemos usar o mesmo objeto para renderer e input.
        var uiImplementation = new VeldridRenderer();
        IRenderer renderer = uiImplementation;
        IInputProvider inputProvider = uiImplementation;

        var cpu = new Chip8(renderer);
        ICartridge cartridge = new FileSystemCartridge();

        try
        {
            // 2. Carregar a ROM
            Console.WriteLine("Initializing CHIP-8 Emulator...");
            cartridge.Flash(args.Length > 0 ? args[0] : "br8kout");
            cpu.Start(cartridge.Dump());

            // 3. Montar e executar o Oscillator, que agora tem o loop principal, pique oscilador mesmo kkkkkk
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
