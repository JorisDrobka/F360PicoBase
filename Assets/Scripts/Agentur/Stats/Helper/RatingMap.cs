using UnityEngine;

namespace F360.Users.Stats
{

    /// @brief
    /// Runtime representation of total & categorized ratings for DriveVR / Exam sessions.
    ///
    public class RatingMap
    {
        public int Total = Constants.RATING_NONE;
        public int Maneuvers = Constants.RATING_NONE;
        public int Awareness = Constants.RATING_NONE;
        public int Attention = Constants.RATING_NONE;
        public int Hazards = Constants.RATING_NONE;
        public int Anticipation = Constants.RATING_NONE;

        private int addedCount = 0;

        public bool isBaked()
        {
            return addedCount == 0;
        }

        public bool Bake()
        {
            if(addedCount > 0)
            {
                addedCount += 1;
                Total = Mathf.RoundToInt(Total / addedCount);
                Maneuvers = Mathf.RoundToInt(Maneuvers / addedCount);
                Awareness = Mathf.RoundToInt(Awareness / addedCount);
                Attention = Mathf.RoundToInt(Attention / addedCount);
                Hazards = Mathf.RoundToInt(Hazards / addedCount);
                Anticipation = Mathf.RoundToInt(Anticipation / addedCount);
                addedCount = 0;
                return true;
            }
            return false;
        }

        public void AddMap(RatingMap other)
        {
            Total += other.Total;
            Maneuvers += other.Maneuvers;
            Awareness += other.Awareness;
            Attention += other.Attention;
            Hazards += other.Hazards;
            Anticipation += other.Anticipation;
            addedCount++;
        }

        public void Clear()
        {
            Total = 0;
            Maneuvers = 0;
            Awareness = 0;
            Attention = 0;
            Hazards = 0;
            Anticipation = 0;
            addedCount = 0;
        }

        public bool HasValue(RatingType rating)
        {
            switch(rating)
            {
                case RatingType.Total:          return Total >= Constants.RATING_MIN;
                case RatingType.Maneuvers:      return Maneuvers >= Constants.RATING_MIN;
                case RatingType.Awareness:      return Awareness >= Constants.RATING_MIN;
                case RatingType.Attention:      return Attention >= Constants.RATING_MIN;
                case RatingType.Hazards:        return Hazards >= Constants.RATING_MIN;
                case RatingType.Anticipation:   return Anticipation >= Constants.RATING_MIN;
                default:                        return false;
            }
        }

        public int GetValue(RatingType rating)
        {
            switch(rating)
            {
                case RatingType.Total:          return Total;
                case RatingType.Maneuvers:      return Maneuvers;
                case RatingType.Awareness:      return Awareness;
                case RatingType.Attention:      return Attention;
                case RatingType.Hazards:        return Hazards;
                case RatingType.Anticipation:   return Anticipation;
                default:                        return Constants.RATING_NONE;
            }
        }


    }



}

