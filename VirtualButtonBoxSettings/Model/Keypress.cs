using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using SlimDX.DirectInput;

namespace VirtualButtonBoxSettings {
    [DataContract]
    public class Keypress {
        [DataMember]
        public readonly Key Key;
        [DataMember]
        public readonly int Scancode;
        [DataMember]
        public readonly bool Extended;

        public Keypress(Key key, int scancode, bool extended) {
            Key = key;
            Scancode = scancode;
            Extended = extended;
        }

        public override string ToString() {
            return Key.ToString();
        }
    }
}
