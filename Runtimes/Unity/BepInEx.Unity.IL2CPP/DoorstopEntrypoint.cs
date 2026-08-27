using System;
using System.IO;
using BepInEx;
using BepInEx.Preloader.Core;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using MonoMod.Utils;

// ReSharper disable once CheckNamespace
namespace Doorstop;

internal static class Entrypoint
{
    /// <summary>
    ///     The main entrypoint of BepInEx, called from Doorstop.
    /// </summary>
    public static void Start()
    {
        // We set it to the current directory first as a fallback, but try to use the same location as the .exe file.
        var silentExceptionLog = Environment.GetEnvironmentVariable("BEPINEX_PRELOADER_LOG") ??
                                 $"preloader_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log";

        // Android CoreCLR 会把命名 Mutex 的共享文件创建在 /data/local/tmp，普通应用 UID 没有写入权限，
        // 执行 new Mutex(...) 会因 EACCES 导致 BepInEx 启动失败，因此保留原代码并注释掉。
        // Mutex mutex = null;

        try
        {
            EnvVars.LoadVars();

            silentExceptionLog =
                Path.Combine(Path.GetDirectoryName(EnvVars.DOORSTOP_PROCESS_PATH), silentExceptionLog);

            // var mutexId = Utility.HashStrings(Process.GetCurrentProcess().ProcessName, EnvVars.DOORSTOP_PROCESS_PATH,
            //                                   typeof(Entrypoint).FullName);
            //
            // mutex = new Mutex(false, $"Global\\{mutexId}");
            // mutex.WaitOne();

            UnityPreloaderRunner.PreloaderMain();
        }
        catch (Exception ex)
        {
            File.WriteAllText(silentExceptionLog, ex.ToString());

            try
            {
                if (PlatformDetection.OS.Is(OSKind.Windows))
                {
                    MessageBox.Show("Failed to start BepInEx", "BepInEx");
                }
                else if (NotifySend.IsSupported)
                {
                    NotifySend.Send("Failed to start BepInEx", "Check logs for details");
                }
                else if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BEPINEX_FAIL_FAST")))
                {
                    // Don't exit the game if we have no way of signaling to the user that a crash happened
                    return;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            Environment.Exit(1);
        }
        // finally
        // {
        //     mutex?.ReleaseMutex();
        // }
    }
}
