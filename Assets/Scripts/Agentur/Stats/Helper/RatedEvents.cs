



namespace F360.Users.Stats
{


    public interface IRatedEvent
    {
        int Index { get; }
        int Rating { get; }
        int Type { get; }

    }


    //-----------------------------------------------------------------------------------------------------------------

    /// @brief
    /// runtime representation of a single rated look event
    ///
    public struct RatedLookEvent : IRatedEvent
    {
        public int Index;
        public LookEventType Type;
        public int ManeuverIndex;
        public ManeuverType ManeuverType;
        public int Rating;
        

        public bool isPartOfManeuver()
        {
            return ManeuverIndex >= 0;
        }

        public bool MatchType(params LookEventType[] types)
        {
            for(int i = 0; i < types.Length; i++) {
                if(this.Type == types[i]) return true;
            }
            return false;
        }

        public RatedLookEvent(int i, LookEventType t, int r)
        {
            this.Index = 0;
            this.ManeuverIndex = -1;
            this.ManeuverType = ManeuverType.Undefined;
            this.Type = t;
            this.Rating = r;
        }
        public RatedLookEvent(int i, LookEventType t, int m, ManeuverType mt, int r)
        {
            this.Index = i;
            this.ManeuverIndex = m;
            this.ManeuverType = mt;
            this.Type = t;
            this.Rating = r;
        }
        public RatedLookEvent(RatedLookEvent other, int newrating)
        {
            this.Index = other.Index;
            this.ManeuverIndex = other.ManeuverIndex;
            this.ManeuverType = other.ManeuverType;
            this.Type = other.Type;
            this.Rating = newrating;
        }


        int IRatedEvent.Index { get { return Index; } }
        int IRatedEvent.Rating { get { return Rating; } }
        int IRatedEvent.Type { get { return (int)Type; } }
    }


    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// runtime representation of a single rated look event
    ///
    public struct RatedHazardEvent : IRatedEvent
    {
        public int Index;
        public HazardEventType Type;
        public int Rating;

        public RatedHazardEvent(HazardEventType t, int r)
        {
            this.Index = 0;
            this.Type = t;
            this.Rating = r;
        }
        public RatedHazardEvent(int i, HazardEventType t, int r)
        {
            this.Index = i;
            this.Type = t;
            this.Rating = r;
        }
        public RatedHazardEvent(RatedHazardEvent other, int newrating)
        {
            this.Index = other.Index;
            this.Type = other.Type;
            this.Rating = newrating;
        }

        int IRatedEvent.Index { get { return Index; } }
        int IRatedEvent.Rating { get { return Rating; } }
        int IRatedEvent.Type { get { return (int)Type; } }
    }



}