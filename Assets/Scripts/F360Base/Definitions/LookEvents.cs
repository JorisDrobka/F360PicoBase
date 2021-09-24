using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace F360
{

    public enum LookEventType
    {
        None=0,
        RearViewMirror=1,       //  DefaultMode=[ MinGaze, 0,5s ] Marker=[ icon ]
        SideMirror_L,           //  
        SideMirror_R,           //
        SideMirror_Any,         //
        ShoulderLook_L,         //  
        ShoulderLook_R,         //  


        FrontWindow,            //  DefaultMode=[ LookAway, 2s ] Marker=[ "Vorne" ]
        RearWindow,             //  DefaultMode=[ LookAway, 2s ] Marker=[ "Hinten" ]

        
        Check_Direction,        //  User must look in a direction, DefaultMode=[ MinGaze, 0,5s ] Marker=[ "Einsehen" ]
        Check_Direction_L,      //  
        Check_Direction_R,      //  
        Observe_Direction,      //  User must observe a direction. DefaultMode=[ LookAway, 2s ] Marker=[ "Einsehen" ]
        Observe_Direction_L,    //
        Observe_Direction_R,    //
        Observe_Situation,      //  User must observe the traffic. DefaultMode=[ LookAway, 2s ] Marker=[ "Beobachten" ]
        Follow_Marker,          //  User must follow marker. DefaultMode=[ LookAway, 2s ] Marker=[ "Folgen" ] 

        Console,                //  Car console
        Wheel,


        //  define maneuver types for serialization purposes 

        M_Spurwechsel_L,
        M_Spurwechsel_R,
        M_Uberholen,
        M_Abbiegen_L,
        M_Abbiegen_R,
        M_Vorbeifahren
    }


}