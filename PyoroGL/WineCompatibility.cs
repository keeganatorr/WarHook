using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MonogameTest
{
    static class WineCompatibility
    {
        // Wine 11's default X11 EGL backend can fail to create the OpenGL
        // context on NVIDIA. Wine supports this per-executable GLX override:
        // https://github.com/wine-mirror/wine/blob/master/dlls/winex11.drv/x11drv_main.c
        public static bool RestartWithCompatibleGraphics()
        {
            if (!OperatingSystem.IsWindows()) return false;
            if (!NativeLibrary.TryLoad("ntdll.dll", out IntPtr library)) return false;
            bool wine;
            try { wine = NativeLibrary.TryGetExport(library, "wine_get_version", out _); }
            finally { NativeLibrary.Free(library); }
            if (!wine) return false;

            string executable = Environment.ProcessPath;
            if (string.IsNullOrEmpty(executable)) return false;
            string name = Path.GetFileName(executable);
            using var settings = Registry.CurrentUser.CreateSubKey(
                @"Software\Wine\AppDefaults\" + name + @"\X11 Driver");
            if (string.Equals(settings.GetValue("UseEGL") as string, "N", StringComparison.OrdinalIgnoreCase))
                return false;
            settings.SetValue("UseEGL", "N", RegistryValueKind.String);
            settings.Flush();

            // The Wine graphics driver may already be loaded before managed
            // Main. A fresh process makes the setting effective on the first run.
            var start = new ProcessStartInfo(executable) { UseShellExecute = false };
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++) start.ArgumentList.Add(args[i]);
            using var child = Process.Start(start);
            if (child == null) throw new InvalidOperationException("Could not restart WarHook with Wine graphics compatibility.");
            Console.WriteLine("Wine graphics compatibility enabled for " + name + " (GLX).");
            return true;
        }
    }
}
