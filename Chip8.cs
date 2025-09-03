using System;

public class Chip8
{
  public const int MEMORY_SIZE = 4096;
  public const int REGISTER_COUNT = 16;
  public const int DISPLAY_WIDTH = 64;
  public const int DISPLAY_HEIGHT = 32;

  public byte[] Memory { get; } = new byte[MEMORY_SIZE];
  public byte[] V { get; } = new byte[REGISTER_COUNT];
  public ushort I { get; private set; } = 0;
  public ushort PC { get; private set; } = 0x200;
  public ushort[] Stack { get; } = new ushort[16];
  public byte SP { get; private set; } = 0;

  public byte DelayTimer { get; private set; } = 0;
  public byte SoundTimer { get; private set; } = 0;

  public bool[] Keys { get; } = new bool[16];
  public bool[,] Display { get; } = new bool[DISPLAY_WIDTH, DISPLAY_HEIGHT];
  public IRenderer _renderer { get; private set; }

  public bool IsPaused { get; private set; } = false;
  public bool DrawFlag { get; private set; } = false;

  private bool _waitingForKeyPress = false;

  public Chip8(IRenderer renderer)
  {
    _renderer = renderer;
    Reset();
  }

  private void InterpretsInstruction(ushort opcode)
  {
    switch (opcode & 0xF000)
    {
      case 0x0000:
        switch (opcode & 0x00FF)
        {
          case 0x00E0: // CLS
            Array.Clear(Display, 0, Display.Length);
            DrawFlag = true;
            PC += 2;
            break;
          case 0x00EE: // RET
            PC = Stack[--SP];
            PC += 2;
            break;
          default:
            PC += 2;
            break;
        }
        break;
      case 0x1000: // JP addr
        PC = (ushort)(opcode & 0x0FFF);
        break;
      case 0x2000: // CALL addr
        Stack[SP++] = PC;
        PC = (ushort)(opcode & 0x0FFF);
        break;
      case 0x3000: // SE Vx, byte
        if (V[(opcode & 0x0F00) >> 8] == (byte)(opcode & 0x00FF))
        {
          PC += 4;
        }
        else
        {
          PC += 2;
        }
        break;
      case 0x4000: // SNE Vx, byte
        if (V[(opcode & 0x0F00) >> 8] != (byte)(opcode & 0x00FF))
        {
          PC += 4;
        }
        else
        {
          PC += 2;
        }
        break;
      case 0x5000: // SE Vx, Vy
        if (V[(opcode & 0x0F00) >> 8] == V[(opcode & 0x00F0) >> 4])
        {
          PC += 4;
        }
        else
        {
          PC += 2;
        }
        break;
      case 0x6000: // LD Vx, byte
        V[(opcode & 0x0F00) >> 8] = (byte)(opcode & 0x00FF);
        PC += 2;
        break;
      case 0x7000: // ADD Vx, byte
        V[(opcode & 0x0F00) >> 8] += (byte)(opcode & 0x00FF);
        PC += 2;
        break;
      case 0x8000:
        switch (opcode & 0x000F)
        {
          case 0x0000: // LD Vx, Vy
            V[(opcode & 0x0F00) >> 8] = V[(opcode & 0x00F0) >> 4];
            PC += 2;
            break;
          case 0x0001: // OR Vx, Vy
            V[(opcode & 0x0F00) >> 8] |= V[(opcode & 0x00F0) >> 4];
            PC += 2;
            break;
          case 0x0002: // AND Vx, Vy
            V[(opcode & 0x0F00) >> 8] &= V[(opcode & 0x00F0) >> 4];
            PC += 2;
            break;
          case 0x0003: // XOR Vx, Vy
            V[(opcode & 0x0F00) >> 8] ^= V[(opcode & 0x00F0) >> 4];
            PC += 2;
            break;
          case 0x0004: // ADD Vx, Vy
            {
              int sum = V[(opcode & 0x0F00) >> 8] + V[(opcode & 0x00F0) >> 4];
              V[0xF] = (byte)(sum > 255 ? 1 : 0);
              V[(opcode & 0x0F00) >> 8] = (byte)(sum & 0xFF);
              PC += 2;
            }
            break;
          case 0x0005: // SUB Vx, Vy
            {
              byte Vx = V[(opcode & 0x0F00) >> 8];
              byte Vy = V[(opcode & 0x00F0) >> 4];
              V[0xF] = (byte)(Vx > Vy ? 1 : 0);
              V[(opcode & 0x0F00) >> 8] = (byte)(Vx - Vy);
              PC += 2;
            }
            break;
          case 0x0006: // SHR Vx {, Vy}
            V[0xF] = (byte)(V[(opcode & 0x0F00) >> 8] & 0x1);
            V[(opcode & 0x0F00) >> 8] >>= 1;
            PC += 2;
            break;
          case 0x0007: // SUBN Vx, Vy
            {
              byte Vx = V[(opcode & 0x0F00) >> 8];
              byte Vy = V[(opcode & 0x00F0) >> 4];
              V[0xF] = (byte)(Vy > Vx ? 1 : 0);
              V[(opcode & 0x0F00) >> 8] = (byte)(Vy - Vx);
              PC += 2;
            }
            break;
          case 0x000E: // SHL Vx {, Vy}
            V[0xF] = (byte)((V[(opcode & 0x0F00) >> 8] & 0x80) >> 7);
            V[(opcode & 0x0F00) >> 8] <<= 1;
            PC += 2;
            break;
          default:
            PC += 2;
            break;
        }
        break;
      case 0x9000: // SNE Vx, Vy
        if (V[(opcode & 0x0F00) >> 8] != V[(opcode & 0x00F0) >> 4])
        {
          PC += 4;
        }
        else
        {
          PC += 2;
        }
        break;
      case 0xA000: // LD I, addr
        I = (ushort)(opcode & 0x0FFF);
        PC += 2;
        break;
      case 0xB000: // JP V0, addr
        PC = (ushort)((opcode & 0x0FFF) + V[0]);
        break;
      case 0xC000: // RND Vx, byte
        {
          Random rand = new Random();
          V[(opcode & 0x0F00) >> 8] = (byte)(rand.Next(0, 256) & (opcode & 0x00FF));
          PC += 2;
        }
        break;
      case 0xD000: // DRW Vx, Vy, nibble
        {
          byte Vx = V[(opcode & 0x0F00) >> 8];
          byte Vy = V[(opcode & 0x00F0) >> 4];
          byte height = (byte)(opcode & 0x000F);
          V[0xF] = 0;
          for (int y = 0; y < height; y++)
          {
            byte pixel = Memory[I + y];
            for (int x = 0; x < 8; x++)
            {
              if ((pixel & (0x80 >> x)) != 0)
              {
                int pixelX = (Vx + x) % DISPLAY_WIDTH;
                int pixelY = (Vy + y) % DISPLAY_HEIGHT;

                if (Display[pixelX, pixelY])
                {
                  V[0xF] = 1;
                }
                Display[pixelX, pixelY] ^= true;
              }
            }
          }
          DrawFlag = true;
          PC += 2;
        }
        break;
      case 0xE000:
        switch (opcode & 0x00FF)
        {
          case 0x009E: // SKP Vx
            if (Keys[V[(opcode & 0x0F00) >> 8]])
            {
              PC += 4;
            }
            else
            {
              PC += 2;
            }
            break;
          case 0x00A1: // SKNP Vx
            if (!Keys[V[(opcode & 0x0F00) >> 8]])
            {
              PC += 4;
            }
            else
            {
              PC += 2;
            }
            break;
          default:
            PC += 2;
            break;
        }
        break;
      case 0xF000:
        switch (opcode & 0x00FF)
        {
          case 0x0007: // LD Vx, DT
            V[(opcode & 0x0F00) >> 8] = DelayTimer;
            PC += 2;
            break;
          case 0x000A: // LD Vx, K
            _waitingForKeyPress = true;
            break;
          case 0x0015: // LD DT, Vx
            DelayTimer = V[(opcode & 0x0F00) >> 8];
            PC += 2;
            break;
          case 0x0018: // LD ST, Vx
            SoundTimer = V[(opcode & 0x0F00) >> 8];
            PC += 2;
            break;
          case 0x001E: // ADD I, Vx
            I += V[(opcode & 0x0F00) >> 8];
            PC += 2;
            break;
          case 0x0029: // LD F, Vx
            I = (ushort)(V[(opcode & 0x0F00) >> 8] * 5);
            PC += 2;
            break;
          case 0x0033: // LD B, Vx
            {
              byte value = V[(opcode & 0x0F00) >> 8];
              Memory[I] = (byte)(value / 100);
              Memory[I + 1] = (byte)((value / 10) % 10);
              Memory[I + 2] = (byte)(value % 10);
              PC += 2;
            }
            break;
          case 0x0055: // LD [I], Vx
            {
              byte x = (byte)((opcode & 0x0F00) >> 8);
              for (int i = 0; i <= x; i++)
              {
                Memory[I + i] = V[i];
              }
              PC += 2;
            }
            break;
          case 0x0065: // LD Vx, [I]
            {
              byte x = (byte)((opcode & 0x0F00) >> 8);
              for (int i = 0; i <= x; i++)
              {
                V[i] = Memory[I + i];
              }
              PC += 2;
            }
            break;
          default:
            PC += 2;
            break;
        }
        break;
      default:
        PC += 2;
        break;
    }
  }

