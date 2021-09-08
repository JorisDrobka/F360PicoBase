using UnityEngine;
using System;
using System.Collections;

/*
 *  from https://github.com/mortennobel/CameraLib4U/blob/master/Assets/_CameraLib4U/Scripts/SphericalCoordinates.cs
 *
 *
 *  also see: https://blog.nobel-joergensen.com/2010/10/22/spherical-coordinates-in-unity/
*/




/// <summary>
/// In mathematics, a spherical coordinate system is a coordinate system for 
/// three-dimensional space where the position of a point is specified by three numbers: 
/// the radial distance of that point from a fixed origin, its inclination angle measured 
/// from a fixed zenith direction, and the azimuth angle of its orthogonal projection on 
/// a reference plane that passes through the origin and is orthogonal to the zenith, 
/// measured from a fixed reference direction on that plane. 
/// 
/// The zenith direction is the up vector (0,1,0) and the azimuth is the right vector (1,0,0)
/// 
/// (From http://en.wikipedia.org/wiki/Spherical_coordinate_system )
///
/// </summary>

[Serializable]
public class PolarCoordinates  
{
	/// <summary>
	/// the radial distance of that point from a fixed origin.
	/// Radius must be >= 0
	/// </summary>
	public float radius;
	/// <summary>
	/// azimuth angle (in radian) of its orthogonal projection on 
	/// a reference plane that passes through the origin and is orthogonal to the zenith
	/// </summary>
	public float azimuth;
	/// <summary>
	/// elevation angle (in radian) from the reference plane 
	/// </summary>
	public float elevation;
	
	/// <summary>
	/// Converts a point from Spherical coordinates to Cartesian (using positive
    /// * Y as up)
	/// </summary>
	public Vector3 ToCartesian()
    {
		Vector3 res = new Vector3();
		PolarToCartesian(radius, azimuth, elevation, out res);
		return res;
	}
	
	/// <summary>
	/// Converts a point from Cartesian coordinates (using positive Y as up) to
    /// Spherical and stores the results in the store var. (Radius, Azimuth,
    /// Polar)
	/// </summary>
    public static PolarCoordinates CartesianToPolar(Vector3 cartCoords) 
    {
		PolarCoordinates store = new PolarCoordinates();        
		CartesianToPolar(cartCoords, out store.radius, out store.azimuth, out store.elevation);
		return store;
    }
	
    
	/// <summary>
	/// Converts a point from Spherical coordinates to Cartesian (using positive
    /// * Y as up). All angles are in radians.
	/// </summary>
	public static void PolarToCartesian(float radius, float polar, float elevation, out Vector3 outCart)
    {
		float a = radius * Mathf.Cos(elevation);
        outCart.x = a * Mathf.Cos(polar);
		outCart.y =	radius * Mathf.Sin(elevation);
		outCart.z = a * Mathf.Sin(polar);
	}
	
	/// <summary>
	/// Converts a point from Cartesian coordinates (using positive Y as up) to
    /// Spherical and stores the results in the store var. (Radius, Azimuth,
    /// Polar)
	/// </summary>
	public static void CartesianToPolar(Vector3 cartCoords, out float outRadius, out float outAzi, out float outElevation)
    {
		if (cartCoords.x == 0)
            cartCoords.x = Mathf.Epsilon;
        outRadius = Mathf.Sqrt((cartCoords.x * cartCoords.x)
                        + (cartCoords.y * cartCoords.y)
                        + (cartCoords.z * cartCoords.z));
        outAzi = Mathf.Atan(cartCoords.z / cartCoords.x);
        if (cartCoords.x < 0)
	 		outAzi += Mathf.PI;
        outElevation = Mathf.Asin(cartCoords.y / outRadius);
	}
}