using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualButtonBoxSettings {
    class DirectoryInfo {
        public static readonly string FolderName = "Virtual Button Box";
        public static readonly string SettingsName = "settings.json";
        public static readonly string ProfileName = "profile.json";
        public static readonly string ProfilesTextureName = "profiles.png";
        public static readonly string FolderPath;
        public static readonly string SettingsPath;
        public static readonly string ProfilesTexturePath;

        static DirectoryInfo() {
            FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), FolderName);
            Directory.CreateDirectory(FolderPath);
            SettingsPath = Path.Combine(FolderPath, SettingsName);
            ProfilesTexturePath = Path.Combine(FolderPath, ProfilesTextureName);
        }

        public static string SanitizeFilename(string name) {
            foreach (var c in Path.GetInvalidFileNameChars()) {
                name = name.Replace(c.ToString(), "");
            }
            return name;
        }
    }
}
