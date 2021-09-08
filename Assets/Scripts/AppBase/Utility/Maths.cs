using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;


//=================================================================================================================


[Flags]
public enum AxisConstraint
{
    X = 0x400 + 1,
    Y = 0x400 + 2,
    Z = 0x400 + 3
}

public enum Direction		{ north=0, northEast, east, southEast, south, southWest, west, northWest }

public enum Anchor2D		{ CenterLeft, Center, CenterRight, UpperLeft, UpperCenter, UpperRight, LowerLeft, LowerCenter, LowerRight }


[Serializable]
public struct RangeNrm
{
    [Range(0, 1)] public float min; 
    [Range(0, 1)] public float max;
    public float length { get { return Mathf.Clamp01(max-min); } }
    
    public bool hasZeroLength() { return Mathf.Approximately(length, 0); }

    public float GetMin(float absRange=1f)
    {
        return min * absRange;
    }
    public float GetMax(float absRange=1f)
    {
        return max * absRange;
    }
    public bool Contains(float t, float absRange=1f)
    {
        return t >= (min*absRange) && t <= (max*absRange);
    }

    public RangeNrm(float t)
    {
        t = Mathf.Clamp01(t);
        this.min = t;
        this.max = t;
    }

    public RangeNrm(float min, float max)
    {
        this.min = Mathf.Clamp01(min);
        this.max = Mathf.Clamp01(max);
        if(min > max)
        {
            min = max;
        }
    }

    public static RangeNrm fullRange { get { return _fullR; } }
    static RangeNrm _fullR = new RangeNrm(0, 1);

    public override bool Equals(object obj)
    {   
        if(!ReferenceEquals(obj, null) && obj is RangeNrm) {
            return Equals((RangeNrm)obj);
        }
        return false;
    }
    public bool Equals(RangeNrm other)
    {
        return Mathf.Approximately(min, other.min) && Mathf.Approximately(max, other.max);
    }
    public override int GetHashCode()
    {
        unchecked {
            return (min.GetHashCode() ^ 7) + (max.GetHashCode() ^ 17);
        }
    }
    public static bool operator==(RangeNrm a, RangeNrm b) {
        return a.Equals(b);
    }
    public static bool operator!=(RangeNrm a, RangeNrm b) {
        return !a.Equals(b);
    }
    
}


//-----------------------------------------------------------------------------------------------


namespace Utility
{


	public static partial class Math
    {
        

        //	equals

        const float defaultFloatingTolerance = 0.0001f;

        /// <summary>
        /// Test if floating point a equals b with a tolerance
        /// </summary>
        public static bool SloppyEquals_f(float a, float b, float tolerance = defaultFloatingTolerance)
        {
            return !(b < a - tolerance || b > a + tolerance);
        }
        /// <summary>
        /// Test if Vector2 a equals b with a tolerance
        /// </summary>
        public static bool SloppyEquals_2f(Vector2 a, Vector2 b, float tolerance = defaultFloatingTolerance)
        {
            return SloppyEquals_f(a.x, b.x, tolerance) && SloppyEquals_f(a.y, b.y, tolerance);
        }
        /// <summary>
        /// Test if Vector3 a equals b with a tolerance
        /// </summary>
        public static bool SloppyEquals_3f(Vector3 a, Vector3 b, float tolerance = defaultFloatingTolerance)
        {
            return 	SloppyEquals_f(a.x, b.x, tolerance) && 
					SloppyEquals_f(a.y, b.y, tolerance) && 
					SloppyEquals_f(a.z, b.z, tolerance);
        }
        /// <summary>
        /// Test if Vector3 a equals b with a tolerance on XZ plane, ignoring y value
        /// </summary>
        public static bool SloppyEquals_3to2f(Vector3 a, Vector3 b, float tolerance = defaultFloatingTolerance)
        {
            return SloppyEquals_f(a.x, b.x, tolerance) && SloppyEquals_f(a.z, b.z, tolerance);
        }

        //-----------------------------------------------------------------------------------------------

        /// <summary>
        /// standard range mapping
        /// </summary>
        public static float MapF(float val, float min1, float max1, float min2, float max2, bool clamp = true)
        {
            if (clamp)
                val = Mathf.Clamp(val, min1, max1);
            return ((max2 - min2) * ((val - min1) / (max1 - min1))) + min2;
        }

        //-----------------------------------------------------------------------------------------------

        //	angles

        public static float GetVector2AngleBetween(Vector2 a, Vector2 b)
        {
            float angle = Vector2.Angle(a, b);
            Vector3 cross = Vector3.Cross(a, b);

            if (cross.z > 0)
                angle = 360 - angle;
            return angle;
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F)
                angle += 360F;
            if (angle > 360F)
                angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }

		public static Vector2 Vector2FromDegree(float angle)
		{
			return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
		}
		public static Vector2 Vector2FromRadians(float angle)
		{
			return new Vector2 (Mathf.Cos(angle), Mathf.Sin(angle));
		}

        //-- Directions ---------------------------------------------------------------------------------
		
		const int angleDirStep = 45;
		
		public static Direction Get4Direction( Vector2 v )
		{
			v = v.normalized;
			
			Direction d = Direction.north;
			float angle = -Utility.Math.GetVector2AngleBetween(v, Vector2.up);
			if(angle < 0)
				angle += 360;

			if(angle < 45 || angle > 315)
				d = Direction.north;
			else if(angle < 135)
				d = Direction.east;
			else if(angle < 225)
				d = Direction.south;
			else
				d = Direction.west;
			return d;
		}

