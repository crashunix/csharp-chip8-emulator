
using System;
using System.Threading;
using chip8_emulator.IO;
using chip8_emulator.Interfaces;

// Cria uma instância do provedor de entrada e renderizador.
// Como VeldridRenderer implementa ambas as interfaces, podemos usar o mesmo objeto.
var veldridImplementation = new VeldridRenderer();
IRenderer renderer = veldridImplementation;
IInputProvider inputProvider = veldridImplementation;

var chip8 = new Chip8(renderer);

ICartridge cartridge = new FileSystemCartridge();

try
{
    // Flash da ROM no cartucho
    cartridge.Flash(args.Length > 0 ? args[0] : "ibm");

    // Carrega a ROM na CPU
    chip8.Start(cartridge.Dump());

    // Loop principal do emulador
    while (inputProvider.IsActive)
    {
        // Processa eventos de entrada (teclado, fechar janela, etc.)
        inputProvider.ProcessInput(chip8);

        // Executa vários ciclos da CPU por quadro para atingir a velocidade desejada.
        // A velocidade original do CHIP-8 era de cerca de 500-1000 Hz.
        // 10 ciclos a 60 FPS = 600 Hz.
        for (int i = 0; i < 10; i++)
        {
            chip8.Cycle();
        }

        // Atualiza os timers (Delay e Sound) a 60Hz.
        chip8.Tick60Hz();

        // Pausa para manter a taxa de quadros em aproximadamente 60 FPS.
        Thread.Sleep(16);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
finally
{
    renderer.Dispose();
    Console.WriteLine("Emulator stopped.");
}