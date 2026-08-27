using System;
using System.Runtime.InteropServices;

namespace BepInEx.Unity.IL2CPP.Hook.Dobby;

internal class DobbyDetour : BaseNativeDetour<DobbyDetour>
{
    private readonly bool specialReturnBuffer;

    public DobbyDetour(nint originalMethodPtr, Delegate detourMethod, bool specialReturnBuffer = false)
        : base(ResolveBranchTarget(originalMethodPtr), detourMethod) =>
        this.specialReturnBuffer = specialReturnBuffer;

    private static unsafe nint ResolveBranchTarget(nint target)
    {
        if (!OperatingSystem.IsAndroid() || RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
            return target;

        for (var depth = 0; depth < 8; depth++)
        {
            var instruction = *(uint*)target;
            if ((instruction & 0xFC000000) != 0x14000000)
                break;

            var immediate = (int)(instruction << 6) >> 6;
            target += (nint)((long)immediate << 2);
        }

        return target;
    }

    protected override void ApplyImpl() => EnsureSuccess(DobbyLib.Commit(OriginalMethodPtr), "DobbyCommit");

    protected override unsafe void PrepareImpl()
    {
        nint trampolinePtr = 0;
        EnsureSuccess(DobbyLib.Prepare(OriginalMethodPtr, DetourMethodPtr, specialReturnBuffer, &trampolinePtr),
            "DobbyPrepare");
        TrampolinePtr = trampolinePtr;
    }

    protected override void UndoImpl() => EnsureSuccess(DobbyLib.Destroy(OriginalMethodPtr), "DobbyDestroy");

    protected override void FreeImpl() { }

    private static void EnsureSuccess(int status, string operation)
    {
        if (status != 0)
            throw new InvalidOperationException($"{operation} failed with status {status}");
    }
}