		public static Direction Get8Direction( Vector2 v )
		{
			v = v.normalized;
			
			Direction d = Direction.north;
			float angle = -Utility.Math.GetVector2AngleBetween(v, Vector2.up);
			if(angle < 0)
				angle += 360;
			
			if(angle < 22.5f || angle > 337.5f)
				d = Direction.north;
			else if(angle < 67.5f)
				d = Direction.northEast;
			else if(angle < 112.5f)
				d = Direction.east;
			else if(angle < 157.5f)
				d = Direction.southEast;
			else if(angle < 202.5f)
				d = Direction.south;
			else if(angle < 247.5f)
				d = Direction.southWest;
			else if(angle < 292.5f)
				d = Direction.west;
			else 
				d = Direction.northWest;			
			return d;
		}

        //-----------------------------------------------------------------------------------------------

        /// <summary>
        /// hypothetical 2D cross product
        /// </summary>
        public static float Cross2D2f(Vector2 v1, Vector2 v2)
        {
            return (v1.x * v2.y) - (v1.y * v2.x);
        }

        /// <summary>
        /// hypothetical 2D cross product with vector3s
        /// </summary>
        public static float Cross2D3f(Vector3 v1, Vector3 v2)
        {
            return (v1.x * v2.z) - (v1.z * v2.x);
        }

        //-----------------------------------------------------------------------------------------------

        /// <summary>
        /// returns true if two given lines intersect at any point [Vector3]
        /// </summary>
        public static bool LinesIntersect3f(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            return LinesIntersect2f(new Vector2(a.x, a.z), new Vector2(b.x, b.z), new Vector2(c.x, c.z), new Vector2(d.x, d.z));
        }

        //	returns true if two given lines intersect at any point [Vector2]
        public static bool LinesIntersect2f(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float numerator = ((a.y - c.y) * (d.x - c.x)) - ((a.x - c.x) * (d.y - c.y));
            float denominator = ((b.x - a.x) * (d.y - c.y)) - ((b.y - a.y) * (d.x - c.x));

            if (denominator == 0 && numerator != 0)
                // Lines are parallel.
                return false;

            // Lines are collinear or intersect at a single point.
            return true;
        }

        //	returns true if two given line segments intersect at any point [Vector3]
//        public static bool SegmentsIntersect3f(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
//        {
//            return SegmentsIntersect2f(new Vector2(p1.x, p1.z), new Vector2(p2.x, p2.z), new Vector2(p3.x, p3.z), new Vector2(p4.x, p4.z));
//        }
//
//        //	returns true if two given line segments intersect at any point [Vector2]
//        public static bool SegmentsIntersect2f(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
//        {
//            Vector2 a = p2 - p1;
//            Vector2 b = p3 - p4;
//            Vector2 c = p1 - p3;
//
//            float alphaNumerator = b.y * c.x - b.x * c.y;
//            float alphaDenominator = a.y * b.x - a.x * b.y;
//            float betaNumerator = a.x * c.y - a.y * c.x;
//            float betaDenominator = alphaDenominator; /*2013/07/05, fix by Deniz*/
//
//            bool doIntersect = true;
//
//            if (alphaDenominator == 0 || betaDenominator == 0)
//            {
//                doIntersect = false;
//            }
//            else
//            {
//
//                if (alphaDenominator > 0)
//                {
//                    if (alphaNumerator < 0 || alphaNumerator > alphaDenominator)
//                    {
//                        doIntersect = false;
//                    }
//                }
//                else if (alphaNumerator > 0 || alphaNumerator < alphaDenominator)
//                {
//                    doIntersect = false;
//                }
//
//                if (doIntersect && betaDenominator > 0)
//                {
//                    if (betaNumerator < 0 || betaNumerator > betaDenominator)
//                    {
//                        doIntersect = false;
//                    }
//                }
//                else if (betaNumerator > 0 || betaNumerator < betaDenominator)
//                {
//                    doIntersect = false;
//                }
//            }
//            return doIntersect;
//        }

        //		public static bool SegmentsIntersect3D(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, out Vector3 intersectionPoint)
        //		{
        //			Vector2 intersect2f;
        //			
        //			if(SegmentIntersects2D(new Vector2(p1.x, p1.z), new Vector2(p2.x, p2.z), 
        //			                       new Vector2(p3.x, p3.z), new Vector2(p4.x, p4.z), 
        //			                       out intersect2f))
        //			{
        //				//	hypotenuse = ankathete / sin(angle(ankathete, hypotenuse))
        //				Vector3 kathete 	= new Vector3(intersect2f.x, p1.y, intersect2f.y) - p1;
        //				Vector3 hypotenuse	= p2 - p1;	
        //				float angle			= Vector3.Angle( hypotenuse.normalized, kathete.normalized );
        //				
        //				float slope = Mathf.Tan(Mathf.Deg2Rad * angle) * kathete.magnitude;
        //				float y 	= p1.y < p2.y ? slope : -slope;
        //				intersectionPoint = new Vector3(intersect2f.x, p1.y + y, intersect2f.y);
        //				return true;
        //			}
        //			intersectionPoint = Vector3.zero;
        //			return false;
        //		}
        //		
        //		//	does this work???
        //		public static bool SegmentIntersects2D(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersectionPoint)
        //		{
        //			float firstLineSlopeX, firstLineSlopeY, secondLineSlopeX, secondLineSlopeY;
        //			
        //			firstLineSlopeX = p2.x - p1.x;
        //			firstLineSlopeY = p2.y - p1.y;
        //			
        //			secondLineSlopeX = p4.x - p3.x;
        //			secondLineSlopeY = p4.y - p3.y;
        //			
        //			float s, t;
        //			s = (-firstLineSlopeY * (p1.x - p3.x) + firstLineSlopeX * (p1.y - p3.y)) / (-secondLineSlopeX * firstLineSlopeY + firstLineSlopeX * secondLineSlopeY);
        //			t = (secondLineSlopeX * (p1.y - p3.y) - secondLineSlopeY * (p1.x - p3.x)) / (-secondLineSlopeX * firstLineSlopeY + firstLineSlopeX * secondLineSlopeY);
        //			
        //			if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
        //			{
        //				float intersectionPointX = p1.x + (t * firstLineSlopeX);
        //				float intersectionPointY = p1.y + (t * firstLineSlopeY);
        //				
        //				// Collision detected
        //				intersectionPoint = new Vector2(intersectionPointX, intersectionPointY);
        //				
        //				return true;
        //			}
        //			
        //			intersectionPoint = Vector2.zero;
        //			return false; // No collision
        //		}

