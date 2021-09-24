using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace F360
{

    public enum ManeuverType
    {
        Undefined = 0,
        Vorbeifahren,
        Uberholen,

        Spurenwechsel_L,
        Spurenwechsel_R,
        
        Abbiegen_L,
        Abbiegen_R,

        Spurenwechsel_Any,
        Abbiegen_Any
    }

}