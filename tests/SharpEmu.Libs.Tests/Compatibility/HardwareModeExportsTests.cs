// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.HLE;
using Xunit;

namespace SharpEmu.Libs.Tests.Compatibility;

public sealed class HardwareModeExportsTests
{
    private const ulong MemoryBase = 0x1_0000_0000;

    [Fact]
    public void AgcGetIsTrinityMode_WritesFalseAndReturnsOk()
    {
        var manager = CreateRegisteredManager();
        var memory = new FakeCpuMemory(MemoryBase, 0x1000);
        var ctx = new CpuContext(memory, Generation.Gen5);
        ctx[CpuRegister.Rdi] = MemoryBase;
        memory.TryWrite(MemoryBase, stackalloc byte[] { 0xFF });

        Assert.True(manager.TryDispatch("BfBDZGbti7A", ctx, out var result));

        Span<byte> value = stackalloc byte[1];
        Assert.True(memory.TryRead(MemoryBase, value));
        Assert.Equal(OrbisGen2Result.ORBIS_GEN2_OK, result);
        Assert.Equal(0UL, ctx[CpuRegister.Rax]);
        Assert.Equal(0, value[0]);
    }

    [Fact]
    public void KernelIsTrinityMode_ReturnsFalse()
    {
        var manager = CreateRegisteredManager();
        var ctx = new CpuContext(new FakeCpuMemory(MemoryBase, 0x1000), Generation.Gen5);
        ctx[CpuRegister.Rax] = ulong.MaxValue;

        Assert.True(manager.TryDispatch("tU5e3f9gSiU", ctx, out var result));

        Assert.Equal(OrbisGen2Result.ORBIS_GEN2_OK, result);
        Assert.Equal(0UL, ctx[CpuRegister.Rax]);
    }

    private static ModuleManager CreateRegisteredManager()
    {
        var manager = new ModuleManager();
        manager.RegisterExports(
            SharpEmu.Generated.SysAbiExportRegistry.CreateExports(Generation.Gen5));
        return manager;
    }
}