        //-----------------------------------------------------------------------------------------------

        //	returns distance between point and a line defined by the positions AB
        public static float GetPointToLineDist(Vector3 point, Vector3 lineA, Vector3 lineB)
        {
            Vector3 v = lineB - lineA;
            Vector3 w = point - lineA;

            float c1 = Vector3.Dot(w, v);
            float c2 = Vector3.Dot(v, v);
            float b = c1 / c2;

            Vector3 pb = lineA + b * v;
            return Vector3.Distance(point, pb);
        }

        /// <summary>
        /// returns distance between a point and a line segment AB
        /// </summary>
        public static float GetPointToLineSegmentDist3f(Vector3 point, Vector3 segmentA, Vector3 segmentB)
        {
            Vector3 v = segmentB - segmentA;
            Vector3 w = point - segmentA;

            float c1 = Vector3.Dot(w, v);
            if (c1 <= 0)
                return Vector3.Distance(point, segmentA);

            float c2 = Vector3.Dot(v, v);
            if (c2 <= c1)
                return Vector3.Distance(point, segmentB);

            float b = c1 / c2;
            Vector3 pb = segmentA + b * v;
            return Vector3.Distance(point, pb);
        }

        /// <summary>
        /// returns distance between a point and a line segment AB
        /// </summary>
        public static float GetPointToLineSegmentDist2f(Vector2 point, Vector2 segmentA, Vector2 segmentB)
        {
            Vector2 v = segmentB - segmentA;
            Vector2 w = point - segmentA;

            float c1 = Vector2.Dot(w, v);
            if (c1 <= 0)
                return Vector2.Distance(point, segmentA);

            float c2 = Vector2.Dot(v, v);
            if (c2 <= c1)
                return Vector2.Distance(point, segmentB);

            float b = c1 / c2;
            Vector2 pb = segmentA + b * v;
            return Vector2.Distance(point, pb);
        }

        // Find the distance from this point to a line segment (which is not the same as from this 
        //  point to anywhere on an infinite line). Also returns the closest point.
        public static float DistanceToLineSegment(Vector2 A, Vector2 B, Vector2 P, out Vector2 closestPoint)
        {
            return Mathf.Sqrt(DistanceToLineSegmentSquared(A, B, P, out closestPoint));
        }

        public static float DistanceToLineSegment(Vector2 A, Vector2 B, Vector2 P)
        {
            Vector2 p;
            return Mathf.Sqrt(DistanceToLineSegmentSquared(A, B, P, out p));
        }

		public static float DistanceToLineSegmentSquared(Vector2 A, Vector2 B, Vector2 P)
		{
			Vector2 p;
			return DistanceToLineSegmentSquared(A, B, P, out p);
		}
        // Same as above, but avoid using Sqrt(), saves a new nanoseconds in cases where you only want 
        //  to compare several distances to find the smallest or largest, but don't need the distance
        public static float DistanceToLineSegmentSquared(Vector2 A, Vector2 B, Vector2 P, out Vector2 closestPoint)
        {
            // Compute length of line segment (squared) and handle special case of coincident points
            float segmentLengthSquared = A.SqrDist(B);
            if (segmentLengthSquared < 1E-7f)  // Arbitrary "close enough for government work" value
            {
                closestPoint = A;
                return P.SqrDist(closestPoint);
            }

            // Use the magic formula to compute the "projection" of this point on the infinite line
            Vector2 lineSegment = B - A;
            float t = Vector2.Dot(P - A, lineSegment) / segmentLengthSquared;

            // Handle the two cases where the projection is not on the line segment, and the case where 
            //  the projection is on the segment
            if (t <= 0)
                closestPoint = A;
            else if (t >= 1)
                closestPoint = B;
            else
                closestPoint = A + (lineSegment * t);
            return P.SqrDist(closestPoint);
        }

        private static float _dotProduct2D(Vector2 a, Vector2 b)
        {
            return a.x * b.x + a.y * b.y;
        }

