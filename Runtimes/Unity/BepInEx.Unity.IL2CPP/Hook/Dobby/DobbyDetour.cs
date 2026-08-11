using System;
using System.Runtime.InteropServices;

namespace BepInEx.Unity.IL2CPP.Hook.Dobby;

internal class DobbyDetour : BaseNativeDetour<DobbyDetour>
{
    public DobbyDetour(nint originalMethodPtr, Delegate detourMethod)
        : base(ResolveBranchTarget(originalMethodPtr), detourMethod) { }

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

    protected override void ApplyImpl() => DobbyLib.Commit(OriginalMethodPtr);

    protected override unsafe void PrepareImpl()
    {
        nint trampolinePtr = 0;
        DobbyLib.Prepare(OriginalMethodPtr, DetourMethodPtr, &trampolinePtr);
        TrampolinePtr = trampolinePtr;
    }

    protected override void UndoImpl() => DobbyLib.Destroy(OriginalMethodPtr);

    protected override void FreeImpl() { }
}
