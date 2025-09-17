namespace Chip8.Core.Tests;

using Xunit;
using Moq;
using Chip8.Core.CPU;
using Chip8.Core.Interfaces;
using System;

public class Chip8CpuTests
{
    private Mock<IRenderer> _mockRenderer;
    private Chip8 _cpu;

    public Chip8CpuTests()
    {
        _mockRenderer = new Mock<IRenderer>();
        _cpu = new Chip8(_mockRenderer.Object);
    }

    // Helper method to load and execute an opcode
    private void LoadAndExecuteOpcode(ushort opcode, ushort address = 0x200)
    {
        _cpu.Memory[address] = (byte)(opcode >> 8);
        _cpu.Memory[address + 1] = (byte)(opcode & 0xFF);
        // Simulate PC being at the address before execution
        // This is a hack because PC is private set. Ideally, Chip8 would have a method to set PC for testing.
        // For now, we assume PC is at 0x200 after reset and opcodes are placed there.
        _cpu.Cycle();
    }

    // Helper method to set a register Vx using LD Vx, byte opcode
    private void SetRegisterVx(byte registerIndex, byte value, ushort address = 0x200)
    {
        ushort opcode = (ushort)(0x6000 | (registerIndex << 8) | value);
        LoadAndExecuteOpcode(opcode, address);
    }

    // Helper method to set index register I using LD I, addr opcode
    private void SetIndexRegisterI(ushort value, ushort address = 0x200)
    {
        ushort opcode = (ushort)(0xA000 | value);
        LoadAndExecuteOpcode(opcode, address);
    }

    // Helper method to set DelayTimer using LD DT, Vx opcode
    private void SetDelayTimer(byte value, ushort address = 0x200)
    {
        SetRegisterVx(0, value, address); // Use V0 to set DT
        LoadAndExecuteOpcode(0xF015, (ushort)(address + 2)); // LD DT, V0
    }

    // Helper method to set SoundTimer using LD ST, Vx opcode
    private void SetSoundTimer(byte value, ushort address = 0x200)
    {
        SetRegisterVx(0, value, address); // Use V0 to set ST
        LoadAndExecuteOpcode(0xF018, (ushort)(address + 2)); // LD ST, V0
    }

    [Fact]
    public void Chip8_Initialization_SetsDefaultValues()
    {
        // Assert
        Assert.Equal(0x200, _cpu.PC);
        Assert.Equal(0, _cpu.SP);
        Assert.Equal(0, _cpu.I);
        Assert.Equal(0, _cpu.DelayTimer);
        Assert.Equal(0, _cpu.SoundTimer);
        Assert.False(_cpu.IsPaused);
        Assert.False(_cpu.DrawFlag);
        Assert.All(_cpu.Memory, b => Assert.Equal(0, b)); // Memory should be cleared except for fontset
        Assert.All(_cpu.V, b => Assert.Equal(0, b)); // Registers should be cleared
        Assert.All(_cpu.Stack, s => Assert.Equal(0, s)); // Stack should be cleared
        Assert.All(_cpu.Keys, k => Assert.False(k)); // Keys should be reset

        // Check if fontset is loaded (first few bytes)
        Assert.Equal(0xF0, _cpu.Memory[0x50]);
        Assert.Equal(0x90, _cpu.Memory[0x51]);
    }

    [Fact]
    public void Chip8_Reset_ResetsAllValuesAndClearsRenderer()
    {
        // Arrange - Manipulate state via public methods/opcodes if possible
        // Since PC, SP, I, Timers are private set, we can't directly set them for this test.
        // We rely on the fact that the constructor already calls Reset().
        // This test primarily verifies the side effects of Reset() on the renderer and fontset.
        _cpu.Memory[0x200] = 0xFF; // Simulate some memory change
        _cpu.V[0] = 0xAA; // Simulate some register change
        _cpu.Keys[5] = true; // Simulate some key state change

        // Act
        _cpu.Reset();

        // Assert
        Assert.Equal(0x200, _cpu.PC);
        Assert.Equal(0, _cpu.SP);
        Assert.Equal(0, _cpu.I);
        Assert.Equal(0, _cpu.DelayTimer);
        Assert.Equal(0, _cpu.SoundTimer);
        Assert.False(_cpu.IsPaused);
        Assert.False(_cpu.DrawFlag);
        Assert.All(_cpu.V, b => Assert.Equal(0, b));
        Assert.All(_cpu.Stack, s => Assert.Equal(0, s));
        Assert.All(_cpu.Keys, k => Assert.False(k));
        _mockRenderer.Verify(r => r.Clear(), Times.Once);
        
        // Check if fontset is reloaded after reset
        Assert.Equal(0xF0, _cpu.Memory[0x50]);
    }

    [Fact]
    public void Chip8_Opcode00E0_ClearsScreenAndSetsDrawFlag()
    {
        // Arrange
        // PC is 0x200 by default after reset
        LoadAndExecuteOpcode(0x00E0); // CLS opcode

        // Assert
        _mockRenderer.Verify(r => r.Clear(), Times.Once);
        Assert.True(_cpu.DrawFlag);
        Assert.Equal(0x202, _cpu.PC);
    }

    [Fact]
    public void Chip8_Opcode6XNN_SetsRegisterVx()
    {
        // Arrange
        // PC is 0x200 by default after reset
        LoadAndExecuteOpcode(0x612A); // LD V1, 0x2A

        // Assert
        Assert.Equal(0x2A, _cpu.V[1]);
        Assert.Equal(0x202, _cpu.PC);
    }