		/// <summary>
		/// Returns closest distance between two line segments
		/// If distance is 0, the segments intersect
		/// </summary>
		public static float DistanceBetweenLineSegments(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
		{
			if(LineSegmentsIntersect(a1, a2, b1, b2))
			{
				return 0f;
			}
			float[] d = new float[4];
			d[0] = DistanceToLineSegmentSquared(a1, b1, b2);
			d[1] = DistanceToLineSegmentSquared(a2, b1, b2);
			d[2] = DistanceToLineSegmentSquared(b1, a1, a2);
			d[3] = DistanceToLineSegmentSquared(b2, a1, a2);
			int index = 0;
			float dist = d[0];
			for(int i = 1; i < 4; i++)
			{
				if(d[i] < dist)
				{
					dist = d[i];
					index = i;
				}
			}
			return Mathf.Sqrt( d[index] );
		}

		/// <summary>
		/// Returns closest distance between two line segments as well as the closest point between them.
		/// If distance is 0, the segments intersect at closestP
		/// </summary>
		public static float DistanceBetweenLineSegments(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, 
		                                                out Vector2 closestP1, out Vector2 closestP2)
		{
			IntersectionInfo info;
			if(LineSegmentsIntersect(a1, a2, b1, b2, out closestP1, out info))
			{
				closestP2 = closestP1;
				return 0f;
			}
			Vector2[] c = new Vector2[4];
			float[] d	= new float[4];
			d[0] = DistanceToLineSegmentSquared(b1, b2, a1, out c[0]);
			d[1] = DistanceToLineSegmentSquared(b1, b2, a2, out c[1]);
			d[2] = DistanceToLineSegmentSquared(a1, a2, b1, out c[2]);
			d[3] = DistanceToLineSegmentSquared(a1, a2, b2, out c[3]);
			int index = 0;
			float dist = d[0];
			for(int i = 1; i < 4; i++)
			{
				if(d[i] < dist)
				{
					dist = d[i];
					index = i;
				}
			}
			closestP1 = c[index];
			switch(index)
			{
			case 0:	closestP2 = a1; break;
			case 1: closestP2 = a2; break;
			case 2: closestP2 = b1; break;
			case 3: closestP2 = b2; break;
			default: closestP2 = Vector2.zero; break;
			}
			return Mathf.Sqrt( d[index] );
		}

        public static float GetPointPlaneDistance(Vector3 point, Vector3 plane, Vector3 planeNRM, out Vector3 onPlane)
        {
            float sb, sn, sd;
            sn = -Vector3.Dot(planeNRM.normalized, (point - plane));
            sd = Vector3.Dot(planeNRM, planeNRM);
            sb = sn / sd;

            onPlane = point + sb * planeNRM;
            return Vector3.Distance(point, onPlane);
        }

		/// <summary>
		/// Returns true if point is on the line segment AB
		/// </summary>
		public static bool TestPointOnLineSegment3f(Vector3 A, Vector3 B, Vector3 point, float tolerance=0.0001f)
		{
			float AB = Vector3.Distance(A, B);
			float AP = Vector3.Distance(A, point);
			float PB = Vector3.Distance(B, point);
			return SloppyEquals_f(AB, AP + PB, tolerance);
		}

		/// <summary>
		/// Returns true if point is on the line segment AB
		/// </summary>
		public static bool TestPointOnLineSegment2f(Vector2 A, Vector2 B, Vector2 point, float tolerance=0.0001f)
		{
			float AB = Vector2.Distance(A, B);
			float AP = Vector2.Distance(A, point);
			float PB = Vector2.Distance(point, B);
			return SloppyEquals_f(AB, AP + PB, tolerance);
		}

        //-----------------------------------------------------------------------------------------------

        /// <summary>
        /// returns point on line AB that is closest to point P
        /// </summary>
        [System.Obsolete("use DistanceToLineSegment() instead")]
        public static Vector2 GetClosestPointOnLineSegment2f(Vector2 A, Vector2 B, Vector2 P)
        {
            Vector2 AP = P - A;       //Vector from A to P   
            Vector2 AB = B - A;       //Vector from A to B  

            float magnitudeAB = AB.sqrMagnitude;     //Magnitude of AB vector (it's length squared)     
            float ABAPproduct = Vector2.Dot(AP, AB);    //The DOT product of a_to_p and a_to_b     
            float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point  

            if (distance < 0)     //Check if P projection is over vectorAB     
            {
                return A;

            }
            else if (distance > 1)
            {
                return B;
            }
            else
            {
                return A + AB * distance;
            }
        }

        //	returns point on line AB that is closest to point P
        //[System.Obsolete("use DistanceToLineSegment() instead")]
        //commented out attribute because there is no Vector3 version of DistanceToLineSegment
        public static Vector3 GetClosestPointOnLineSegment3f(Vector3 A, Vector3 B, Vector3 P)
        {
            Vector3 AB = B - A;
            Vector3 AP = P - A;
            float magnitudeAB = AB.sqrMagnitude;                //Magnitude of AB vector (it's length squared)     
            float ABAPproduct = Vector2.Dot(AP, AB);            //The DOT product of a_to_p and a_to_b     
            float distance = ABAPproduct / magnitudeAB;     //The normalized "distance" from a to your closest point  

            if (distance < 0)     //Check if P projection is over vectorAB     
            {
                return A;
            }
            else if (distance > 1)
            {
                return B;
            }
            else
            {
                Vector3 w2 = AP - (AB * Vector3.Dot(AP, AB) / AB.sqrMagnitude);
                return P - w2;
            }
        }

        //-----------------------------------------------------------------------------------------------

        //	ignores y coordinate while calculating intersection. Calcs y position afterwards using segmentA slope
        public static bool GetPointOfLineIntersection3f(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2, out Vector3 intersectionP)
        {
            intersectionP = Vector3.zero;

            //	project to XZ plane
            Vector2 a1f = new Vector2(a1.x, a1.z);
            Vector2 a2f = new Vector2(a2.x, a2.z);
            Vector2 b1f = new Vector2(b1.x, b1.z);
            Vector2 b2f = new Vector2(b2.x, b2.z);

            Vector2 intersection;
            if (GetPointOfLineIntersection2f(a1f, a2f, b1f, b2f, out intersection))
            {
                //	hypotenuse = ankathete / sin(angle(ankathete, hypotenuse))
                Vector3 kathete = new Vector3(intersection.x, a1.y, intersection.y) - a1;
                Vector3 hypotenuse = a2 - a1;
                float angle = Vector3.Angle(hypotenuse.normalized, kathete.normalized);

                float slope = Mathf.Tan(Mathf.Deg2Rad * angle) * kathete.magnitude;
                float y = a1.y < a2.y ? slope : -slope;
                intersectionP = new Vector3(intersection.x, a1.y + y, intersection.y);
                return true;
            }
            return false;
        }

        public static bool GetPointOfLineIntersection2f(Vector2 a1, Vector2 a2, Vector2 b1,
                                                        Vector2 b2, out Vector2 intersection)
        {
            // Get A,B,C of first line - points : a1 to a2
            float A1 = a2.y - a1.y;
            float B1 = a1.x - a2.x;
            float C1 = A1 * a1.x + B1 * a1.y;

            // Get A,B,C of second line - points : b1 to b2
            float A2 = b2.y - b1.y;
            float B2 = b1.x - b2.x;
            float C2 = A2 * b1.x + B2 * b1.y;

            // Get delta and check if the lines are parallel
            float delta = A1 * B2 - A2 * B1;
            if (delta == 0)
            {
                intersection = Vector2.zero;
                return false;
            }
            else
            {
                // now return the Vector2 intersection point
                intersection = new Vector2((B2 * C1 - B1 * C2) / delta, (A1 * C2 - A2 * C1) / delta);
                return true;
            }
        }

        //-----------------------------------------------------------------------------------------------

		/// <summary>
		/// returns wether a line is intersecting a circle, on which points and how
		///	0 - no intersection, 1 - one intersection (intersectA), 2 two intersections (intersectB will be the enter intersection)
		/// </summary>
        public static int LineCircleIntersection2f(Vector2 circle, float radius, Vector2 lineA, Vector2 lineB, out Vector2 exitP, out Vector2 enterP)
        {
            Vector2 v = (lineB - lineA).normalized;
            Vector2 proj = lineA + Vector2.Dot(circle - lineA, v) * v;
            float distSqr = Vector2.Distance(circle, proj);

            if (SloppyEquals_f(distSqr, radius))
            {
                //	line touches circle at one point
                float offset = Mathf.Sqrt(radius * radius - distSqr);
                exitP = proj + offset * v;
                enterP = proj - offset * v;
                if (GetPointToLineDist(exitP, lineA, lineB) > GetPointToLineDist(enterP, lineA, lineB))
                {
                    exitP = enterP;
                }
                return 1;
            }
            else if (distSqr < radius)
            {
                //	line crosses circle, two intersections
                float offset = Mathf.Sqrt(radius * radius - distSqr);
                exitP = proj + offset * v;
                enterP = proj - offset * v;
                return 2;
            }
            else
            {
                //	no intersection
                exitP = Vector2.zero;
                enterP = Vector2.zero;
                return 0;
            }
        }

        public static bool LineCircleIntersect_Simple3f(Vector3 p, float radius, Vector2 linePoint1, Vector2 linePoint2)
        {
            return LineCircleIntersect_Simple2f(new Vector2(p.x, p.z), radius, linePoint1, linePoint2);
        }

        public static bool LineCircleIntersect_Simple2f(Vector2 p, float radius, Vector2 linePoint1, Vector2 linePoint2)
        {
            Vector2 p1 = new Vector2(linePoint1.x, linePoint1.y);
            Vector2 p2 = new Vector2(linePoint2.x, linePoint2.y);
            p1.x -= p.x;
            p1.y -= p.y;
            p2.x -= p.x;
            p2.y -= p.y;
            float dx = p2.x - p1.x;
            float dy = p2.y - p1.y;
            float dr = (float)Mathf.Sqrt((dx * dx) + (dy * dy));
            float D = (p1.x * p2.y) - (p2.x * p1.y);

            float di = (radius * radius) * (dr * dr) - (D * D);

            if (di < 0) return false;
            else return true;
        }

		public static bool TestSegmentInCircle(Vector2 circle, float radius, Vector2 segmentA, Vector2 segmentB)
		{
			return DistanceToLineSegment(segmentA, segmentB, circle) <= radius;
		}

//		/// <summary>
//		///
//		/// </summary>
//		public static bool SegmentCircleIntersection(Vector2 segmentA, Vector2 segmentB, Vector2 circle, float radius,
//		                                             out Vector2 intersectA, out Vector2 intersectB)
//		{
//			intersectA = Vector2.zero;
//			intersectB = Vector2.zero;
//			Vector2 closest;
//			float d = DistanceToLineSegment(segmentA, segmentB, circle, out closest);
//			if(d < radius)
//			{
//
//			}
//			return 0;
//		}



		/// <summary>
		/// returns wether a linesegment is intersecting a circle, on which points and how
		///	0 - no intersection, 
		/// 1 - one intersection (intersectA), 
		/// 2 - two intersections (intersectB), 
		/// 4 - segment inside circle
		/// from stackoverflow.com/questions/23016676/line-segment-and-circle-intersection
		/// </summary>
		/// <returns>The line circle intersections.</returns>
		public static int TestSegmentInCircle(Vector2 circle, float radius, Vector2 segmentA, Vector2 segmentB,
		                                       		out Vector2 intersection1, out Vector2 intersection2)
		{
			if(!TestSegmentInCircle(circle, radius, segmentA, segmentB))
			{
				//	no intersection
				intersection1 = new Vector2(float.NaN, float.NaN);
				intersection2 = new Vector2(float.NaN, float.NaN);
				return 0;
			}
			else 
			{
				float dx, dy, A, B, C, det, t;
				
				dx = segmentB.x - segmentA.x;
				dy = segmentB.y - segmentA.y;
				
				A = dx * dx + dy * dy;
				B = 2 * (dx * (segmentA.x - circle.x) + dy * (segmentA.y - circle.y));
				C = (segmentA.x - circle.x) * (segmentA.x - circle.x) + (segmentA.y - circle.y) * (segmentA.y - circle.y) - radius * radius;
				
				det = B * B - 4 * A * C;
				if ((A <= 0.0000001) || (det < 0))
				{
					//	inside radius
					intersection1 = new Vector2(float.NaN, float.NaN);
					intersection2 = new Vector2(float.NaN, float.NaN);
					return 4;
				}
				else if (det == 0)
				{
					// One solution.
					t = -B / (2 * A);
					intersection1 = new Vector2(segmentA.x + t * dx, segmentA.y + t * dy);
					intersection2 = new Vector2(float.NaN, float.NaN);
					return 1;
				}
				else
				{
					// Two solutions.
					t = (float)((-B + det) / (2 * A));
					intersection1 = new Vector2(segmentA.x + t * dx, segmentA.y + t * dy);
					t = (float)((-B - det) / (2 * A));
					intersection2 = new Vector2(segmentA.x + t * dx, segmentA.y + t * dy);
					return 2;
				}
			}
		}

		public static bool CircleTriangleIntersection(Vector2 circle, float radius, Vector2 a, Vector2 b, Vector2 c)
		{
			return TestSegmentInCircle(circle, radius, a, b)
				|| TestSegmentInCircle(circle, radius, b, c)
				|| TestSegmentInCircle(circle, radius, c, a);
		}

        //-----------------------------------------------------------------------------------------------

        //	returns wether or not given point is inside segment AB
        public static bool PointInSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            if (a.x != b.x)     //	segment not vertical
            {
                if (a.x <= point.x && point.x <= b.x)
                    return true;
                if (a.x >= point.x && point.x >= b.x)
                    return true;
            }
            else                //	segment vertical, test y
            {
                if (a.y <= point.y && point.y <= b.y)
                    return true;
                if (a.y >= point.y && point.y >= b.y)
                    return true;
            }
            return false;
        }

