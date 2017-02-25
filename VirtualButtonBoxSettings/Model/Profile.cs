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
    class Profile : IComparable<Profile> {
        [DataMember]
        private string name;
        [DataMember]
        public List<ButtonGrid> grids;
        [DataMember]
        private bool isDefault;

        [DataMember]
        public int index;
        public string directory;

        private bool dummy;

        public string Name {
            get { return this.name; }
            set {
                this.name = value;
                string oldDirectory = this.directory;
                this.directory = DirectoryInfo.SanitizeFilename(value);
                if (!this.directory.Equals(oldDirectory)) {
                    DeconflictDirectory();
                    Directory.Move(Path.Combine(DirectoryInfo.FolderPath, oldDirectory), Path.Combine(DirectoryInfo.FolderPath, this.directory));
                }
                Save();
            }
        }

        private void DeconflictDirectory() {
            while (Directory.Exists(Path.Combine(DirectoryInfo.FolderPath, this.directory))) {
                this.directory += "_";
            }
        }

        public Profile() {
            this.dummy = false;
            this.name = "New Profile";
            this.grids = new List<ButtonGrid>();
            this.isDefault = false;

            this.directory = DirectoryInfo.SanitizeFilename(this.name);
            DeconflictDirectory();
            Directory.CreateDirectory(Path.Combine(DirectoryInfo.FolderPath, this.directory));
            Save();
        }

        public Profile(string dummyDirectory) {
            this.dummy = true;
            this.name = "Profiles";
            this.grids = new List<ButtonGrid>();
            this.isDefault = false;
            this.directory = dummyDirectory;
        }

        public override string ToString() {
            return this.name;
        }

        public ButtonGrid NewGrid() {
            ButtonGrid grid = new ButtonGrid();
            grid.parent = this;
            grid.DeconflictFilename();
            this.grids.Add(grid);
            Save();
            return grid;
        }

        public void DeleteGrid(ButtonGrid grid) {
            this.grids.Remove(grid);
            File.Delete(Path.Combine(DirectoryInfo.FolderPath, this.directory, grid.filename));
            Save();
        }

        [OnDeserialized]
        private void Deserialized(StreamingContext c) {
            foreach(ButtonGrid grid in this.grids) {
                grid.parent = this;
            }
        }

        public void Save() {
            if(this.dummy) {
                return;
            }
            FileStream stream = new FileStream(Path.Combine(DirectoryInfo.FolderPath, this.directory, DirectoryInfo.ProfileName), FileMode.Create, FileAccess.Write);
            var ser = new DataContractJsonSerializer(typeof(Profile));
            ser.WriteObject(stream, this);
            stream.Close();
        }

        private void GenerateTextures() {
            foreach(ButtonGrid grid in this.grids) {
                //grid.CreateAndSaveImage();
            }
        }

        public int CompareTo(Profile other) {
            return index - other.index;
        }
    }
}
