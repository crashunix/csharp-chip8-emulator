namespace Chip8.Core.Execution;

using System.Threading;
using Chip8.Core.CPU;
using Chip8.Core.Interfaces;

/// <summary>
/// Gera os pulsos de clock que comandam o loop principal do emulador,
/// sincronizando os ciclos da CPU, timers e input.
/// </summary>
public class Oscillator
{
    private readonly Chip8 _cpu;
    private readonly IInputProvider _inputProvider;
    private readonly int _cyclesPerTick;

    /// <summary>
    /// Inicializa uma nova instância do Oscilador.
    /// </summary>
    /// <param name="cpu">A instância da CPU Chip8 a ser comandada.</param>
    /// <param name="inputProvider">O provedor de input do usuário.</param>
    /// <param name="frequencyHz">A frequência alvo da CPU em Hertz.</param>
    public Oscillator(Chip8 cpu, IInputProvider inputProvider, int frequencyHz = 600)
    {
        _cpu = cpu;
        _inputProvider = inputProvider;
        // O loop principal roda a 60Hz (aprox. 16ms de sleep).
        // Logo, ciclos por tick = frequência alvo / 60.
        _cyclesPerTick = frequencyHz / 60;
    }

    /// <summary>
    /// Inicia o loop principal da emulação e o executa até que o provedor de input não esteja mais ativo.
    /// </summary>
    public void Run()
    {
        while (_inputProvider.IsActive)
        {
            // Processa qualquer input pendente do usuário.
            _inputProvider.ProcessInput(_cpu);

            // Executa um lote de ciclos da CPU para atingir a frequência alvo.
            for (int i = 0; i < _cyclesPerTick; i++)
            {
                _cpu.Cycle();
            }

            // Atualiza os timers de delay e som a uma frequência constante de 60Hz.
            _cpu.Tick60Hz();

            // Pausa para manter uma taxa de atualização consistente de 60Hz.
            Thread.Sleep(16);
        }
    }
}