        //-----------------------------------------------------------------------------------------------

        /// @brief returns wether a given segment and plane intersect, how, and at which point
        ///	    return values are: 
        ///	    0 - no intersection
        ///	    1 - intersection in <intersectPoint>
        ///	    2 - segment lies in plane
        public static int PlaneSegmentIntersection(Vector3 segmentA, Vector3 segmentB,
                                                   Vector3 planeP, Vector3 planeNRM,
                                                   out Vector3 intersectPoint)
        {
            intersectPoint = Vector3.zero;

            Vector3 u = segmentB - segmentA;
            Vector3 w = segmentA - planeP;

            float d = Vector3.Dot(planeNRM, u);
            float n = -Vector3.Dot(planeNRM, w);

            if (Mathf.Abs(d) < 0.001f)          //	segment is parallel to plane
            {
                if (n == 0)                     //	segment lies in plane
                    return 2;
                else
                    return 0;                   //	no intersection
            }
            float sI = n / d;
            if (sI < 0 || sI > 1)
                return 0;                       //	no intersection

            intersectPoint = segmentA + u * sI;
            return 1;
        }

        //-----------------------------------------------------------------------------------------------

        public static float CalcAverage(params float[] vals)
        {
            float sum = 0;
            for (int i = 0; i < vals.Length; i++)
                sum += vals[i];
            return sum / (float)vals.Length;
        }

