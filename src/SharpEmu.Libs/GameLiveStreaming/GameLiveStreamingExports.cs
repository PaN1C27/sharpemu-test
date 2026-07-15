// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.HLE;

namespace SharpEmu.Libs.GameLiveStreaming;

public static class GameLiveStreamingExports
{
    [SysAbiExport(
        Nid = "kvYEw2lBndk",
        ExportName = "sceGameLiveStreamingInitialize",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceGameLiveStreaming")]
    public static int GameLiveStreamingInitialize(CpuContext ctx)
    {
        // Streaming is unavailable, but initialization succeeds in the disabled state.
        return ctx.SetReturn(OrbisGen2Result.ORBIS_GEN2_OK);
    }
}
