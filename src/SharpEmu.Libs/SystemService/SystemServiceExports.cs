// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.HLE;
using SharpEmu.Libs.Gpu;
using SharpEmu.Libs.VideoOut;
using System.Buffers.Binary;
using System.Text;

namespace SharpEmu.Libs.SystemService;

public static class SystemServiceExports
{
    private const int OrbisSystemServiceErrorParameter = unchecked((int)0x80A10003);
    private const int SystemServiceParamIdSystemName = 6;
    private const int SystemServiceStatusSize = 0x0C;
    private const int DisplaySafeAreaInfoSize = sizeof(float) + 128;
    private const int HdrToneMapLuminanceSize = sizeof(float) * 3;
    private const string SystemName = "CFI-1000A";

    [SysAbiExport(
        Nid = "fZo48un7LK4",
        ExportName = "sceSystemServiceParamGetInt",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceSystemService")]
    public static int SystemServiceParamGetInt(CpuContext ctx)
    {
        var parameterId = unchecked((int)ctx[CpuRegister.Rdi]);
        var valueAddress = ctx[CpuRegister.Rsi];
        if (valueAddress == 0)
        {
            return ctx.SetReturn(OrbisSystemServiceErrorParameter);
        }

        var value = parameterId switch
        {
            1 or 2 or 3 or 1000 => 1,
            4 => 180,
            _ => 0,
        };

        Span<byte> valueBytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(valueBytes, value);
        return ctx.Memory.TryWrite(valueAddress, valueBytes)
            ? ctx.SetReturn(0)
            : ctx.SetReturn((int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT);
    }

    [SysAbiExport(
        Nid = "rPo6tV8D9bM",
        ExportName = "sceSystemServiceGetStatus",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceSystemService")]
    public static int SystemServiceGetStatus(CpuContext ctx)
    {
        var statusAddress = ctx[CpuRegister.Rdi];
        if (statusAddress == 0)
        {
            return ctx.SetReturn(OrbisSystemServiceErrorParameter);
        }

        Span<byte> status = stackalloc byte[SystemServiceStatusSize];
        status.Clear();
        BinaryPrimitives.WriteInt32LittleEndian(status, 0);
        status[0x06] = 1;

        return ctx.Memory.TryWrite(statusAddress, status)
            ? ctx.SetReturn(0)
            : ctx.SetReturn((int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT);
    }

    [SysAbiExport(
        Nid = "1n37q1Bvc5Y",
        ExportName = "sceSystemServiceGetDisplaySafeAreaInfo",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceSystemService")]
    public static int SystemServiceGetDisplaySafeAreaInfo(CpuContext ctx)
    {
        var infoAddress = ctx[CpuRegister.Rdi];
        if (infoAddress == 0)
        {
            return ctx.SetReturn(OrbisSystemServiceErrorParameter);
        }

        Span<byte> info = stackalloc byte[DisplaySafeAreaInfoSize];
        info.Clear();
        BinaryPrimitives.WriteSingleLittleEndian(info, 1.0f);

        return ctx.Memory.TryWrite(infoAddress, info)
            ? ctx.SetReturn(0)
            : ctx.SetReturn((int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT);
    }

    [SysAbiExport(
        Nid = "SsC-m-S9JTA",
        ExportName = "sceSystemServiceParamGetString",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceSystemService")]
    public static int SystemServiceParamGetString(CpuContext ctx)
    {
        var parameterId = unchecked((int)ctx[CpuRegister.Rdi]);
        var valueAddress = ctx[CpuRegister.Rsi];
        var capacity = ctx[CpuRegister.Rdx];
        var valueBytes = Encoding.UTF8.GetBytes(SystemName);
        if (parameterId != SystemServiceParamIdSystemName ||
            valueAddress == 0 ||
            capacity <= (ulong)valueBytes.Length)
        {
            return ctx.SetReturn(OrbisSystemServiceErrorParameter);
        }

        Span<byte> output = stackalloc byte[valueBytes.Length + 1];
        output.Clear();
        valueBytes.CopyTo(output);
        return ctx.Memory.TryWrite(valueAddress, output)
            ? ctx.SetReturn(0)
            : ctx.SetReturn((int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT);
    }

    [SysAbiExport(
        Nid = "mPpPxv5CZt4",
        ExportName = "sceSystemServiceGetHdrToneMapLuminance",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceSystemService")]
    public static int SystemServiceGetHdrToneMapLuminance(CpuContext ctx)
    {
        var luminanceAddress = ctx[CpuRegister.Rdi];
        if (luminanceAddress == 0)
        {
            return ctx.SetReturn(OrbisSystemServiceErrorParameter);
        }

        Span<byte> luminance = stackalloc byte[HdrToneMapLuminanceSize];
        BinaryPrimitives.WriteSingleLittleEndian(luminance[0x00..0x04], 1000.0f);
        BinaryPrimitives.WriteSingleLittleEndian(luminance[0x04..0x08], 1000.0f);
        BinaryPrimitives.WriteSingleLittleEndian(luminance[0x08..0x0C], 0.01f);
        return ctx.Memory.TryWrite(luminanceAddress, luminance)
            ? ctx.SetReturn(0)
            : ctx.SetReturn((int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT);
    }

    [SysAbiExport(
        Nid = "Vo5V8KAwCmk",
        ExportName = "sceSystemServiceHideSplashScreen",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceSystemService")]
    public static int SystemServiceHideSplashScreen(CpuContext ctx)
    {
        GuestGpu.Current.HideSplashScreen();
        return ctx.SetReturn(0);
    }

    [SysAbiExport(
        Nid = "3s8cHiCBKBE",
        ExportName = "sceSystemServiceReportAbnormalTermination",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceSystemService")]
    public static int SystemServiceReportAbnormalTermination(CpuContext ctx) => ctx.SetReturn(0);
}