        public static Vector2 CalcCentroid(IEnumerable<Vector2> positions)
        {
            Vector2 sum = new Vector2();
            foreach (Vector2 p in positions)
            {
                sum += p;
            }
            return sum / positions.Count();
        }
        public static Vector3 CalcCentroid(IEnumerable<Vector3> positions)
        {
            Vector3 sum = new Vector3();
            foreach (Vector3 p in positions)
            {
                sum += p;
            }
            return sum / positions.Count();
        }

        public static Vector2 CalcCentroid(List<Vector2> positions)
        {
            Vector2 sum = new Vector2();
            for (int i = 0; i < positions.Count; i++) sum += positions[i];
            return sum / positions.Count;
        }

        public static Vector3 CalcCentroid(List<Vector3> positions)
        {
            Vector3 sum = new Vector3();
            for (int i = 0; i < positions.Count; i++) sum += positions[i];
            return sum / positions.Count;
        }

		//-----------------------------------------------------------------------------------------------

		/// <summary>
		/// constraint a point to the bounds of this lookup
		/// </summary>
		public static Vector2 restraintToRect(Vector2 point, Rect bounds, float offset=0.001f)
		{
			if(bounds.width > 0 
			   && bounds.height > 0 
			   && !float.IsNaN(point.x)
			   && !float.IsNaN(point.y)
			   && !bounds.Contains(point))
			{
				Rect r = bounds;
				Vector2 p1 = new Vector2(r.x, r.y);
				Vector2 p2 = new Vector2(r.x+r.width, r.y);
				Vector2 p3 = new Vector2(r.x+r.width, r.y+r.height);
				Vector2 p4 = new Vector2(r.x, r.y+r.height);
				Vector2[] closest = new Vector2[4];

				float[] d = new float[4];
				d[0] = DistanceToLineSegmentSquared(p1, p2, point, out closest[0]);
				d[1] = DistanceToLineSegmentSquared(p2, p3, point, out closest[1]);
				d[2] = DistanceToLineSegmentSquared(p3, p4, point, out closest[2]);
				d[3] = DistanceToLineSegmentSquared(p4, p1, point, out closest[3]);

				int c=-1;
				float dist = float.MaxValue;
				for(int i = 0; i < 4; i++)
				{
					if(d[i] < dist)
					{
						c = i;
						dist = d[i];
					}
				}
				point = closest[c] + (closest[c] - point).normalized * offset;
			}
			return point;
		}

        //-----------------------------------------------------------------------------------------------

        //	truncates given floating point number after n digits
        public static float TruncateFloat(float f, int digits)
        {
            float d = 10; for (int i = 1; i < digits; i++) d *= 10;
            return Mathf.RoundToInt(f * d) / d;
        }

        //-----------------------------------------------------------------------------------------------
	

