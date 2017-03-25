using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Windows.Media.Imaging;
using GDI = System.Drawing;
using System.ComponentModel;

namespace VirtualButtonBoxSettings {
    public class Profiles {
        public static List<Profile> profiles;

        static Profiles() {
            profiles = new List<Profile>();

            foreach(string directory in Directory.GetDirectories(DirectoryInfo.FolderPath)) {
                string filename = Path.Combine(directory, DirectoryInfo.ProfileName);
                if(File.Exists(filename)) {
                    var ser = new DataContractJsonSerializer(typeof(Profile));
                    FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
                    try {
                        Profile profile = (Profile)ser.ReadObject(stream);
                        profile.directory = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar));
                        profiles.Add(profile);
                    }
                    catch(Exception e) {
                        Console.WriteLine(e);
                    }
                    stream.Close();
                }
            }

            profiles.Sort();
            ReindexProfiles();
        }

        private static void ReindexProfiles() {
            for (int i = 0; i < profiles.Count; i++) {
                profiles[i].index = i;
            }
            SaveProfiles();
        }

        public static void SaveProfiles() {
            foreach(Profile profile in profiles) {
                profile.Save();
            }
        }

        public static Profile NewProfile() {
            Profile profile = new Profile();
            profile.index = profiles.Count;
            profiles.Add(profile);
            profile.Save();
            return profile;
        }

        public static void DeleteProfile(Profile profile) {
            profiles.Remove(profile);
            Directory.Delete(Path.Combine(DirectoryInfo.FolderPath, profile.directory), true);
            ReindexProfiles();
        }

        public static void MoveProfileUp(Profile profile) {
            int index = profiles.IndexOf(profile);
            if(index <= 0) {
                return;
            }
            Profile previous = profiles[index - 1];
            profiles[index - 1] = profile;
            profiles[index] = previous;
            ReindexProfiles();
        }

        public static void MoveProfileDown(Profile profile) {
            int index = profiles.IndexOf(profile);
            if (index >= profiles.Count-1) {
                return;
            }
            Profile next = profiles[index + 1];
            profiles[index + 1] = profile;
            profiles[index] = next;
            ReindexProfiles();
        }

        public static void SaveProfilesTexture(BackgroundWorker worker) {
            float buttonSize = 0.1f;
            int gridWidth = 4;
            int gridHeight = 2;
            while((gridWidth*(gridHeight-1)) < profiles.Count) {
                if(gridWidth == gridHeight-1) {
                    gridWidth++;
                }
                else {
                    gridHeight++;
                }
            }

            Profile p = new Profile(DirectoryInfo.FolderPath);
            ButtonGrid g = p.NewGrid();
            g.filename = DirectoryInfo.ProfilesTextureName;

            g.GridWidth = gridWidth;
            g.GridHeight = gridHeight;
            g.Width = gridWidth * buttonSize;
            g.Height = gridHeight * buttonSize;
            g.Outline = true;

            GridButton resetButton = g.NewButton(0, 0);
            resetButton.Width = gridWidth/2;
            resetButton.Text = "Reset Seated Position";
            resetButton.FontSize = 0.0125;

            GridButton laserButton = g.NewButton(2, 0);
            laserButton.Width = gridWidth - (gridWidth / 2);
            laserButton.Text = "Toggle Laser Mode";
            laserButton.FontSize = 0.0125;

            int gridX = 0;
            int gridY = 1;
            foreach(Profile profile in profiles) {
                GridButton b = g.NewButton(gridX, gridY);
                b.Text = profile.Name;

                gridX++;
                if (gridX >= gridWidth) {
                    gridX = 0;
                    gridY++;
                }
            }

            g.CreateAndSaveImage(worker);
        }
    }
}
