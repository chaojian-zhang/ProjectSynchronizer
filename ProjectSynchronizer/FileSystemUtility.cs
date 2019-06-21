using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectSynchronizer
{
    public static class FileSystemUtility
    {
        public static string EscapeFilename(this string name)
        {
            if (name.Contains(Path.DirectorySeparatorChar))
                throw new ArgumentException($"Input name '{name}' must be a filename not a file path.");
            return string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
