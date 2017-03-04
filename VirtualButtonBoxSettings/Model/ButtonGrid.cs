using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Diagnostics;
using GDI = System.Drawing;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Interop;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace VirtualButtonBoxSettings {
    [DataContract]
    class ButtonGrid {
        [DataMember]
        private string name;
        [DataMember]
        private int gridWidth;
        [DataMember]
        private int gridHeight;
        [DataMember]
        private double width;
        [DataMember]
        private double height;
        [DataMember]
        private double x;
        [DataMember]
        private double y;
        [DataMember]
        private double z;
        [DataMember]
        private double pitch;
        [DataMember]
        private double yaw;
        [DataMember]
        private double roll;
        [DataMember]
        private double alpha;
        [DataMember]
        private bool outline;
        [DataMember]
        private bool border;
        [DataMember]
        private bool locked;
        [DataMember]
        private List<GridButton> buttons;
        [DataMember]
        public string filename;

        private GridButton[,] SpatialIndex;

        public Profile parent;

        public string Name {
            get { return this.name; }
            set {
                this.name = value;
                string oldFile = this.filename;
                this.filename = DirectoryInfo.SanitizeFilename(value) + ".png";
                if (!this.filename.Equals(oldFile)) {
                    DeconflictFilename();
                    if (File.Exists(Path.Combine(DirectoryInfo.FolderPath, parent.directory, oldFile))) {
                        File.Move(Path.Combine(DirectoryInfo.FolderPath, parent.directory, oldFile), Path.Combine(DirectoryInfo.FolderPath, parent.directory, this.filename));
                    }
                }
                parent.Save();
            }
        }

        public int GridWidth {
            get { return this.gridWidth; }
            set {
                if (this.CanSetGridWidthTo(value)) {
                    this.gridWidth = value;
                    this.RebuildSpatialIndex();
                    parent.Save();
                }
            }
        }

        public int GridHeight {
            get { return this.gridHeight; }
            set {
                if (this.CanSetGridHeightTo(value)) {
                    this.gridHeight = value;
                    this.RebuildSpatialIndex();
                    parent.Save();
                }
            }
        }

        public double Width {
            get { return this.width; }
            set {
                this.width = Math.Max(value, 0.001);
                parent.Save();
            }
        }

        public double Height {
            get { return this.height; }
            set {
                this.height = Math.Max(value, 0.001);
                parent.Save();
            }
        }

        public double X {
            get { return this.x; }
            set {
                this.x = value;
                parent.Save();
            }
        }

        public double Y {
            get { return this.y; }
            set {
                this.y = value;
                parent.Save();
            }
        }

        public double Z {
            get { return this.z; }
            set {
                this.z = value;
                parent.Save();
            }
        }

        public double Pitch {
            get { return this.pitch; }
            set {
                this.pitch = Math.Max(Math.Min(360.0, value), 0);
                parent.Save();
            }
        }

        public double Yaw {
            get { return this.yaw; }
            set {
                this.yaw = Math.Max(Math.Min(360.0, value), 0);
                parent.Save();
            }
        }

        public double Roll {
            get { return this.roll; }
            set {
                this.roll = Math.Max(Math.Min(360.0, value), 0);
                parent.Save();
            }
        }

        public double Alpha {
            get { return this.alpha; }
            set {
                this.alpha = Math.Max(Math.Min(1.0, value), 0.0);
                parent.Save();
            }
        }

        public bool Outline {
            get { return this.outline; }
            set {
                this.outline = value;
                parent.Save();
            }
        }

        public bool Border {
            get { return this.border; }
            set {
                this.border = value;
                parent.Save();
            }
        }

        public bool Locked {
            get { return this.locked; }
            set {
                this.locked = value;
                parent.Save();
            }
        }

        public ButtonGrid() {
            this.name = "New Grid";
            this.gridWidth = 4;
            this.gridHeight = 4;
            this.width = 0.5;
            this.height = 0.5;
            this.x = 0;
            this.y = 0;
            this.z = 0.5;
            this.pitch = 0;
            this.yaw = 0;
            this.roll = 0;
            this.alpha = 1;
            this.outline = false;
            this.border = true;
            this.locked = false;
            this.buttons = new List<GridButton>();
            this.RebuildSpatialIndex();

            this.filename = DirectoryInfo.SanitizeFilename(this.name) + ".png";
        }

        public void DeconflictFilename() {
            while (File.Exists(Path.Combine(DirectoryInfo.FolderPath, parent.directory, this.filename))) {
                this.filename = this.filename.Insert(this.filename.Length - 4, "_");
            }
        }

        public override string ToString() {
            return this.name;
        }

        [OnDeserialized]
        private void SetButtonParents(StreamingContext c) {
            foreach (GridButton button in this.buttons) {
                button.parent = this;
            }
            this.RebuildSpatialIndex();
        }

        private void RebuildSpatialIndex() {
            this.SpatialIndex = new GridButton[this.gridWidth, this.gridHeight];
            foreach (GridButton button in this.buttons) {
                this.AddButtonToIndex(button);
            }
        }

        public void RemoveButtonFromIndex(GridButton button) {
            for (int x = button.X; x < button.X + button.Width; x++) {
                for (int y = button.Y; y < button.Y + button.Height; y++) {
                    Debug.Assert(this.SpatialIndex[x, y] == button);
                    this.SpatialIndex[x, y] = null;
                }
            }
        }

        public void AddButtonToIndex(GridButton button) {
            for (int x = button.X; x < button.X + button.Width; x++) {
                for (int y = button.Y; y < button.Y + button.Height; y++) {
                    Debug.Assert(this.SpatialIndex[x, y] == null);
                    this.SpatialIndex[x, y] = button;
                }
            }
        }

        public List<GridButton> Buttons {
            get { return new List<GridButton>(this.buttons); }
        }

        public GridButton ButtonAtPosition(int x, int y) {
            return this.SpatialIndex[x, y];
        }

        public GridButton NewButton(int x, int y) {
            Debug.Assert(this.SpatialIndex[x, y] == null);
            GridButton button = new GridButton(x, y);
            button.parent = this;
            this.buttons.Add(button);
            this.SpatialIndex[x, y] = button;
            parent.Save();
            return button;
        }

        public void DeleteButton(GridButton button) {
            this.RemoveButtonFromIndex(button);
            this.buttons.Remove(button);
            parent.Save();
        }

        public bool CanSetGridWidthTo(int width) {
            if (width < 1) {
                return false;
            }
            if (width >= this.gridWidth) {
                return true;
            }
            for (int x = this.gridWidth - 1; x > width - 1; x--) {
                for (int y = 0; y < this.gridHeight; y++) {
                    if (this.SpatialIndex[x, y] != null) {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool CanSetGridHeightTo(int height) {
            if (height < 1) {
                return false;
            }
            if (height >= this.gridHeight) {
                return true;
            }
            for (int y = this.gridHeight - 1; y > height - 1; y--) {
                for (int x = 0; x < this.gridWidth; x++) {
                    if (this.SpatialIndex[x, y] != null) {
                        return false;
                    }
                }
            }
            return true;
        }

        public BitmapSource CreateAndSaveImage(BackgroundWorker worker) {
            int width = (int)(this.width * Settings.PixelDensity);
            int height = (int)(this.height * Settings.PixelDensity);

            var bitmap = new GDI.Bitmap(width, height, GDI.Imaging.PixelFormat.Format32bppArgb);
            var graphics = GDI.Graphics.FromImage(bitmap);
            graphics.TextRenderingHint = GDI.Text.TextRenderingHint.AntiAliasGridFit;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;

            int penWidth = 2;
            GDI.Pen pen = new GDI.Pen(GDI.Color.White, penWidth);
            GDI.Brush brush = new GDI.SolidBrush(GDI.Color.White);
            System.Drawing.StringFormat format = new System.Drawing.StringFormat();
            format.Alignment = System.Drawing.StringAlignment.Center;
            format.LineAlignment = System.Drawing.StringAlignment.Center;

            if (this.border) {
                graphics.DrawRectangle(pen, new GDI.Rectangle(penWidth / 2, penWidth / 2, width - penWidth, height - penWidth));
            }

            foreach (GridButton button in this.buttons) {
                int x = (button.X * width) / this.gridWidth;
                int y = (button.Y * height) / this.gridHeight;
                int buttonWidth = ((button.X + button.Width) * width) / this.gridWidth - x;
                int buttonHeight = ((button.Y + button.Height) * height) / this.gridHeight - y;

                if(button.ButtonType == ButtonType.Normal || button.ButtonType == ButtonType.ThreeWaySwitch) {
                    graphics.DrawRectangle(pen, x, y, buttonWidth, buttonHeight);
                }
                else {
                    int minWidth = Math.Min(buttonWidth, buttonHeight);
                    int circleX = (x + buttonWidth / 2) - minWidth / 2;
                    int circleY = (y + buttonHeight / 2) - minWidth / 2;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphics.DrawEllipse(pen, circleX, circleY, minWidth, minWidth);
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
                }
                
                GDI.Font font = new GDI.Font(GDI.SystemFonts.DefaultFont.FontFamily, (float)(button.FontSize * Settings.PixelDensity));

                graphics.DrawString(button.Text, font, brush, new GDI.RectangleF(x + penWidth, y + penWidth, buttonWidth - penWidth * 2, buttonHeight - penWidth * 2), format);
            }

            graphics.Dispose();

            if (worker.CancellationPending) { return null; }

            if (this.outline) {
                OutlineBitmap(bitmap, worker, true);
                OutlineBitmap(bitmap, worker, false);
                OutlineBitmap(bitmap, worker, false);
            }

            if (worker.CancellationPending) { return null; }

            bitmap.Save(Path.Combine(DirectoryInfo.FolderPath, parent.directory, this.filename), GDI.Imaging.ImageFormat.Png);

            var hbmp = bitmap.GetHbitmap();
            var options = BitmapSizeOptions.FromEmptyOptions();
            return Imaging.CreateBitmapSourceFromHBitmap(hbmp, IntPtr.Zero, Int32Rect.Empty, options);
        }

        private void OutlineBitmap(GDI.Bitmap bitmap, BackgroundWorker worker, bool first) {
            GDI.Imaging.BitmapData bData = bitmap.LockBits(new GDI.Rectangle(0, 0, bitmap.Width, bitmap.Height), GDI.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);

            int bytesPerPixel = GDI.Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            int byteCount = bData.Stride * bitmap.Height;
            byte[] pixels = new byte[byteCount];
            IntPtr ptrFirstPixel = bData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
            byte[] nPixels = new byte[byteCount];
            int heightInPixels = bData.Height;
            int widthInBytes = bData.Width * bytesPerPixel;
            int width = bitmap.Width;
            int height = bitmap.Height;
            int stride = bData.Stride;

            for (int y = 0; y < heightInPixels; y++) {
                if (worker.CancellationPending) { bitmap.UnlockBits(bData); return; }
                int currentLine = y * stride;
                for (int x = 0; x < widthInBytes; x = x + bytesPerPixel) {
                    int oB = pixels[currentLine + x];
                    int oG = pixels[currentLine + x + 1];
                    int oR = pixels[currentLine + x + 2];
                    int oA = pixels[currentLine + x + 3];

                    int nB = 0;
                    int nG = 0;
                    int nR = 0;
                    int nA = 0;

                    if (oA > 0 && oA < 255) {
                        nA = 255;
                        nR = (oR * oA) / 255;
                        nG = (oR * oA) / 255;
                        nB = (oR * oA) / 255;
                    }
                    else if (oA == 255) {
                        nA = 255;
                        nR = oR;
                        nB = oB;
                        nG = oG;
                    }
                    else {
                        int sum = 0;
                        for (int i = -1; i <= 1; i++) {
                            if (((y + i) < 0) || ((y + i) >= height)) {
                                continue;
                            }
                            int cLine = (y + i) * stride;
                            for (int j = -1; j <= 1; j++) {
                                if ((((x / bytesPerPixel) + j) < 0) || (((x / bytesPerPixel) + j) >= width)) {
                                    continue;
                                }

                                int cX = x + j * bytesPerPixel;
                                sum += pixels[cLine + cX + 3];
                            }
                        }
                        if(first && sum > 0) {
                            sum = 255 * 3;
                        }
                        nA = Math.Min(sum / 3, 255);
                        nR = 0;
                        nG = 0;
                        nB = 0;
                    }

                    // calculate new pixel value
                    nPixels[currentLine + x] = (byte)nB;
                    nPixels[currentLine + x + 1] = (byte)nG;
                    nPixels[currentLine + x + 2] = (byte)nR;
                    nPixels[currentLine + x + 3] = (byte)nA;
                }
            }

            // copy modified bytes back
            Marshal.Copy(nPixels, 0, ptrFirstPixel, pixels.Length);
            bitmap.UnlockBits(bData);
            /*
            for (int y=0; y<bitmap.Height; y++) {
                for(int x=0; x<bitmap.Width; x++) {
                    GDI.Color color = bitmap.GetPixel(x, y);
                    if(color.A > 0 && color.A < 255) {
                        GDI.Color newColor = GDI.Color.FromArgb((color.R * color.A) / 255, (color.G * color.A) / 255, (color.B * color.A) / 255);
                        newBitmap.SetPixel(x, y, newColor);
                    }
                    else if(color.A == 255) {
                        newBitmap.SetPixel(x, y, color);
                    }
                    else {
                        bool black = false;
                        for(int i=-4; i<=4; i++) {
                            if(y+i < 0 || y+i >= bitmap.Height) {
                                continue;
                            }
                            for(int j=-4; j<=4; j++) {
                                if (x + j < 0 || x + j >= bitmap.Width) {
                                    continue;
                                }
                                if (bitmap.GetPixel(x + j, y + i).A > 0) {
                                    black = true;
                                    goto done;
                                }
                            }
                        }
                        done:
                        if(black) {
                            newBitmap.SetPixel(x, y, GDI.Color.Black);
                        }
                    }
                }
            }
            return newBitmap;
            */
        }
    }
}