  private void LoadSpritesIntoMemory()
  {
    byte[] chip8Fontset =
    {
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80  // F
        };
    Array.Copy(chip8Fontset, 0, Memory, 0x050, chip8Fontset.Length);
  }

  private void UpdateTimers()
  {
    if (DelayTimer > 0)
    {
      DelayTimer--;
    }
    if (SoundTimer > 0)
    {
      SoundTimer--;
    }
  }

  private void LoadProgram(byte[] program)
  {
    Array.Copy(program, 0, Memory, 0x200, program.Length);
  }

  public void Reset()
  {
    Array.Clear(Memory, 0, Memory.Length);
    Array.Clear(V, 0, V.Length);
    Array.Clear(Stack, 0, Stack.Length);
    Array.Clear(Keys, 0, Keys.Length);
    Array.Clear(Display, 0, Display.Length);
    I = 0;
    PC = 0x200;
    SP = 0;
    DelayTimer = 0;
    SoundTimer = 0;
    IsPaused = false;
    LoadSpritesIntoMemory();
  }

  public void SetKey(int keyIndex, bool isPressed)
  {
    Keys[keyIndex] = isPressed;
    if (isPressed && _waitingForKeyPress)
    {
      _waitingForKeyPress = false;
      PC += 2;
    }
  }

  private void Cycle()
  {
    if (!IsPaused && !_waitingForKeyPress)
    {
      ushort opcode = (ushort)((Memory[PC] << 8) | Memory[PC + 1]);
      InterpretsInstruction(opcode);
      UpdateTimers();
    }
  }

  public void Start(byte[] program)
  {
    LoadProgram(program);

    while (true)
    {
      Cycle();

      // Renderiza o display se necessário
      // É importante que a lógica de renderização esteja aqui para interagir com o ambiente
      if (DrawFlag)
      {
        // Aqui você chamaria seu método de renderização da classe externa
        _renderer.DrawChip8Screen(Display);
        DrawFlag = false;
      }

      // Atraso para controlar a taxa de emulação
      // Este valor pode ser ajustado para simular diferentes velocidades
      Thread.Sleep(1);
    }
  }
}