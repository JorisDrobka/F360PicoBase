using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using F360.Data;
using F360.Data.Runtime;


namespace F360.Users.Stats
{
    
    public enum DriveLocation
    {
        Undefined = 0,
        Innercity=1,
        Countryside=2,
        Highway=3
    }


    public enum TrafficParticipant
    {
        None = 0,
        Children=1,
        Pedestrian=2,
        Bicycle=3,
        Car=4,
        PublicTransport=5
    }


    public static class TrafficHelper
    {

        public static bool ParseLocation(string raw, out DriveLocation location)
        {
            raw = raw.ToLower();
            location = DriveLocation.Undefined;
            switch(raw)
            {
                case "innenstadt":  location = DriveLocation.Innercity; return true;
                case "landstraße":
                case "landstrasse": location = DriveLocation.Countryside; return true;
                case "autobahn":    location = DriveLocation.Highway; return true;
                default:            return false;
            }
        }

        public static string Readable(this DriveLocation location)
        {   
            switch(location)
            {
                case DriveLocation.Innercity:    return "Innenstadt";
                case DriveLocation.Countryside:  return "Landstraße";
                case DriveLocation.Highway:      return "Autobahn";
                default:                        return "UNDEFINED";
            }
        }




        public static RangeInt GetTrafficRangeMap(int max=5)
        {   
            return new RangeInt(1, Math.Min(max, 5));
        }

        public static RangeInt GetLocationRangeMap()
        {
            return new RangeInt(1, 3);
        }

        public static IEnumerable<TrafficParticipant> GetParticipantsFromTags(params string[] tags)
        {
            foreach(var k in map.Keys)
            {
                for(int i = 0; i < tags.Length; i++)
                {
                    if(map[k].Contains(tags[i])) { yield return k; break; }
                }
            }
        }
        public static IEnumerable<TrafficParticipant> GetParticipantsFromTags(params Tag[] tags)
        {
            foreach(var k in map.Keys)
            {
                for(int i = 0; i < tags.Length; i++)
                {
                    if(!string.IsNullOrEmpty(tags[i].label))
                    {
                        if(map[k].Contains(tags[i].label.ToLower())) { yield return k; break; }
                    }
                    else
                    {
                        Debug.LogWarning("empty tag! k=" + k);
                    }
                }
            }
        }

        public static bool FindInTags(TrafficParticipant p, params string[] tags)
        {
            return GetParticipantsFromTags(tags).Contains(p);
        }

        public static bool FindInTags(TrafficParticipant p, params Tag[] tags)
        {
            return GetParticipantsFromTags(tags).Contains(p);
        }

        public static bool MatchFromTags(IEnumerable<TrafficParticipant> set, params string[] tags)
        {
            var p = GetParticipantsFromTags(tags);
            foreach(var pp in set) if(p.Contains(pp)) return true;
            return false;
        }
        public static bool MatchFromTags(IEnumerable<TrafficParticipant> set, params Tag[] tags)
        {
            var p = GetParticipantsFromTags(tags);
            foreach(var pp in set) if(p.Contains(pp)) return true;
            return false;
        }


        static Dictionary<TrafficParticipant, string[]> map;

        static TrafficHelper()
        {
            map = new Dictionary<TrafficParticipant, string[]>();
            map.Add(TrafficParticipant.Pedestrian, new string[] {
                "fußgänger",
            });
            map.Add(TrafficParticipant.Children, new string[] {
                "kind", "kinder"
            });
            map.Add(TrafficParticipant.Bicycle, new string[] {
                "rad", "fahrrad", "radfahrer", "fahrradfahrer"
            });
            map.Add(TrafficParticipant.Car, new string[] {
                "auto", "autos", "pkw"
            });
            map.Add(TrafficParticipant.PublicTransport, new string[] {
                "straßenbahn", "bus", "öffentlicher verkehr"
            });
        }

    }


}