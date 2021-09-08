using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;



//== TYPES ========================================================================================================

#region types



#endregion


//=================================================================================================================

namespace Utility
{

	#region misc


	public enum TimeFormat
	{
		Minutes,
		Seconds,
		Millis
	}

	public static class Misc
	{
		
		//-- Array Search -------------------------------------------------------------------------------
		
		//	returns the index of the smallest distance in an array of indices
		public static int GetShortestDistanceID(float[] distances)
		{
			int id = 0;
			float mag = float.MaxValue;
			for(int i = 0; i < distances.Length; i++)
			{
				if(distances[i] < mag)
				{
					id = i;
					mag = distances[i];
				}
			}
			return id;
		}
		
		public static int GetShortestDistanceID(float[] distances, int defaultID)
		{
			int id = defaultID;
			float mag = float.MaxValue;
			for(int i = 0; i < distances.Length; i++)
			{
				if(distances[i] < mag)
				{
					id = i;
					mag = distances[i];
				}
			}
			return id;
		}
		
		public static int GetShortestDistanceID(float[] distances, float minDist, int defaultID = 0)
		{
			int id = defaultID;
			float mag = float.MaxValue;
			for(int i = 0; i < distances.Length; i++)
			{
				if(distances[i] <= minDist && distances[i] < mag)
				{
					id = i;
					mag = distances[i];
				}
			}
			return id;
		}
		
		public static int GetShortestDistanceID(float[] distances, out float shortestDist, float minimumDist = float.MaxValue, int defaultID = 0)
		{
			int id = defaultID;
			float mag = float.MaxValue;
			for(int i = 0; i < distances.Length; i++)
			{
				if(distances[i] <= minimumDist && distances[i] < mag)
				{
					id = i;
					mag = distances[i];
				}
			}
			shortestDist = mag;
			return id;
		}
		
		//-- Time ---------------------------------------------------------------------------------------
		
		//	DateTime formatting
		
		public static string FormatDateTime_German( DateTime time )
		{
			return time.Day.ToString() + "/" + time.Month.ToString() + "/" + time.Year.ToString();
		}
		
		public static string FormatDateTimeExact_German( DateTime time )
		{
			return FormatDateTime_German(time) + " " + time.Hour.ToString() + ":" + time.Minute.ToString();
		}

		public static string FormatMinutesFromS( float seconds, string separator=":", bool showLeadZero=true, int round = 0)
		{	
			int s = Mathf.RoundToInt(seconds);
			int ss = s % 60;
			int minutes = (s-ss) / 60;
			if(round < 0) { ss = 0; }
			else if(round > 0) { ss = 0; minutes++; }
			string _m = (minutes < 10 && (showLeadZero || minutes == 0) ? "0" : "") + minutes.ToString();
			string _s = (ss < 10 ? "0" : "") + ss.ToString();
			return _m + separator + _s;
		}

		public static string FormatMinutesFromMs( float milliseconds, string append="", bool showMillis=true, bool showLeadZero=true, string separator=":")
		{
			int ms = Mathf.RoundToInt(milliseconds) % 1000;
			int s = Mathf.RoundToInt((milliseconds-ms)/1000f);
			ms = Mathf.RoundToInt(ms/10);
			int rs = s % 60;
			int minutes = (s-rs) / 60;
			string _m = (minutes < 10 && (showLeadZero || minutes == 0) ? "0" : "") + minutes.ToString();
			string _s = (rs < 10 ? "0" : "") + rs.ToString();
			string _ms = showMillis ? (separator + (ms < 10 ? "0" : "") + ms.ToString() ) : "";
			return _m + separator + _s + _ms + append;
		}


		public static string FormatSecondsFromMs(float millis, string append="", bool showMillis=true, bool showLeadZero=true, string separator=":")
		{
			int ms = Mathf.RoundToInt(millis) % 1000;
			int s = Mathf.RoundToInt((millis-ms)/1000f);
			ms = Mathf.RoundToInt(ms/10);
			string _s = (s < 10 && (showLeadZero || s == 0) ? "0" : "") + s.ToString();
			string _ms = showMillis ? (separator + (ms < 10 ? "0" : "") + ms.ToString()) : "";
			return _s + _ms + append;
		}
		

		public static string FormatMilliseconds( float millis, TimeFormat format, string append="", bool showLeadZero=true, string separator=":")
		{
			switch(format)
			{
				case TimeFormat.Seconds:	return FormatSecondsFromMs(millis, append, true, showLeadZero, separator);
				case TimeFormat.Minutes: 	return FormatMinutesFromMs(millis, append, true, showLeadZero, separator);
				default:					return Mathf.CeilToInt(millis).ToString() + append;
			}
		}
		

