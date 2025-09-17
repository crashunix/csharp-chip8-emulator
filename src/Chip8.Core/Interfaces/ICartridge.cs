namespace Chip8.Core.Interfaces;

public interface ICartridge
{
  byte[] Dump();
  void Flash(string programName);
}
