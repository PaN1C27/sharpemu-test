// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Buffers.Binary;
using SharpEmu.Core.Loader;
using SharpEmu.Core.Memory;
using Xunit;

namespace SharpEmu.Libs.Tests.Loader;

public sealed class SelfLoaderTests
{
    private const int SelfHeaderSize = 0x20;
    private const int ElfHeaderSize = 0x40;

    [Theory]
    [InlineData(0x00, 0x00000101u, 0x22)]
    [InlineData(0x10, 0x10000101u, 0x32)]
    public void Load_ProsperoSelfHeaderVariants_AreAccepted(byte version, uint keyType, ushort flags)
    {
        var imageData = CreateProsperoSelf(version, keyType, flags);

        var image = new SelfLoader().Load(imageData, new VirtualMemory());

        Assert.True(image.IsSelf);
        Assert.Equal(2, image.ElfHeader.AbiVersion);
        Assert.Empty(image.ProgramHeaders);
    }

    [Fact]
    public void Load_ProsperoSelfWithoutEmbeddedElf_IsRejected()
    {
        var imageData = CreateProsperoSelf(version: 0x10, keyType: 0x10000101, flags: 0x32);
        imageData[SelfHeaderSize] = 0;

        var exception = Assert.Throws<InvalidDataException>(
            () => new SelfLoader().Load(imageData, new VirtualMemory()));

        Assert.Equal("Input does not contain a valid ELF header.", exception.Message);
    }

    private static byte[] CreateProsperoSelf(byte version, uint keyType, ushort flags)
    {
        var imageData = new byte[SelfHeaderSize + ElfHeaderSize];
        var header = imageData.AsSpan(0, SelfHeaderSize);
        header[0] = 0x54;
        header[1] = 0x14;
        header[2] = 0xF5;
        header[3] = 0xEE;
        header[4] = version;
        header[5] = 0x01;
        header[6] = 0x01;
        header[7] = 0x12;
        BinaryPrimitives.WriteUInt32LittleEndian(header[8..], keyType);
        BinaryPrimitives.WriteUInt16LittleEndian(header[0x0C..], SelfHeaderSize);
        BinaryPrimitives.WriteUInt64LittleEndian(header[0x10..], (ulong)imageData.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(header[0x1A..], flags);

        var elf = imageData.AsSpan(SelfHeaderSize, ElfHeaderSize);
        elf[0] = 0x7F;
        elf[1] = (byte)'E';
        elf[2] = (byte)'L';
        elf[3] = (byte)'F';
        elf[4] = 0x02;
        elf[5] = 0x01;
        elf[6] = 0x01;
        elf[7] = 0x09;
        elf[8] = 0x02;
        BinaryPrimitives.WriteUInt16LittleEndian(elf[0x10..], 0xFE10);
        BinaryPrimitives.WriteUInt16LittleEndian(elf[0x12..], 0x003E);
        BinaryPrimitives.WriteUInt32LittleEndian(elf[0x14..], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(elf[0x34..], ElfHeaderSize);
        BinaryPrimitives.WriteUInt16LittleEndian(elf[0x36..], 0x38);

        return imageData;
    }
}
