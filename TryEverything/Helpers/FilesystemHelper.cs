using System.IO;
using System.Text.RegularExpressions;

namespace TryEverything.Helpers
{
    static class FilesystemHelper
    {
        /// <summary>
        /// Removes all invalid path characters from the given text and then removes
        /// double spaces (in case any were added by removing invalid characters).
        /// </summary>
        /// <param name="text">The text to sanitise.</param>
        /// <returns>The path-safe sanitised text.</returns>
        public static string SanitiseForPath(string text)
        {
            return Regex.Replace(text, "[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]", "").Replace("  ", " ");
        }
    }
}
