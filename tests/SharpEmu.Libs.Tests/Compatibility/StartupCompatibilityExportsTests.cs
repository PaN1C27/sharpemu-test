// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.HLE;
using System.Buffers.Binary;
using Xunit;

namespace SharpEmu.Libs.Tests.Compatibility;

public sealed class StartupCompatibilityExportsTests
{
    private const ulong MemoryBase = 0x1_0000_0000;

    [Fact]
    public void SystemServiceGetHdrToneMapLuminance_WritesDisplayDefaults()
    {
        var manager = CreateRegisteredManager();
        var memory = new FakeCpuMemory(MemoryBase, 0x1000);
        var ctx = new CpuContext(memory, Generation.Gen5);
        ctx[CpuRegister.Rdi] = MemoryBase;

        Assert.True(manager.TryDispatch("mPpPxv5CZt4", ctx, out var result));

        Span<byte> luminance = stackalloc byte[sizeof(float) * 3];
        Assert.True(memory.TryRead(MemoryBase, luminance));
        Assert.Equal(OrbisGen2Result.ORBIS_GEN2_OK, result);
        Assert.Equal(1000.0f, BinaryPrimitives.ReadSingleLittleEndian(luminance[0x00..0x04]));
        Assert.Equal(1000.0f, BinaryPrimitives.ReadSingleLittleEndian(luminance[0x04..0x08]));
        Assert.Equal(0.01f, BinaryPrimitives.ReadSingleLittleEndian(luminance[0x08..0x0C]));
    }

    [Fact]
    public void SystemServiceParamGetString_WritesDeterministicDefault()
    {
        var manager = CreateRegisteredManager();
        var memory = new FakeCpuMemory(MemoryBase, 0x1000);
        var ctx = new CpuContext(memory, Generation.Gen5);
        ctx[CpuRegister.Rdi] = 6;
        ctx[CpuRegister.Rsi] = MemoryBase;
        ctx[CpuRegister.Rdx] = 65;

        Assert.True(manager.TryDispatch("SsC-m-S9JTA", ctx, out var result));

        Span<byte> value = stackalloc byte[9];
        Assert.True(memory.TryRead(MemoryBase, value));
        Assert.Equal(OrbisGen2Result.ORBIS_GEN2_OK, result);
        Assert.Equal("SharpEmu\0"u8.ToArray(), value.ToArray());
    }

    [Fact]
    public void KernelGetOpenPsId_WritesDeterministicId()
    {
        var manager = CreateRegisteredManager();
        var memory = new FakeCpuMemory(MemoryBase, 0x1000);
        var ctx = new CpuContext(memory, Generation.Gen5);
        ctx[CpuRegister.Rdi] = MemoryBase;

        Assert.True(manager.TryDispatch("DLORcroUqbc", ctx, out var result));

        Span<byte> id = stackalloc byte[16];
        Assert.True(memory.TryRead(MemoryBase, id));
        Assert.Equal(OrbisGen2Result.ORBIS_GEN2_OK, result);
        Assert.Equal("SharpEmuOpenPsId"u8.ToArray(), id.ToArray());
    }

    [Fact]
    public void NpEntitlementAccessGetSkuFlag_ReportsFullSku()
    {
        var manager = CreateRegisteredManager();
        var memory = new FakeCpuMemory(MemoryBase, 0x1000);
        var ctx = new CpuContext(memory, Generation.Gen5);
        ctx[CpuRegister.Rdi] = MemoryBase;

        Assert.True(manager.TryDispatch("lPDO62PpJIA", ctx, out var result));

        Span<byte> flag = stackalloc byte[sizeof(uint)];
        Assert.True(memory.TryRead(MemoryBase, flag));
        Assert.Equal(OrbisGen2Result.ORBIS_GEN2_OK, result);
        Assert.Equal(3u, BinaryPrimitives.ReadUInt32LittleEndian(flag));
    }

