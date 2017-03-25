using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace VirtualButtonBoxSettings {
    [DataContract]
    public class Settings {

        public static readonly int PixelDensity = 1500;
        private static Settings _instance;
        private static object syncRoot = new object();

        [DataMember]
        public bool Lasermode;
        [DataMember]
        public bool OculusTouchMode;

        static Settings() {
            if (File.Exists(DirectoryInfo.SettingsPath)) {
                var ser = new DataContractJsonSerializer(typeof(Settings));
                FileStream stream = new FileStream(DirectoryInfo.SettingsPath, FileMode.Open, FileAccess.Read);
                _instance = (Settings)ser.ReadObject(stream);
                stream.Close();
            }
            else {
                _instance = new Settings();
            }
        }

        public static Settings Instance {
            get {
                return _instance;
            }
        }

        private Settings() {
            
        }

        public void save() {
            FileStream stream = new FileStream(DirectoryInfo.SettingsPath, FileMode.Create, FileAccess.Write);
            var ser = new DataContractJsonSerializer(typeof(Settings));
            ser.WriteObject(stream, this);
            stream.Close();
        }
    }
}
