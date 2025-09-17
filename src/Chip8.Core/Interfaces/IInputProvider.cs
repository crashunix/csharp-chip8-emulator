namespace Chip8.Core.Interfaces;

using Chip8.Core.CPU;

public interface IInputProvider
{
    bool IsActive { get; }
    void ProcessInput(Chip8 cpu);
}