		//-- String formatting --------------------------------------------------------------------------

		//	returns a string of a floating point number with n floating digits
		public static string Float2String(float f, int digits=2)
		{
			if(digits == 0)
			{
				return Mathf.RoundToInt(f).ToString();
			}
			int d = 10; for(int i = 1; i < digits; i++) d *= 10;
			return (Mathf.RoundToInt(f*d) / (float)d).ToString();
		}
		//	returns a string of a floating point number with n floating digits
		public static string Float2String(double d, int digits=2)
		{
			return Float2String ((float)d, digits);
		}

		public static string FormatTypename(System.Type type)
		{
			string s = type.ToString ();
			string[] split = s.Split ('.');
			return split.Length > 0 ? split [split.Length - 1] : s;
		}

		public static string getTabbing(int tabbing)
		{
			if (tabbing > 0) {
				var b = new System.Text.StringBuilder ();
				for(int i = 0; i < tabbing; i++) {
					b.Append ("\t");
				}
				return b.ToString();
			}
			else {
				return "";
			}
		}
			
		public static string FormatHash(object o)
		{
			if(o == null) 
			{
				return "NULL";
			}
			else
			{
				string s = o.GetHashCode ().ToString();
				if(s.Length <= 4)
				{
					return s;
				}
				else
				{
					int num = Mathf.Min(Mathf.FloorToInt(s.Length / 2f ), 3);
					return s.Substring (0, num) + ".." + s.Substring (s.Length-num-1, num);
				}
			}
		}
		
		//-- Other --------------------------------------------------------------------------------------

		//	returns current framerate for display
		public static string GetRoundedFrameRate(int floatingDigits)
		{
			return Mathf.RoundToInt(1.0f/Time.smoothDeltaTime).ToString();
		}

		//	breaks up a given number into its individual digits for display
		public static List<int> GetNumberDigits(int num)
		{
			List<int> listOfInts = new List<int>();
			while(num > 0)
			{
				listOfInts.Add(num % 10);
				num = num / 10;
			}
			listOfInts.Reverse();
			return listOfInts;
		}
		
	}
	
	#endregion
	
}

#region richtext utility

public static class RichText
{
	public static Color _warning;
	public static Color _error;
	public static Color _process;
	public static Color _success;

	public static Color _darkGreen;
	public static Color _darkRed;
	public static Color _orange;
	public static Color _darkOrange;
	public static Color _darkYellow;
	public static Color _darkCyan;

	public static Color _darkMagenta;

	public static Color tag1Color;

	static RichText()
	{
		_warning 		= _hexToColor("bbb000");
		_error			= _hexToColor("8e0000");
		_process		= _hexToColor("4145A6FF");
		_success		= _hexToColor("04a200");
		_darkGreen		= Color.Lerp( Color.green, Color.black, 0.65f );
		_darkRed		= Color.Lerp( Color.red, Color.black, 0.3f );
		_orange			= Color.Lerp( Color.red, Color.yellow, 0.4f );
		_darkOrange 	= Color.Lerp(Color.red, Color.yellow, 0.22f);
		_darkYellow 	= Color.Lerp(Color.yellow, Color.black, 0.6f);
		_darkMagenta 	= Color.Lerp(Color.magenta, Color.blue, 0.4f);
		_darkCyan		= Color.Lerp(Color.cyan, Color.blue, 0.6f);
		 
		tag1Color 	= Color.Lerp (Color.red, Color.yellow, 0.375f);
	}

	//	convert Color/Color32 to hex representation
	public static string _colorToHex(Color32 color)
	{
		string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
		return hex;
	}
	
	//	convert hex to Color
	public static Color _hexToColor(string hex)
	{
		byte r = byte.Parse(hex.Substring(0,2), System.Globalization.NumberStyles.HexNumber);
		byte g = byte.Parse(hex.Substring(2,2), System.Globalization.NumberStyles.HexNumber);
		byte b = byte.Parse(hex.Substring(4,2), System.Globalization.NumberStyles.HexNumber);
		return new Color32(r,g,b, 255);
	}
	
	public static string color( string text, Color32 c )
	{
		return "<color=#" + _colorToHex(c) + ">" + text + "</color>";
	}
	public static string color( string text, Color32 c1, Color32 c2, float w=0.5f )
	{
		return color(text, Color.Lerp(c1, c2, w));
	}

