public interface ICartridge
{
  byte[] Dump();
  void Flash(string programName);
}