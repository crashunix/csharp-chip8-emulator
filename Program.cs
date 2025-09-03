var chip8 = new Chip8(new ConsoleRenderer());

byte[] programData = System.IO.File.ReadAllBytes("roms/ibm.ch8");
chip8.Start(programData);