	public static string green(string text) { return color(text, Color.green); }
	public static string red(string text) { return color(text, Color.red); }
	public static string yellow(string text) { return color(text, Color.yellow); }
	public static string blue(string text) { return color(text, Color.blue); }
	public static string cyan(string text) { return color(text, Color.cyan); }
	public static string magenta(string text) { return color(text, Color.magenta); }

	public static string darkGreen(string text) { return color(text, _darkGreen); }
	public static string orange(string text) { return color(text, _orange); }
	public static string darkOrange(string text) { return color(text, _darkOrange); }
	public static string darkRed(string text) { return color(text, _darkRed); }
	public static string darkYellow(string text) { return color(text, _darkYellow); }
	public static string darkMagenta(string text) { return color(text, _darkMagenta); }
	public static string darkCyan(string text) { return color(text, _darkCyan); }
	
	public static string italic( string text )
	{
		return "<i>" + text + "</i>";
	}
	public static string emph( string text )
	{
		return "<b>" + text + "</b>";
	}
	public static string emph( object o )
	{
		if(!ReferenceEquals(o, null)) return emph(o.ToString());
		else return emph("null");
	}
	public static string size( string text, int size )
	{
		return "<size=" + size.ToString() + ">" + text + "</size>";
	}


	/*public static string FormatTag1Message(string context, string sourceScript, string msg)
	{
		return RichText.color (context, tag1Color) + RichText.emph (sourceScript + ":: ") + msg;
	}
	public static string FormatWarningMessage(string sourceScript, string msg)
	{
		return RichText.color("Warning: ", _warning) + RichText.emph(sourceScript + ":: ") + msg;
	}
	public static string FormatErrorMessage(string sourceScript, string msg)
	{
		return RichText.emph(RichText.color("Error: ", _error) + sourceScript + ":: ") + msg;
	}
	public static string FormatProcessMessage(string sourceScript, string msg)
	{
		return RichText.color("Process: ", _process) + RichText.emph(sourceScript + ":: ") + msg;
	}
	public static string FormatSuccessMessage(string sourceScript, string msg)
	{
		return RichText.color("Success: ", _success) + RichText.emph(sourceScript + ":: ") + msg;
	}*/
}

#endregion

//=================================================================================================================

#region flag utility

public static class EnumHelper
{
	public static bool EnumFlagTest(this Enum keys, Enum flag)
	{
		ulong keysVal = Convert.ToUInt64(keys);
		ulong flagVal = Convert.ToUInt64(flag);
		
		return (keysVal & flagVal) == flagVal;
	}
	
	//	//	returns true if given enums share a flag
	//	public static bool Compare<T>(T a, T b)
	//		where T : struct
	//	{
	//		foreach(T e1 in IterateFlags(a))
	//			if(IsFlagPresent<T>(e1, b))
	//				return true;
	//		return false;
	//	}
	//	
	//	//	returns true if given enum input has given flag
	//	public static bool IsFlagPresent<T>(T input, T lookingForFlag)
	//		where T : struct
	//	{
	//		int intVal = (int) (object) input;
	//		int intLookingFor = (int) (object) lookingForFlag;
	//		return intVal == intLookingFor && intLookingFor != 0;
	//	}
	//	
	//	//	returns each flag entry of an enum separately
	//	public static IEnumerable IterateFlags<T>(T input)
	//		where T : struct
	//	{
	//	    foreach (T e in Enum.GetValues(input.GetType()))
	//		{
	//			if(IsFlagPresent<T>(input, e))
	//				yield return e;
	//		}
	//	}
	//	
	//	public static List<T> GetFlags<T>(T input)
	//		where T : struct
	//	{
	//		List<T> result = new List<T>();
	//		foreach(T t in IterateFlags<T>(input))
	//			result.Add(t);
	//		return result;
	//	}
}

#endregion


//=================================================================================================================

public static class StringUtil
{
    public static string StringWithUniqueInt(IEnumerable<string> currentStrings, string startString)
    {
        var query = currentStrings.Where((str)=>str.Length > startString.Length)
              .Select((str) => new { start = str.Substring(0, startString.Length), end = str.Substring(startString.Length) })
              .Where((str) => {
                  int i;
                  return str.start.Equals(startString, StringComparison.CurrentCultureIgnoreCase) && int.TryParse(str.end, out i);
              })
              .Select((name) => int.Parse(name.end));
        if(query.Count() > 0)
        {
            return startString + (query.Max() + 1);
        }
        else
        {
            return startString + "1";
        }
    }
}