    [Fact]
    public void Chip8_Opcode7XNN_AddsValueToRegisterVx()
    {
        // Arrange
        // PC is 0x200 by default after reset
        SetRegisterVx(2, 0x10); // LD V2, 0x10
        LoadAndExecuteOpcode(0x7205, 0x202); // ADD V2, 0x05

        // Assert
        Assert.Equal(0x15, _cpu.V[2]);
        Assert.Equal(0x204, _cpu.PC); // PC advanced twice
    }

    [Fact]
    public void Chip8_Opcode7XNN_AddsValueToRegisterVx_OverflowsCorrectly()
    {
        // Arrange
        // PC is 0x200 by default after reset
        SetRegisterVx(3, 0xFF); // LD V3, 0xFF
        LoadAndExecuteOpcode(0x7302, 0x202); // ADD V3, 0x02

        // Assert
        Assert.Equal(0x01, _cpu.V[3]); // 0xFF + 0x02 = 0x101, wraps to 0x01
        Assert.Equal(0x204, _cpu.PC); // PC advanced twice
    }

    [Fact]
    public void Chip8_OpcodeANNN_SetsIndexRegisterI()
    {
        // Arrange
        // PC is 0x200 by default after reset
        LoadAndExecuteOpcode(0xA134); // LD I, 0x134

        // Assert
        Assert.Equal(0x134, _cpu.I);
        Assert.Equal(0x202, _cpu.PC);
    }

    [Fact]
    public void Chip8_OpcodeDXYN_SetsDrawFlagAndCallsRendererFlipPixel()
    {
        // Arrange
        // PC is 0x200 by default after reset
        SetRegisterVx(0, 0); // LD V0, 0
        SetRegisterVx(1, 0, 0x202); // LD V1, 0
        SetIndexRegisterI(0x300, 0x204); // LD I, 0x300
        _cpu.Memory[0x300] = 0b10000000; // A single pixel sprite
        LoadAndExecuteOpcode(0xD011, 0x206); // DRW V0, V1, 1

        _mockRenderer.Setup(r => r.GetPixel(It.IsAny<int>(), It.IsAny<int>())).Returns(false);

        // Assert
        Assert.True(_cpu.DrawFlag);
        _mockRenderer.Verify(r => r.FlipPixel(0, 0), Times.Once);
        Assert.Equal(0x208, _cpu.PC); // PC advanced multiple times
        Assert.Equal(0, _cpu.V[0xF]); // No collision
    }

    [Fact]
    public void Chip8_OpcodeDXYN_SetsVFOnCollision()
    {
        // Arrange
        // PC is 0x200 by default after reset
        SetRegisterVx(0, 0); // LD V0, 0
        SetRegisterVx(1, 0, 0x202); // LD V1, 0
        SetIndexRegisterI(0x300, 0x204); // LD I, 0x300
        _cpu.Memory[0x300] = 0b10000000; // A single pixel sprite
        LoadAndExecuteOpcode(0xD011, 0x206); // DRW V0, V1, 1

        _mockRenderer.Setup(r => r.GetPixel(0, 0)).Returns(true); // Simulate existing pixel

        // Assert
        Assert.True(_cpu.DrawFlag);
        _mockRenderer.Verify(r => r.FlipPixel(0, 0), Times.Once);
        Assert.Equal(0x208, _cpu.PC); // PC advanced multiple times
        Assert.Equal(1, _cpu.V[0xF]); // Collision occurred
    }

    [Fact]
    public void Chip8_Tick60Hz_DecrementsTimers()
    {
        // Arrange
        // PC is 0x200 by default after reset
        SetDelayTimer(10); // LD V0, 10; LD DT, V0
        SetSoundTimer(5, 0x204); // LD V0, 5; LD ST, V0

        // Act
        _cpu.Tick60Hz();

        // Assert
        Assert.Equal(9, _cpu.DelayTimer);
        Assert.Equal(4, _cpu.SoundTimer);
    }

    [Fact]
    public void Chip8_Tick60Hz_DoesNotDecrementTimersBelowZero()
    {
        // Arrange
        // Timers start at 0 after reset

        // Act
        _cpu.Tick60Hz();

        // Assert
        Assert.Equal(0, _cpu.DelayTimer);
        Assert.Equal(0, _cpu.SoundTimer);
    }

    [Fact]
    public void Chip8_SetKey_UpdatesKeyState()
    {
        // Arrange
        int keyIndex = 5;

        // Act
        _cpu.SetKey(keyIndex, true);

        // Assert
        Assert.True(_cpu.Keys[keyIndex]);
    }

    [Fact]
    public void Chip8_SetKey_AdvancesPCWhenWaitingForKeyPress()
    {
        // Arrange
        // PC is 0x200 by default after reset
        LoadAndExecuteOpcode(0xF00A); // Fx0A: LD Vx, K (opcode that waits for key press)
        // After Fx0A, PC should not advance until key is pressed
        Assert.Equal(0x200, _cpu.PC); 

        int keyIndex = 5;

        // Act
        _cpu.SetKey(keyIndex, true); // Press a key

        // Assert
        Assert.True(_cpu.Keys[keyIndex]);
        Assert.Equal(0x202, _cpu.PC); // PC should advance after key press
    }

    [Fact]
    public void Chip8_ResetKeys_ClearsAllKeys()
    {
        // Arrange
        _cpu.Keys[0] = true;
        _cpu.Keys[15] = true;

        // Act
        _cpu.ResetKeys();

        // Assert
        Assert.All(_cpu.Keys, k => Assert.False(k));
    }
}