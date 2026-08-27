using System;
using System.Runtime.InteropServices;

namespace BepInEx.Unity.IL2CPP.Hook.Dobby;

internal static unsafe class DobbyLib
{
    [DllImport("dobby", EntryPoint = "DobbyHook", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Hook(nint target, nint replacement, nint* originalCall);

    [DllImport("dobby", EntryPoint = "DobbyPrepare", CallingConvention = CallingConvention.Cdecl)]
    private static extern int PrepareDirect(nint target, nint replacement, nint* originalCall);

    [DllImport("dobby", EntryPoint = "DobbyCommit", CallingConvention = CallingConvention.Cdecl)]
    private static extern int CommitDirect(nint target);

    [DllImport("dobby", EntryPoint = "DobbyDestroy", CallingConvention = CallingConvention.Cdecl)]
    private static extern int DestroyDirect(nint target);

    [DllImport("modbootstrap", EntryPoint = "mod_dobby_prepare", CallingConvention = CallingConvention.Cdecl)]
    private static extern int PrepareAndroid(nint target, nint replacement, int specialReturnBuffer,
        nint* originalCall);

    [DllImport("modbootstrap", EntryPoint = "mod_dobby_commit", CallingConvention = CallingConvention.Cdecl)]
    private static extern int CommitAndroid(nint target);

    [DllImport("modbootstrap", EntryPoint = "mod_dobby_destroy", CallingConvention = CallingConvention.Cdecl)]
    private static extern int DestroyAndroid(nint target);

    public static int Prepare(nint target, nint replacement, bool specialReturnBuffer, nint* originalCall)
    {
        if (OperatingSystem.IsAndroid())
            return PrepareAndroid(target, replacement, specialReturnBuffer ? 1 : 0, originalCall);
        if (specialReturnBuffer)
            throw new PlatformNotSupportedException("Special return buffers are only supported on Android ARM64");
        return PrepareDirect(target, replacement, originalCall);
    }

    public static int Commit(nint target) =>
        OperatingSystem.IsAndroid() ? CommitAndroid(target) : CommitDirect(target);

    public static int Destroy(nint target) =>
        OperatingSystem.IsAndroid() ? DestroyAndroid(target) : DestroyDirect(target);
}
