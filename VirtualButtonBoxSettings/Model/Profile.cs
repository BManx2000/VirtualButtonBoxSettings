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
    public class Profile : IComparable<Profile> {
        [DataMember]
        private string name;
        [DataMember]
        public List<ButtonGrid> grids;
        [DataMember]
        private bool isDefault;
        [DataMember]
        private bool hidePointer;
        [DataMember]
        private double pointerAlpha;
        [DataMember]
        private double controllerAlpha;

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

        public bool HidePointer {
            get { return hidePointer; }
            set {
                hidePointer = value;
                Save();
            }
        }

        public double PointerAlpha {
            get { return this.pointerAlpha; }
            set {
                this.pointerAlpha = Math.Min(Math.Max(0.0, value), 1.0);
                Save();
            }
        }

        public double ControllerAlpha {
            get { return this.controllerAlpha; }
            set {
                this.controllerAlpha = Math.Min(Math.Max(0.0, value), 1.0);
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
            this.pointerAlpha = 0.5;
            this.controllerAlpha = 0.1;

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
