using System;
using System.ComponentModel;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace BepInEx.Preloader.RuntimeFixes;

public static class HarmonyBackendFix
{
    private static readonly ConfigEntry<MonoModBackend> ConfigHarmonyBackend = ConfigFile.CoreConfig.Bind(
     "Preloader",
     "HarmonyBackend",
     MonoModBackend.auto,
     "Specifies which MonoMod backend to use for Harmony patches. Auto uses the best available backend.\nThis setting should only be used for development purposes (e.g. debugging in dnSpy). Other code might override this setting.");

    public static void Initialize(bool isAndroid = false)
    {
        if (isAndroid)
        {
            // HarmonyX 的堆栈修正会创建 MonoMod 原生 trampoline；Android CoreCLR 执行它时会触发 SIGILL。
            var stackTraceFixes = typeof(Harmony).Assembly.GetType("HarmonyLib.Internal.RuntimeFixes.StackTraceFixes", true);
            stackTraceFixes.GetField("_applied", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, true);
        }

        switch (ConfigHarmonyBackend.Value)
        {
            case MonoModBackend.auto:
                // Android CoreCLR 无法使用 DynamicMethod 生成 Harmony wrapper，默认改用 Cecil。
                if (isAndroid)
                    Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", MonoModBackend.cecil.ToString());
                break;
            case MonoModBackend.dynamicmethod:
            case MonoModBackend.methodbuilder:
            case MonoModBackend.cecil:
                Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", ConfigHarmonyBackend.Value.ToString());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ConfigHarmonyBackend), ConfigHarmonyBackend.Value,
                                                      "Unknown backend");
        }
    }

    private enum MonoModBackend
    {
        // Enum names are important!
        [Description("Auto")]
        auto = 0,

        [Description("DynamicMethod")]
        dynamicmethod,

        [Description("MethodBuilder")]
        methodbuilder,

        [Description("Cecil")]
        cecil
    }
}
