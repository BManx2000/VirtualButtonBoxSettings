using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace VirtualButtonBoxSettings {
    enum ButtonType {
        Normal,
        TwoDirectionRotary,
        MultiPositionRotary
    }

    [DataContract]
    class GridButton {
        [DataMember]
        private int x;
        [DataMember]
        private int y;
        [DataMember]
        private int width;
        [DataMember]
        private int height;
        [DataMember]
        private string text;
        [DataMember]
        private double fontSize;
        [DataMember]
        private ButtonType buttonType;
        [DataMember]
        private double rotaryAngle;
        [DataMember]
        private KeyCombo keypress;
        [DataMember]
        private KeyCombo cwKeypress;
        [DataMember]
        private KeyCombo ccwKeypress;
        [DataMember]
        private List<KeyCombo> multiKeypresses;
        [DataMember]
        private int defaultKeypress;

        public ButtonGrid parent;

        [OnDeserialized]
        private void RemoveUnityNulls(StreamingContext c) {
            if (keypress != null && keypress.keypresses.Count == 0) {
                keypress = null;
            }
            if (cwKeypress != null && cwKeypress.keypresses.Count == 0) {
                cwKeypress = null;
            }
            if (ccwKeypress != null && ccwKeypress.keypresses.Count == 0) {
                ccwKeypress = null;
            }
            if (multiKeypresses != null) {
                for (int i = 0; i < multiKeypresses.Count; i++) {
                    if (multiKeypresses[i] != null && multiKeypresses[i].keypresses.Count == 0) {
                        multiKeypresses[i] = null;
                    }
                }
            }
        }

        public int X {
            get { return this.x; }
            set {
                if (this.CanMoveTo(value, this.y)) {
                    this.parent.RemoveButtonFromIndex(this);
                    this.x = value;
                    this.parent.AddButtonToIndex(this);
                    parent.parent.Save();
                }
            }
        }

        public int Y {
            get { return this.y; }
            set {
                if (this.CanMoveTo(this.x, value)) {
                    this.parent.RemoveButtonFromIndex(this);
                    this.y = value;
                    this.parent.AddButtonToIndex(this);
                    parent.parent.Save();
                }
            }
        }

        public int Width {
            get { return this.width; }
            set {
                if (this.CanSetWidthTo(value)) {
                    this.parent.RemoveButtonFromIndex(this);
                    this.width = value;
                    this.parent.AddButtonToIndex(this);
                    parent.parent.Save();
                }
            }
        }

        public int Height {
            get { return this.height; }
            set {
                if (this.CanSetHeightTo(value)) {
                    this.parent.RemoveButtonFromIndex(this);
                    this.height = value;
                    this.parent.AddButtonToIndex(this);
                    parent.parent.Save();
                }
            }
        }

        public double FontSize {
            get { return this.fontSize; }
            set {
                this.fontSize = Math.Max(value, 0.001);
                parent.parent.Save();
            }
        }

        public ButtonType ButtonType {
            get { return this.buttonType; }
            set {
                this.buttonType = value;
                parent.parent.Save();
            }
        }

        public double RotaryAngle {
            get { return this.rotaryAngle; }
            set {
                this.rotaryAngle = Math.Min(Math.Max(0.1, value), 180);
                parent.parent.Save();
            }
        }

        public string Text {
            get { return this.text; }
            set {
                this.text = value;
                parent.parent.Save();
            }
        }

        public KeyCombo Keypress {
            get { return this.keypress; }
            set {
                this.keypress = value;
                parent.parent.Save();
            }
        }

        public KeyCombo CWKeypress {
            get { return this.cwKeypress; }
            set {
                this.cwKeypress = value;
                parent.parent.Save();
            }
        }

        public KeyCombo CCWKeypress {
            get { return this.ccwKeypress; }
            set {
                this.ccwKeypress = value;
                parent.parent.Save();
            }
        }

        public int DefaultKeypress {
            get { return this.defaultKeypress; }
            set {
                this.defaultKeypress = Math.Max(Math.Min(Math.Max(0, value), this.multiKeypresses.Count - 1), 0);
                parent.parent.Save();
            }
        }

        public void AddKeypress() {
            this.multiKeypresses.Add(null);
            parent.parent.Save();
        }

        public void RemoveKeypress() {
            if (this.multiKeypresses.Count > 2) {
                this.multiKeypresses.RemoveAt(this.multiKeypresses.Count - 1);
                parent.parent.Save();
            }
        }

        public List<KeyCombo> MultiKeypresses {
            get { return this.multiKeypresses; }
        }

        public void SetMultiKeypress(int index, KeyCombo keypress) {
            this.multiKeypresses[index] = keypress;
            parent.parent.Save();
        }

        public GridButton(int x, int y) {
            this.x = x;
            this.y = y;
            this.width = 1;
            this.height = 1;
            this.text = "";
            this.fontSize = 0.01;
            this.keypress = null;
            this.buttonType = ButtonType.Normal;
            this.rotaryAngle = 30;
            this.defaultKeypress = 0;
            this.multiKeypresses = new List<KeyCombo>();
            this.multiKeypresses.Add(null);
            this.multiKeypresses.Add(null);
        }

        public bool CanMoveTo(int x, int y) {
            if(x < 0 || y < 0 || x + this.width > this.parent.GridWidth || y + this.height > this.parent.GridHeight) {
                return false;
            }
            for (; x < this.x + this.width; x++) {
                for (; y < this.y + this.height; y++) {
                    GridButton button = this.parent.ButtonAtPosition(x, y);
                    if(button != null && button != this) {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool CanSetHeightTo(int height) {
            if(height < 1) {
                return false;
            }
            if(height <= this.height) {
                return true;
            }
            if(this.y + height > this.parent.GridHeight) {
                return false;
            }
            for (int y = this.y + this.height; y < this.y + height; y++) {
                for (int x = this.x; x < this.x + this.width; x++) {
                    if (this.parent.ButtonAtPosition(x, y) != null) {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool CanSetWidthTo(int width) {
            if (width < 1) {
                return false;
            }
            if (width <= this.width) {
                return true;
            }
            if (this.x + width > this.parent.GridWidth) {
                return false;
            }
            for (int x = this.x + this.width; x < this.x + width; x++) {
                for (int y = this.y; y < this.y + this.height; y++) {
                    if (this.parent.ButtonAtPosition(x, y) != null) {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
