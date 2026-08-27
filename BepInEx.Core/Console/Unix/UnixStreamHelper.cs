using System;
using System.IO;
using System.Runtime.InteropServices;
using MonoMod.Utils;

namespace BepInEx.Unix;

internal static class UnixStreamHelper
{
    public delegate int dupDelegate(int fd);

    public delegate int fcloseDelegate(IntPtr stream);

    public delegate IntPtr fdopenDelegate(int fd, string mode);

    public delegate int fflushDelegate(IntPtr stream);

    public delegate IntPtr freadDelegate(IntPtr ptr, IntPtr size, IntPtr nmemb, IntPtr stream);

    public delegate int fwriteDelegate(IntPtr ptr, IntPtr size, IntPtr nmemb, IntPtr stream);

    public delegate int isattyDelegate(int fd);

    public static readonly dupDelegate dup;
    public static readonly fdopenDelegate fdopen;
    public static readonly freadDelegate fread;
    public static readonly fwriteDelegate fwrite;
    public static readonly fcloseDelegate fclose;
    public static readonly fflushDelegate fflush;
    public static readonly isattyDelegate isatty;

    static UnixStreamHelper()
    {
        var candidates = new[] { "libc.so.6", "libc.so", "libc", "/usr/lib/libSystem.dylib" };
        IntPtr libc = IntPtr.Zero;
        foreach (var candidate in candidates)
            if (DynDll.TryOpenLibrary(candidate, out libc))
                break;

        if (libc == IntPtr.Zero)
            throw new DllNotFoundException("Unable to load libc");

        dup = Resolve<dupDelegate>(libc, nameof(dup));
        fdopen = Resolve<fdopenDelegate>(libc, nameof(fdopen));
        fread = Resolve<freadDelegate>(libc, nameof(fread));
        fwrite = Resolve<fwriteDelegate>(libc, nameof(fwrite));
        fclose = Resolve<fcloseDelegate>(libc, nameof(fclose));
        fflush = Resolve<fflushDelegate>(libc, nameof(fflush));
        isatty = Resolve<isattyDelegate>(libc, nameof(isatty));
    }

    private static T Resolve<T>(IntPtr library, string name) where T : Delegate =>
        (T) Marshal.GetDelegateForFunctionPointer(DynDll.GetExport(library, name), typeof(T));

    public static Stream CreateDuplicateStream(int fileDescriptor)
    {
        var newFd = dup(fileDescriptor);

        return new UnixStream(newFd, FileAccess.Write);
    }
}