        public static float AreaOfTriangle(Vector2 a, Vector2 b, Vector2 c)
        {
            float abLength = (a - b).magnitude;
            float acLength = (a - c).magnitude;
            float bcLength = (b - c).magnitude;
            // Heron's formula
            float s = (abLength + acLength + bcLength) / 2;
            return Mathf.Sqrt(s * (s - abLength) * (s - acLength) * (s - bcLength));
        }
        public static float AreaOfTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            float abLength = (a - b).magnitude;
            float acLength = (a - c).magnitude;
            float bcLength = (b - c).magnitude;
            // Heron's formula
            float s = (abLength + acLength + bcLength) / 2;
            return Mathf.Sqrt(s * (s - abLength) * (s - acLength) * (s - bcLength));
        }
			

        // based on http://mathworld.wolfram.com/TrianglePointPicking.html
        public static Vector2 RandomPointInsideTriangle(Vector2 a, Vector2 b, Vector2 c)
        {

            Vector2 aOrigin = Vector2.zero;
            Vector2 bOrigin = b - a;
            Vector2 cOrigin = c - a;

            Vector2 randomInsideQuad = Random.value * bOrigin + Random.value * cOrigin;
            bool isInsideTri = IsPointInsideTriangle(randomInsideQuad, aOrigin, bOrigin, cOrigin);
            if (!isInsideTri)
            {
                Vector2 quadCenter = (bOrigin + cOrigin) * 0.5f;
                randomInsideQuad -= quadCenter;
                randomInsideQuad.x *= -1;
                randomInsideQuad.y *= -1;
                randomInsideQuad += quadCenter;
            }
            randomInsideQuad += a;

            return randomInsideQuad;
        }

        public static Vector3 RandomPointInsideTriangle(Vector3 a, Vector3 b, Vector3 c)
        {

            //Vector3 aOrigin = Vector3.zero;
            Vector3 bOrigin = b - a;
            Vector3 cOrigin = c - a;
            Vector3 crossPoint = (bOrigin - cOrigin) * 0.5f;
            Vector3 crossPointOffset = cOrigin + crossPoint;
            Vector3 normal = Vector3.Cross(bOrigin, cOrigin);
            Vector3 planeNormal = Vector3.Cross(normal, crossPoint);
            Plane crossPlane = new Plane(planeNormal.normalized, bOrigin);


            //var normal = (bOrigin + cOrigin).normalized;
            //Plane plane = new Plane(normal, bOrigin);

            Vector3 randomInsideQuad = Random.value * bOrigin + Random.value * cOrigin;


            if (crossPlane.GetDistanceToPoint(randomInsideQuad) > 0)
            {
                Vector3 localToCross = randomInsideQuad - crossPointOffset;
                randomInsideQuad -= localToCross * 2;
            }


            //var distance = plane.GetDistanceToPoint(randomInsideQuad);

            //if (distance > 0)
            //{
            //    Vector3 backVector = normal * -distance;
            //    randomInsideQuad += backVector;
            //}

            //bool isInsideTri = IsPointInsideTriangle(randomInsideQuad, aOrigin, bOrigin, cOrigin);
            //if (!isInsideTri)
            //{
            //    Vector2 quadCenter = (bOrigin + cOrigin) * 0.5f;
            //    randomInsideQuad -= quadCenter;
            //    randomInsideQuad.x *= -1;
            //    randomInsideQuad.y *= -1;
            //    randomInsideQuad += quadCenter;
            //}
            randomInsideQuad += a;

            return randomInsideQuad;
        }

        //http://stackoverflow.com/questions/2049582/how-to-determine-a-point-in-a-triangle
        public static bool IsPointInsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            var s = a.y * c.x - a.x * c.y + (c.y - a.y) * point.x + (a.x - c.x) * point.y;
            var t = a.x * b.y - a.y * b.x + (a.y - b.y) * point.x + (b.x - a.x) * point.y;

            if ((s < 0) != (t < 0))
                return false;

            var A = -b.y * c.x + a.y * (c.x - b.x) + a.x * (b.y - c.y) + b.x * c.y;
            if (A < 0.0)
            {
                s = -s;
                t = -t;
                A = -A;
            }
            return s > 0 && t > 0 && (s + t) < A;
        }

		/// <summary>
		/// calculates a triangles circumcenter & radius
		/// code from http://blog.ivank.net/basic-geometry-functions.html
		/// </summary>
		public static Vector2 CalcTriangleCircumcenter(Vector2 a, Vector2 b, Vector2 c, out float radius)
		{
			radius = 0;

			// m1 - center of (a,b), the normal goes through it
			var f1 = (b.x - a.x) / (a.y - b.y);
			var m1 = new Vector2((a.x + b.x)/2, (a.y + b.y)/2);
			var g1 = m1.y - f1*m1.x;

			var f2 = (c.x - b.x) / (b.y - c.y);
			var m2 = new Vector2((b.x + c.x)/2, (b.y + c.y)/2);
			var g2 = m2.y - f2*m2.x;

		   // degenerated cases
		   // - 3 points on a line
			if (f1 == f2) return Vector2.one * -100;
			// - a, b have the same height -> slope of normal of |ab| = infinity
			else if(a.y == b.y) return new Vector2(m1.x, f2*m1.x + g2);
			else if(b.y == c.y) return new Vector2(m2.x, f1*m2.x + g1);

			var x = (g2-g1) / (f1 - f2);
			Vector2 circumcenter = new Vector2(x, f1*x + g1);
			radius = Vector2.Distance(circumcenter, a);
			return circumcenter;
		}



		//=================================================================================================================

        /// <summary>
        /// Test if two line segments intersect.
        /// Based on the top answer on http://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect
        /// </summary>
        /// <param name="p1">Start point of the first line</param>
        /// <param name="p2">End point of the first line</param>
        /// <param name="q1">Start point of the second line</param>
        /// <param name="q2">End point of the second line</param>
        /// <param name="pointOfIntersection">The point of intersection will be written to this Vector2. It will</param>
        /// <param name="intersectionInfo">Additional info about the test.</param>
        /// <returns>true if the line segments intersect.</returns>
        public enum IntersectionInfo
        {
            Collinear,
            Parallel,
            Intersecting,
            NonIntersecting
        }
        static public bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2, out Vector2 pointOfIntersection, out IntersectionInfo intersectionInfo)
        {
            Vector2 r = p2 - p1;
            Vector2 s = q2 - q1;
            float rXs = Vector3.Cross(r, s).z;
            float qpXr = Vector3.Cross((q1 - p1), r).z;

            //t = (q − p) × s / (r × s)
            float t = Vector3.Cross((q1 - p1), s).z / rXs;

            //u = (q − p) × r / (r × s)
            float u = qpXr / rXs;

            //If r × s = 0 and (q − p) × r = 0, then the two lines are collinear.
            if (rXs == 0 && qpXr == 0)
            {
                pointOfIntersection = new Vector2(float.NaN, float.NaN);
                intersectionInfo = IntersectionInfo.Collinear;
                return false;
            }
            else if (rXs == 0 && qpXr != 0) // If r × s = 0 and(q − p) × r ≠ 0, then the two lines are parallel and non-intersecting.
            {
                pointOfIntersection = new Vector2(float.NaN, float.NaN);
                intersectionInfo = IntersectionInfo.Parallel;
                return false;
            }
            else if (rXs != 0 && t <= 1 && t >= 0 && u <= 1 && u >= 0) // If r × s ≠ 0 and 0 ≤ t ≤ 1 and 0 ≤ u ≤ 1, the two line segments meet at the point p + t r = q + u s
            {
                pointOfIntersection = p1 + t * r;
                intersectionInfo = IntersectionInfo.Intersecting;
                return true;
            }
            else
            {
                // Otherwise, the two line segments are not parallel but do not intersect.
                pointOfIntersection = new Vector2(float.NaN, float.NaN);
                intersectionInfo = IntersectionInfo.NonIntersecting;
                return false;
            }
        }
        static public bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            Vector2 dummy;
            IntersectionInfo info;
            return LineSegmentsIntersect(p1, p2, q1, q2, out dummy, out info);
        }
        /// <summary>
        /// Test if a point is left of, right of or on an infinite 2D line.
        /// </summary>
        /// <returns>
        /// greater than 0 for point left of the line through lineStart to lineEnd
        /// equal 0 for point on the line
        /// less than 0 for point right of the line
        /// </returns>
        public static float IsLeft(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
        {
            return ((lineEnd.x - lineStart.x) * (point.y - lineStart.y) - (point.x - lineStart.x) * (lineEnd.y - lineStart.y));
        }

        /// <summary>
        /// Project a vector onto another one. (Orthogonal projection)
        /// </summary>
        /// <param name="vec">The vector that will be projected.</param>
        /// <param name="ontoVec">The vector to be projected on.</param>
        /// <returns>The vector component of vec in the direction of ontoVec. Returns the zero vector when ontoVec is zero.</returns>
        public static Vector2 Project(Vector2 vec, Vector2 ontoVec)
        {
            float ontoDot = Vector2.Dot(ontoVec, ontoVec);
            if (ontoDot != 0)
                return (Vector2.Dot(vec, ontoVec) / ontoDot) * ontoVec;
            else
                return Vector2.zero;
        }

        //=================================================================================================================


        /// <summary>
        /// Fits a plane into a given point set with at least 3 points and returns a best-fit plane centroid+normal representation.
        /// </summary>
        /// <returns><c>true</c>, if plane by averaging was fitted, <c>false</c> otherwise.</returns>
        /// <param name="points">Points to average the plane out.</param>
        /// <param name="planeP">Plane centroid.</param>
        /// <param name="planeNRM">Plane normal.</param>
        public static bool BestFitPlaneByAveraging(IEnumerable<Vector3> points, out Vector3 planeP, out Vector3 planeNRM)
        {

            // Assume at least three points

            IEnumerator<Vector3> a = points.GetEnumerator();
            IEnumerator<Vector3> b = points.GetEnumerator();

            Vector3 centroid = Vector3.zero;
            Vector3 normal = Vector3.zero;

            b.MoveNext();
            int count = 0;
            while (a.MoveNext())
            {
                if (!b.MoveNext())
                {
                    b = points.GetEnumerator();
                    // b.Reset(); Reset is not supported when using yield
                    b.MoveNext();
                }
                Vector3 i = a.Current;
                Vector3 j = b.Current;

                normal.x += (i.y - j.y) * (i.z + j.z); // Project on yz
                normal.y += (i.z - j.z) * (i.x + j.x); // Project on xz
                normal.z += (i.x - j.x) * (i.y + j.y); // Project on xy
                centroid += j;
                count++;
            }
            if (count < 3)
            {
                planeP = centroid / (float)count;
                planeNRM = Vector3.up;
                return false;
            }
            else
            {
                planeP = centroid / (float)count;
                planeNRM = normal.normalized;
                return true;
            }
        }

        /// <summary>
        /// Calculate the area of the non-self-intersecting polygon.
        /// </summary>
        /// <param name="polygon">The vertices of the polygon.</param>
        /// <returns></returns>
        public static float SignedAreaOfPolygon(IEnumerable<Vector2> verts)
        {
            float signedArea = 0;
            Vector2[] vertices = verts.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                signedArea += 0.5f * (vertices[i].x * vertices[(i + 1) % vertices.Length].y - vertices[(i + 1) % vertices.Length].x * vertices[i].y);
            }
            return signedArea;
        }
        /// <summary>
        /// Calculate the area of the non-self-intersecting polygon that is in the XZ plane.
        /// </summary>
        /// <param name="polygon">The vertices of the polygon.</param>
        /// <returns></returns>
        public static float SignedAreaOfPolygon(IEnumerable<Vector3> verts)
        {
            return SignedAreaOfPolygon(verts.Select((vert) => new Vector2(vert.x, vert.z)));
        }


		//=================================================================================================================

		public static IEnumerable<Vector2> GetRandomPointsInsideCircle(Vector2 circleP, float radius, int num, float constraint=0f)
		{
			List<Vector2> points = new List<Vector2> ();
			int att = 1000;
			int fails = 0;
			while(att >= 0)
			{
				att--;
				Vector2 p = Random.insideUnitCircle * radius;
				if(constraint > 0)
				{
					for(int i = 0; i < points.Count; i++)
					{
						if(Vector2.Distance(points[i], p) < constraint)
						{
							fails++;
							if(fails % 20 == 0)
							{
								constraint *= 0.8f;
								fails = 0;
							}
							continue;
						}
					}
					points.Add (p);
					yield return p;
				}
			}
		}
    }
}
//=================================================================================================================