    [Fact]
    public void UserServiceGetGamePresets_WritesDefaultPresetStructure()
    {
        var manager = CreateRegisteredManager();
        var memory = new FakeCpuMemory(MemoryBase, 0x1000);
        var ctx = new CpuContext(memory, Generation.Gen5);
        Span<byte> initial = stackalloc byte[0x28];
        initial.Fill(0xFF);
        Assert.True(memory.TryWrite(MemoryBase, initial));
        ctx[CpuRegister.Rdi] = 0x10000000;
        ctx[CpuRegister.Rsi] = MemoryBase;
        ctx[CpuRegister.Rcx] = 0;

        Assert.True(manager.TryDispatch("-sD02mFDBh4", ctx, out var result));

        Span<byte> presets = stackalloc byte[0x28];
        Assert.True(memory.TryRead(MemoryBase, presets));
        Assert.Equal(OrbisGen2Result.ORBIS_GEN2_OK, result);
        Assert.Equal(0x28UL, BinaryPrimitives.ReadUInt64LittleEndian(presets));
        Assert.Equal(new byte[0x20], presets[sizeof(ulong)..].ToArray());
    }

    [Fact]
    public void GameLiveStreamingInitialize_ReturnsDisabledSuccess()
    {
        var manager = CreateRegisteredManager();
        var ctx = new CpuContext(new FakeCpuMemory(MemoryBase, 0x1000), Generation.Gen5);

        Assert.True(manager.TryDispatch("kvYEw2lBndk", ctx, out var result));
        Assert.Equal(OrbisGen2Result.ORBIS_GEN2_OK, result);
        Assert.Equal(0UL, ctx[CpuRegister.Rax]);
    }

    [Fact]
    public void VideoOutIsOutputSupported_ReturnsTrueForOpenPort()
    {
        var manager = CreateRegisteredManager();
        var ctx = new CpuContext(new FakeCpuMemory(MemoryBase, 0x1000), Generation.Gen5);
        ctx[CpuRegister.Rdi] = 255;

        Assert.True(manager.TryDispatch("Up36PTk687E", ctx, out var openResult));
        var handle = (int)openResult;
        Assert.True(handle > 0);

        try
        {
            ctx[CpuRegister.Rdi] = unchecked((uint)handle);
            ctx[CpuRegister.Rsi] = 1;

            Assert.True(manager.TryDispatch("Nv8c-Kb+DUM", ctx, out var supportedResult));
            Assert.Equal(1, (int)supportedResult);
            Assert.Equal(1UL, ctx[CpuRegister.Rax]);
        }
        finally
        {
            ctx[CpuRegister.Rdi] = unchecked((uint)handle);
            manager.TryDispatch("uquVH4-Du78", ctx, out _);
        }
    }

    [Fact]
    public void AgcResourceRegistration_ReturnsOkWithoutLegacyInitializer()
    {
        var manager = CreateRegisteredManager();
        var memory = new FakeCpuMemory(MemoryBase, 0x2000);
        var ctx = new CpuContext(memory, Generation.Gen5);
        var ownerAddress = MemoryBase;
        var resourceNameAddress = memory.WriteCString(MemoryBase + 0x200, "Display target");

        ctx[CpuRegister.Rdi] = ownerAddress;
        ctx[CpuRegister.Rsi] = 1;
        ctx[CpuRegister.Rdx] = resourceNameAddress;
        ctx[CpuRegister.R8] = 1;
        ctx[CpuRegister.R9] = 0;

        Assert.True(manager.TryDispatch("W5z4eZrjEas", ctx, out var resourceResult));
        Assert.Equal(OrbisGen2Result.ORBIS_GEN2_OK, resourceResult);
        Assert.Equal(0UL, ctx[CpuRegister.Rax]);
    }

    [Theory]
    [InlineData("XlNp7jzGiPo")]
    [InlineData("MM4IZSEYytQ")]
    public void AgcDriverSetupExports_ReturnOk(string nid)
    {
        var manager = CreateRegisteredManager();
        var ctx = new CpuContext(new FakeCpuMemory(MemoryBase, 0x1000), Generation.Gen5);

        Assert.True(manager.TryDispatch(nid, ctx, out var result));
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
