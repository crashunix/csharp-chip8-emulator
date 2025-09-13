
// Injetar o renderer padrão (por enquanto é o ConsoleRenderer, to fazendo um com Veldrid kkkk)
IRenderer renderer = new ConsoleRenderer();
var chip8 = new Chip8(renderer);

// Mapeamento do teclado: Teclado do PC -> Teclado CHIP-8
var keyMap = new Dictionary<ConsoleKey, int>
{
    { ConsoleKey.D1, 0x1 }, { ConsoleKey.D2, 0x2 }, { ConsoleKey.D3, 0x3 }, { ConsoleKey.D4, 0xC },
    { ConsoleKey.Q, 0x4 }, { ConsoleKey.W, 0x5 }, { ConsoleKey.E, 0x6 }, { ConsoleKey.R, 0xD },
    { ConsoleKey.A, 0x7 }, { ConsoleKey.S, 0x8 }, { ConsoleKey.D, 0x9 }, { ConsoleKey.F, 0xE },
    { ConsoleKey.Z, 0xA }, { ConsoleKey.X, 0x0 }, { ConsoleKey.C, 0xB }, { ConsoleKey.V, 0xF }
};

ICartridge cartridge = new FileSystemCartridge();

// Flash da ROM no cartucho
cartridge.Flash(args.Length > 0 ? args[0] : "test_opcode");

// Carrega a ROM na CPU
chip8.Start(cartridge.Dump());

Console.WriteLine("Controles:");
Console.WriteLine("1 2 3 4 | Q W E R");
Console.WriteLine("A S D F | Z X C V");
Console.WriteLine("\nPressione qualquer tecla para iniciar...");
Console.ReadKey();

// Loop principal do emulador
while (true)
{
  // 2. Lógica de Teclado: Limpa, verifica e define as teclas pressionadas.
  chip8.ResetKeys();
  while (Console.KeyAvailable)
  {
    var key = Console.ReadKey(true).Key;
    if (keyMap.TryGetValue(key, out int chip8Key))
    {
      chip8.SetKey(chip8Key, true);
    }
  }

  for (int i = 0; i < 20; i++) // Executa 20 ciclos da CPU por quadro (aprox. 1200 Hz)
  {
    chip8.Cycle();
  }
  // Executa um ciclo de CPU
  chip8.Tick60Hz();
  // Controla a velocidade da emulação.
  Thread.Sleep(16);
}