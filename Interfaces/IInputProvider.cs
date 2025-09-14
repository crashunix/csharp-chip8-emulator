namespace chip8_emulator.Interfaces
{
    public interface IInputProvider
    {
        bool IsActive { get; }
        void ProcessInput(Chip8 cpu);
    }
}
