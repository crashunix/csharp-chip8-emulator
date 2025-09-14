public class FileSystemCartridge : ICartridge
{
  private byte[] _rom = [];
  private bool _isFlashed = false;

  public void Flash(string path)
  {
    if (path == null)
    {
      throw new ArgumentNullException(nameof(path), "Path to ROM cannot be null");
    }

    var programName = path;
    var programPath = $"./Roms/{programName}.ch8";

    Console.WriteLine($"Flashing ROM from path: {programPath}");

    if (!File.Exists(programPath))
    {
      throw new FileNotFoundException($"ROM file not found: {programPath}");
    }

    // Carrega a ROM
    byte[] programData = File.ReadAllBytes(programPath);
    _rom = programData;
    if (_rom.Length == 0)
    {
      throw new InvalidOperationException("Failed to flash ROM on your cartridge. The ROM file is empty.");
    }
    _isFlashed = true;
  }

  public byte[] Dump()
  {
    if (!_isFlashed)
    {
      throw new InvalidOperationException("No ROM has been flashed. Your cartridge is empty.");
    }
    return _rom;
  }

}