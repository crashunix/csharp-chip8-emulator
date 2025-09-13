# CHIP-8 Emulator in C#

This project is my first step into the fascinating world of computer emulation. As a learning exercise, I decided to build a CHIP-8 interpreter to understand the fundamental concepts of a virtual machine, including CPU cycles, memory management, registers, and input/output handling.

CHIP-8 is an interpreted programming language from the 1970s. It was designed to make game programming easier on simple 8-bit microcomputers of the time. Its simplicity makes it an ideal starting point for anyone interested in learning about emulators.

## Project Structure

The project is organized into several classes and interfaces, each with a distinct responsibility.

### `IRenderer.cs`
This interface defines the contract for any class that can render the CHIP-8 display. It decouples the core emulation logic from the specifics of how the screen is drawn.

### `ConsoleRenderer.cs`
A simple implementation of `IRenderer` that draws the CHIP-8 display directly into the console window. It uses block characters to represent pixels, providing a straightforward way to visualize the output without needing a complex graphics library.

### `ICartridge.cs`
This interface abstracts the concept of a game cartridge. It defines how the emulator interacts with a ROM, separating the logic of loading the game data from the CPU itself.
- **`void Flash(string romName)`:** Loads the game data from a source (like a file) into the cartridge.
- **`byte[] Dump()`:** Provides the raw game data to be loaded into the emulator's memory.

### `FileSystemCartridge.cs`
An implementation of `ICartridge` that reads ROM files from the local filesystem. It looks for `.ch8` files inside the `roms/` directory.

### `Chip8.cs`
This is the heart of the emulator—the CPU itself. It is responsible for:
- **Memory:** Managing the 4KB of RAM.
- **Registers:** Handling the 16 general-purpose 8-bit registers (V0-VF), the 16-bit index register (I), and the program counter (PC).
- **Stack:** Managing subroutine calls with a 16-level stack.
- **Opcodes:** Fetching, decoding, and executing all 35 CHIP-8 opcodes.
- **Timers:** Managing the delay and sound timers, which decrement at 60Hz.

### `Program.cs`
The entry point of the application. It is responsible for:
- Initializing and wiring together all the components (CPU, Renderer, Cartridge).
- Handling the main emulation loop.
- Capturing keyboard input and mapping it to the CHIP-8 keypad.
- Controlling the speed of the emulation.

## How to Run
You will need the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or a compatible version.

1. Clone the repository.
2. Open your terminal in the project's root directory.
3. Run the default test ROM:
   ```bash
   dotnet run
   ```
4. To run a specific game from the `roms/` folder, pass its name as an argument (without the extension):
   ```bash
   dotnet run ibm
   ```

## Controls
The CHIP-8's 16-key hexadecimal keypad is mapped to a standard QWERTY keyboard as follows:

| CHIP-8 Keypad | PC Keyboard |
|---------------|-------------|
| `1 2 3 C`     | `1 2 3 4`     |
| `4 5 6 D`     | `Q W E R`     |
| `7 8 9 E`     | `A S D F`     |
| `A 0 B F`     | `Z X C V`     |

## Next Steps: The Road Ahead

This project is just the beginning of my journey. After solidifying my understanding of these core concepts, my goal is to tackle more complex and historic architectures. My learning path includes:

1.  **Z80 CPU Emulator:** To understand a more complex instruction set and the architecture that powered legendary systems like the Sinclair ZX Spectrum, and the original Game Boy.
2.  **TMS9918 Video Display Processor:** To learn about video hardware, sprite handling, and color palettes by emulating the VDP used in machines like the ColecoVision and MSX computers.

Thank you for checking out my project!