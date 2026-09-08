using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace MonogameTest
{
    // Locates and reads loose Assets/ files. On desktop they live beside the
    // executable; in the browser (KNI BlazorGL web build) they are served from
    // wwwroot/Assets and read through TitleContainer (synchronous XHR fetch),
    // which must happen on the main thread.
    static class GameAssets
    {
        public static bool IsWeb => OperatingSystem.IsBrowser();

        public static Stream Open(string fileName)
        {
            if (IsWeb)
                return TitleContainer.OpenStream("Assets/" + fileName);
            return File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Assets", fileName));
        }

        // Resolves beam-audio.json the way Game1 historically did: next to the
        // executable, then relative to the CWD (including the repo-root dev
        // fallback). Web gets a URL path resolved against the page.
        public static string BeamAudioConfigPath()
        {
            if (IsWeb)
                return "Assets/beam-audio.json";
            string audioSettings = Path.Combine(AppContext.BaseDirectory, "Assets", "beam-audio.json");
            foreach (string candidate in new[] { "Assets/beam-audio.json", "PyoroGL/Assets/beam-audio.json" })
                if (File.Exists(candidate)) { audioSettings = Path.GetFullPath(candidate); break; }
            return audioSettings;
        }

        public static string ReadText(string resolvedPath)
        {
            if (IsWeb)
            {
                using (var stream = TitleContainer.OpenStream(resolvedPath))
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
            return File.ReadAllText(resolvedPath);
        }
    }
}
