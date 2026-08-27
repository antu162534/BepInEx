using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP.Hook.Dobby;
using BepInEx.Unity.IL2CPP.Hook.Funchook;
using MonoMod.Utils;

namespace BepInEx.Unity.IL2CPP.Hook;

public interface INativeDetour : IDisposable
{
    private static readonly ConfigEntry<DetourProvider> DetourProviderType = ConfigFile.CoreConfig.Bind(
         "Detours", "DetourProviderType",
         DetourProvider.Default,
         "The native provider to use for managed detours"
        );

    public nint OriginalMethodPtr { get; }
    public nint DetourMethodPtr { get; }
    public nint TrampolinePtr { get; }
    public bool IsValid { get; }
    public bool IsApplied { get; }

    public void Apply();
    public void Undo();
    public void Free();
    public T GenerateTrampoline<T>() where T : Delegate;

    private static INativeDetour CreateDefault<T>(nint original, T target, bool specialReturnBuffer)
        where T : Delegate =>
        // TODO: check and provide an OS accurate provider
        new DobbyDetour(original, target, specialReturnBuffer);

    public static INativeDetour Create<T>(nint original, T target, bool specialReturnBuffer = false)
        where T : Delegate
    {
        var detour = DetourProviderType.Value switch
        {
            DetourProvider.Dobby    => new DobbyDetour(original, target, specialReturnBuffer),
            DetourProvider.Funchook when !specialReturnBuffer => new FunchookDetour(original, target),
            DetourProvider.Funchook => throw new PlatformNotSupportedException(
                "Funchook does not support ARM64 return buffers"),
            _ => CreateDefault(original, target, specialReturnBuffer)
        };
        if (PlatformDetection.Runtime != RuntimeKind.Mono)
        {
            return new CacheDetourWrapper(detour, target);
        }

        return detour;
    }

    public static INativeDetour CreateAndApply<T>(nint from, T to, out T original)
        where T : Delegate
    {
        var detour = Create(from, to);
        original = detour.GenerateTrampoline<T>();
        detour.Apply();

        return detour;
    }

    // Workaround for CoreCLR collecting all delegates
    private class CacheDetourWrapper : INativeDetour
    {
        private readonly INativeDetour _wrapped;

        private List<object> _cache = new();

        public CacheDetourWrapper(INativeDetour wrapped, Delegate target)
        {
            _wrapped = wrapped;
            _cache.Add(target);
        }

        public void Dispose()
        {
            _wrapped.Dispose();
            _cache.Clear();
        }

        public void Apply() => _wrapped.Apply();

        public void Undo() => _wrapped.Undo();

        public void Free() => _wrapped.Free();

        public T GenerateTrampoline<T>() where T : Delegate
        {
            var trampoline = _wrapped.GenerateTrampoline<T>();
            _cache.Add(trampoline);
            return trampoline;
        }

        public bool IsValid => _wrapped.IsValid;

        public bool IsApplied => _wrapped.IsApplied;

        public nint OriginalMethodPtr => _wrapped.OriginalMethodPtr;

        public nint DetourMethodPtr => _wrapped.DetourMethodPtr;

        public nint TrampolinePtr => _wrapped.TrampolinePtr;
    }

    internal enum DetourProvider
    {
        Default,
        Dobby,
        Funchook
    }
}
