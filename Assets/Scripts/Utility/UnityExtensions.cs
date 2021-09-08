using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using UnityEngine.UI;

//using System.ComponentModel;

//=================================================================================================================

public static class UnityExtensionMethods
{
	
	#region vector extensions
	
	public static float SqrDist(this Vector2 a, Vector2 b) 			{ return (a-b).sqrMagnitude; }
	public static float SqrDist(this Vector3 a, Vector3 b) 			{ return (a-b).sqrMagnitude; }
	public static float Distance2f(this Vector3 p, Vector3 p2) 		{ return Vector2.Distance( new Vector2(p.x, p.z), new Vector2(p2.x, p2.z) ); }
	public static float SqrDistance2f(this Vector3 p, Vector3 p2)	{ return (new Vector2(p.x,p.z)-new Vector2(p2.x,p2.z)).sqrMagnitude; }

	/// <summary>
	/// Calculate a perpendicular vector that is left of this vector (in counter-clockwise direction)
	/// </summary>
	public static Vector2 GetPerpendicular(this Vector2 vec)
	{
		return new Vector2(-vec.y, vec.x);
	}
	public static Vector2 toXZ(this Vector3 vec)
	{
		return new Vector2(vec.x, vec.z);
	}
	public static Vector3 toXZDir(this Vector3 vec)
	{
		return Vector3.Normalize(new Vector3(vec.x, 0, vec.z));
	}

	public static Vector3 toXYZ(this Vector2 vec, float y = 0)
	{
		return new Vector3(vec.x, y, vec.y);
	}

	/// <summary>
	/// rotates a directional vector around 0,0 in <angle> °
	/// </summary>
	public static Vector2 Rotate(this Vector2 v, float angle) {

		float sin = Mathf.Sin (angle * Mathf.Deg2Rad);
		float cos = Mathf.Cos (angle * Mathf.Deg2Rad);

		float x = v.x * cos - v.y * sin;
		float y = v.x * sin + v.y * cos;

		return new Vector2 (x, y);
	}

	#endregion

	//=================================================================================================================

	#region rect extensions

	public static Vector2[] getCorners(this Rect r)
	{
		Vector2[] corners = new Vector2[4];
		corners[0] = new Vector2(r.x, r.y);
		corners[1] = new Vector2(r.x+r.width, r.y);
		corners[2] = new Vector2(r.x+r.width, r.y+r.height);
		corners[3] = new Vector2(r.x, r.y+r.height);
		return corners;
	}

	public static Vector3[] getCorners3D(this Rect r, float y)
	{
		Vector3[] corners = new Vector3[4];
		corners[0] = new Vector3(r.x, y, r.y);
		corners[1] = new Vector3(r.x+r.width, y, r.y);
		corners[2] = new Vector3(r.x+r.width, y, r.y+r.height);
		corners[3] = new Vector3(r.x, y, r.y+r.height);
		return corners;
	}

	public static Rect Enlarge(this Rect r, float s)
	{
		return new Rect (r.x - s, r.y - s, r.width + s * 2, r.height + s * 2);
	}
	public static Rect Enlarge(this Rect r, Rect add)
	{
		return new Rect(
			Mathf.Min(r.xMin, add.xMin),
			Mathf.Min(r.yMin, add.yMin),
			Mathf.Max(r.xMax, add.xMax),
			Mathf.Max(r.yMax, add.yMax)
		);
	}
	public static Rect Enlarge(this Rect rect, float width, float height)
	{
		return new Rect(rect.x, rect.y, rect.width+width, rect.height+height);
	}
	public static Rect Offset(this Rect rect, float x, float y)
	{
		return new Rect(rect.x+x, rect.y+y, rect.width, rect.height);
	}
	public static Rect Constraint(this Rect r, Rect constraint, bool applyX=true, bool applyY=true)
	{
		Rect result = new Rect(r);
		if(applyX)
		{
			result.x = Mathf.Max(r.xMin, constraint.xMin);
			result.xMax = Mathf.Min(r.xMax, constraint.xMax);
		}
		if(applyY)
		{
			result.y = Mathf.Max(r.yMin, constraint.yMin);
			result.yMax = Mathf.Min(r.yMax, constraint.yMax);
		}
		return result;
	}

	
	public static bool Intersects( this Rect a, Rect b ) {
		FlipNegative( ref a );
		FlipNegative( ref b );
		bool c1 = a.xMin < b.xMax;
		bool c2 = a.xMax > b.xMin;
		bool c3 = a.yMin < b.yMax;
		bool c4 = a.yMax > b.yMin;
		return c1 &&  c2 && c3 && c4;
	}
 
	private static void FlipNegative(ref Rect r) {
		if( r.width < 0 )
			r.x -= ( r.width *= -1 );
		if( r.height < 0 )
			r.y -= ( r.height *= -1 );
	}

	#endregion

	//=================================================================================================================

	#region gameobject extensions


	/// <summary>
	/// Performs a full recursive search for specified component and returns the first match.
	/// </summary>
	/// <returns>First TComponent found or null.</returns>
	/// <param name="parent">GameObject to start from (also gets checked for TComponent).</param>
	public static TComponent GetComponentInHierarchy<TComponent>( this GameObject parent, int depth=int.MaxValue ) where TComponent : Component
	{
		if(parent == null)
		{
			return null;
		}
		return parent.transform.GetComponentInHierarchy<TComponent>(depth);
	}

	/// <summary>
	/// Performs a full recursive search for specified component and returns all occurances.
	/// </summary>
	/// <returns>A list of all found components.</returns>
	/// <param name="parent">Transform to start from (also gets checked for TComponent).</param>
	/// <typeparam name="TComponent">The 1st type parameter.</typeparam>
	public static List<TComponent> GetComponentsInHierarchy<TComponent>( this GameObject parent, int depth=int.MaxValue ) where TComponent : Component
	{
		if(parent == null)
		{
			return null;
		}
		return parent.transform.GetComponentsInHierarchy<TComponent>(depth);
	}

	/// <summary>
	/// Performs a full recursive search for specified type and returns the first occurance.
	/// You may search interfaces with this method, but the typecasting is expensive
	/// </summary>
	/// <returns>A list of all found components.</returns>
	/// <param name="parent">Transform to start from (also gets checked for TComponent).</param>
	/// <typeparam name="T">Type to search for.</typeparam>
	public static T GetInterfaceInHierarchy<T>( this GameObject parent, int depth = int.MaxValue ) where T : class
	{
		if(parent == null) return null;
		return parent.transform.GetInterfaceInHierarchy<T>(depth);
	}
	/// <summary>
	/// Performs a full recursive search for specified type and returns all occurances.
	/// You may search interfaces with this method, but the typecasting is expensive
	/// </summary>
	/// <returns>A list of all found components.</returns>
	/// <param name="parent">Transform to start from (also gets checked for TComponent).</param>
	/// <typeparam name="T">Type to search for.</typeparam>
	public static List<T> GetInterfacesInHierarchy<T>( this GameObject parent, int depth=int.MaxValue ) where T : class
	{
		if(parent == null) return null;
		return parent.transform.GetInterfacesInHierarchy<T>(depth);
	}

	public static TInterface GetInterface<TInterface>(this GameObject go) where TInterface : class
	{
		return go.transform.GetInterface<TInterface> ();
	}

	public static IEnumerable<TInterface> GetInterfaces<TInterface>(this GameObject go) where TInterface : class
	{
		return go.transform.GetInterfaces<TInterface> ();
	}

	/// <summary>
	/// Perform an action on each type found within a full recursive search of parent and its child hierarchy
	/// </summary>
	public static void SendMessagesToComponents<T>( this GameObject parent, System.Action<T> action, int depth=int.MaxValue ) where T : class
	{
		if(parent == null || action == null) return;
		foreach(T type in parent.transform.GetInterfacesInHierarchy<T>(depth))
		{
			if(type != null)
				action(type);
		}
	}

	#endregion

	//=================================================================================================================


	#region Camera

	/// <summary>
	/// Get all image effects using reflection.
	/// </summary>
	static public MonoBehaviour[] GetImageEffects(this Camera cam)
	{
		MonoBehaviour[] allComponents = cam.GetComponents<MonoBehaviour>();
		return allComponents.Where(c => c.GetType()
			.GetMethod("OnRenderImage",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				new Type[] { typeof(RenderTexture), typeof(RenderTexture) },
				null) != null).ToArray();
	}
	static public IEnumerable<MonoBehaviour> GetImageEffectsQuery(this Camera cam)
	{
		MonoBehaviour[] allComponents = cam.GetComponents<MonoBehaviour>();
		return allComponents.Where(c => c.GetType()
			.GetMethod("OnRenderImage",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				new Type[] { typeof(RenderTexture), typeof(RenderTexture) },
				null) != null);
	}

	///	<summary>
	///	returns wether position (with optional bounds) is within camera view frustrum
	///	</summary>
	///
	public static bool IsPositionVisible(this Camera camera, Vector3 pos, Vector3 boundSize) 
	{
		var bounds = new Bounds(pos, boundSize);
		var planes = GeometryUtility.CalculateFrustumPlanes(camera);
		return GeometryUtility.TestPlanesAABB(planes, bounds);
	} 


	#endregion
	
	//=================================================================================================================

	#region transform extensions

	public static bool hasParent( this Transform child, Transform parent, int depth=999 )
	{
		if(parent != null && child != null)
		{
			if(child == parent)
			{
				return true;
			}
			else if( depth > 0 )
			{
				return hasParent(child.parent, parent, depth - 1);
			}
		}
		return false;
	}

	/// <summary>
	/// Performs a full recursive search for given name and returns the first match. 
	/// Uppercase is ignored.
	/// </summary>
	public static Transform FindChildInHierarchy( this Transform parent, string search )
	{
		search = search.ToLower();
		if( parent.name.ToLower().Equals(search) ) return parent;

		foreach( Transform child in parent )
		{
			Transform result = FindChildInHierarchy(child, search);
			if(result != null) return result;
		}

		return null;
	}
	
	/// <summary>
	/// Performs a full recursive search for specified component and returns the first match.
	/// </summary>
	/// <returns>First TComponent found or null.</returns>
	/// <param name="parent">Transform to start from (also gets checked for TComponent).</param>
	public static TComponent GetComponentInHierarchy<TComponent>( this Transform parent, int depth=int.MaxValue ) where TComponent : Component
	{
		if(parent == null || !parent.gameObject.activeSelf) return null;
		TComponent c = parent.GetComponent<TComponent>();
		if(c != null) return c;

		if (depth > 0) {
			foreach (Transform child in parent) {
				c = GetComponentInHierarchy<TComponent> (child, depth - 1);
				if (c != null)
					return c;
			}
		}
		return null;
	}

	/// <summary>
	/// Performs a full recursive search for specified component || interface and returns the first match.
	/// </summary>
	/// <returns>First TComponent found or null.</returns>
	/// <param name="parent">Transform to start from (also gets checked for TComponent).</param>
	public static T GetInterfaceInHierarchy<T>( this Transform parent, int depth=int.MaxValue ) where T : class
	{
		if(parent == null || !parent.gameObject.activeSelf) return null;
		foreach(var component in parent.GetComponents<Component>())
		{
			var cc = component as T;
			if (cc != null)
				return cc;
		}
		if(depth > 0)
		{
			foreach(Transform child in parent)
			{
				if (child.gameObject.activeSelf) {
					var c = GetInterfaceInHierarchy<T> (child, depth - 1);
					if (c != null)
						return c;
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Performs a full recursive search for specified component and returns all occurances.
	/// </summary>
	/// <returns>A list of all found components.</returns>
	/// <param name="parent">Transform to start from (also gets checked for TComponent).</param>
	/// <typeparam name="TComponent">Component type to search for.</typeparam>
	public static List<TComponent> GetComponentsInHierarchy<TComponent>( this Transform parent, int depth=int.MaxValue ) where TComponent : Component
	{
		if(parent == null || !parent.gameObject.activeSelf) return new List<TComponent>();
		List<TComponent> result = new List<TComponent>(4);
		_recursiveComponentSearch<TComponent>(parent, result, depth);
		return result;
	}

	/// <summary>
	/// Performs a full recursive search for specified type and returns all occurances.
	/// You may search interfaces with with method, but the typecasting is expensive
	/// </summary>
	/// <returns>A list of all found components.</returns>
	/// <param name="parent">Transform to start from (also gets checked for TComponent).</param>
	/// <typeparam name="T">Type to search for.</typeparam>
	public static List<T> GetInterfacesInHierarchy<T>( this Transform parent, int depth=int.MaxValue ) where T : class
	{
		if(parent == null || !parent.gameObject.activeSelf) return new List<T>();
		List<T> result = new List<T>(4);
		_recursiveTypeSearch<T>(parent, result, depth);
		return result;
	}

	private static void _recursiveComponentSearch<TComponent>(Transform curr, List<TComponent> list, int depth) where TComponent : Component
	{
		list.AddRange (curr.GetComponents<TComponent>());
		if(depth > 0)
		{
			for(int i = 0; i < curr.childCount; i++)
			{
				if (curr.GetChild (i).gameObject.activeSelf)
					_recursiveComponentSearch<TComponent> (curr.GetChild (i), list, depth - 1);
			}
//			foreach(Transform child in curr)
//			{
//				if(child.gameObject.activeSelf)
//					_recursiveComponentSearch<TComponent>(child, list, depth-1);
//			}
		}
	}

	private static void _recursiveTypeSearch<T>(Transform curr, List<T> list, int depth) where T : class
	{
		Component[] components = curr.GetComponents<Component>();
		foreach(Component c in components)
		{
			if(c is T)
				list.Add(c as T);
		}
		if(depth > 0)
		{
			foreach(Transform child in curr)
			{
				if(child.gameObject.activeSelf)
					_recursiveTypeSearch<T>(child, list, depth-1);
			}
		}
	}

	public static TInterface GetInterface<TInterface>(this Transform t) where TInterface : class
	{
		if (!typeof(TInterface).IsInterface)
			return null;
		else
			return t.GetComponents<Component> ().OfType<TInterface> ().FirstOrDefault ();
	}

	public static IEnumerable<TInterface> GetInterfaces<TInterface>(this Transform t) where TInterface : class
	{
		if(typeof(TInterface).IsInterface)
		{
			foreach (TInterface i in t.GetComponents<Component>().OfType<TInterface>())
				yield return i;
		}
	}

	/// <summary>
	/// Resets the transform t zero pos, rotation and scale 1,1,1
	/// </summary>
	public static void ResetTransform(this Transform trans)
	{
		trans.position = Vector3.zero;
		trans.localRotation = Quaternion.identity;
		trans.localScale = Vector3.one;
	}



	#endregion

	//=================================================================================================================

	

	#region matrix extensions

	public static Quaternion ExtractRotation(this Matrix4x4 matrix)
    {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;
 
        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;
 
        return Quaternion.LookRotation(forward, upwards);
    }
 
    public static Vector3 ExtractPosition(this Matrix4x4 matrix)
    {
        Vector3 position;
        position.x = matrix.m03;
        position.y = matrix.m13;
        position.z = matrix.m23;
        return position;
    }
 
    public static Vector3 ExtractScale(this Matrix4x4 matrix)
    {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }

	#endregion

	//=================================================================================================================

	#region rectTransform extensions

	//	rect transform helper methods
	public static void SetDefaultScale(this RectTransform trans) {
		trans.localScale = new Vector3(1, 1, 1);
	}
	public static void SetPivotAndAnchors(this RectTransform trans, Vector2 aVec) {
		trans.pivot = aVec;
		trans.anchorMin = aVec;
		trans.anchorMax = aVec;
	}
	
	public static Vector2 GetSize(this RectTransform trans) {
		return trans.rect.size;
	}
	public static float GetWidth(this RectTransform trans) {
		return trans.rect.width;
	}
	public static float GetHeight(this RectTransform trans) {
		return trans.rect.height;
	}
	
	public static void SetPositionOfPivot(this RectTransform trans, Vector2 newPos) {
		trans.localPosition = new Vector3(newPos.x, newPos.y, trans.localPosition.z);
	}
	
	public static void SetLeftBottomPosition(this RectTransform trans, Vector2 newPos) {
		trans.localPosition = new Vector3(newPos.x + (trans.pivot.x * trans.rect.width), newPos.y + (trans.pivot.y * trans.rect.height), trans.localPosition.z);
	}
	public static void SetLeftTopPosition(this RectTransform trans, Vector2 newPos) {
		trans.localPosition = new Vector3(newPos.x + (trans.pivot.x * trans.rect.width), newPos.y - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
	}
	public static void SetRightBottomPosition(this RectTransform trans, Vector2 newPos) {
		trans.localPosition = new Vector3(newPos.x - ((1f - trans.pivot.x) * trans.rect.width), newPos.y + (trans.pivot.y * trans.rect.height), trans.localPosition.z);
	}
	public static void SetRightTopPosition(this RectTransform trans, Vector2 newPos) {
		trans.localPosition = new Vector3(newPos.x - ((1f - trans.pivot.x) * trans.rect.width), newPos.y - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
	}
	
	public static void SetSize(this RectTransform trans, Vector2 newSize) {
		Vector2 oldSize = trans.rect.size;
		Vector2 deltaSize = newSize - oldSize;
		trans.offsetMin = trans.offsetMin - new Vector2(deltaSize.x * trans.pivot.x, deltaSize.y * trans.pivot.y);
		trans.offsetMax = trans.offsetMax + new Vector2(deltaSize.x * (1f - trans.pivot.x), deltaSize.y * (1f - trans.pivot.y));
	}
	public static void SetWidth(this RectTransform trans, float newSize) {
		SetSize(trans, new Vector2(newSize, trans.rect.size.y));
	}
	public static void SetHeight(this RectTransform trans, float newSize) {
		SetSize(trans, new Vector2(trans.rect.size.x, newSize));
	}


	/// <summary>
	/// set this transform's position and size to target transform as child.
	/// </summary>
	public static void MatchTransformTo(this RectTransform t, RectTransform target, bool reparent=true)
	{
		var prev = t.parent;
		t.SetParent (target, false);
		t.anchorMin = Vector2.zero;
		t.anchorMax = Vector2.one;
		t.SetSize (target.GetSize ());
		t.localPosition = Vector3.zero;

		if(!reparent)
		{
			t.SetParent(prev, true);
		}
	}
	public static void MatchTransformNoAnchors(this RectTransform t, RectTransform target, bool reparent=true)
	{
		var prev = t.parent;
		t.SetParent (target, false);
		t.SetSize (target.GetSize ());
		t.localPosition = Vector3.zero;

		if(!reparent)
		{
			t.SetParent(prev, true);
		}
	}

	///	@brief
	///	Performs a single raycast against any Worldspace RectTransform.
	///
	///	@details
	///	Uses the same code as Unity's GraphicRaycaster component see <a href="https://bitbucket.org/Unity-Technologies/ui/src/4f3cf8d16c1d8c6e681541a292855792e50b392e/UnityEngine.UI/UI/Core/GraphicRaycaster.cs?at=5.2&fileviewer=file-view-default">here</a> 
	///
	///	@param cam The camera to raycast from.
	///	@param screenP The screen position to raycast from.
	///	@param hit The resulting hit world position.
	///
	///	@returns The hit distance or -1.
	///
	public static float Raycast3D(this RectTransform r, Camera cam, Vector2 screenP, out Vector3 hit)
	{
		if(RectTransformUtility.RectangleContainsScreenPoint(r, screenP, cam))
		{
			var ray = cam.ScreenPointToRay(screenP);

			//	get plane hit distance
			var hitDist = (Vector3.Dot(r.forward, r.position - ray.origin) / Vector3.Dot(r.forward, ray.direction));
			hit = ray.origin + ray.direction * hitDist;
			return hitDist;
		}
		else
		{
			hit = Vector3.zero;
			return -1f;
		}
	}
	
	///	@brief
	///	Get a 3D anchored position of this transform's rectangle.
	///
	///	@param anchor The desired anchor position.
	///	@param canvas The canvas of this RectTransform. If null is given, GetMyCanvas() will be called internally.
	///	@param factor Offset the anchor position by a factor.
	///
	public static Vector3 GetAnchorPos(this RectTransform r, Anchor2D anchor, Canvas canvas=null, float factor=1f)
	{
		if(canvas == null)
		{
			canvas = r.GetMyCanvas();
		}
		var pos = r.position;
		var result = Vector3.zero;
		
		var w = r.rotation * (Vector3.right * r.GetWidth() / canvas.referencePixelsPerUnit) * factor;
		var h = r.rotation * (Vector3.up * r.GetHeight() / canvas.referencePixelsPerUnit) * factor;

		//Debug.Log("ref pixels:: " + canvas.referencePixelsPerUnit + " " + w + " " + h + " anchor=[" + anchor + "]");

		switch(anchor)
		{
			case Anchor2D.UpperLeft:	result = pos - h - w; break;
			case Anchor2D.UpperCenter:	result = pos - h; break;
			case Anchor2D.UpperRight:	result = pos - h + w; break;
			case Anchor2D.CenterLeft:	result = pos - w; break;
			case Anchor2D.Center:		result = pos; break;
			case Anchor2D.CenterRight:	result = pos + w; break;
			case Anchor2D.LowerLeft:	result = pos + h - w; break;
			case Anchor2D.LowerCenter:	result = pos + h; break;
			case Anchor2D.LowerRight:	result = pos + h + w; break;
		}
		return result;
	}

	/// @brief
	///	Sets anchor positions to rect corners.
	///
	///	@details
	/// set this transform's anchor to its boundary corners, 
	/// making it so the element will stay in place why scaling exactly.
	/// Only works when this transform is parented
	///
	public static void SetAnchorsToOwnCorners(this RectTransform r)
	{
		if(r != null && r.parent is RectTransform)
		{
			var p = r.parent as RectTransform;

			var offsetMin = r.offsetMin;
			var offsetMax = r.offsetMax;
			var _anchorMin = r.anchorMin;
			var _anchorMax = r.anchorMax;

			var parent_width = p.rect.width;
			var parent_height = p.rect.width;

			var anchorMin = new Vector2 (_anchorMin.x + (offsetMin.x / parent_width),
										 _anchorMin.y + (offsetMin.y / parent_height));
			var anchorMax = new Vector2 (_anchorMax.x + (offsetMax.x / parent_width),
										 _anchorMax.y + (offsetMax.y / parent_height));

			r.anchorMin = anchorMin;
			r.anchorMax = anchorMax;

			r.offsetMin = new Vector2 (0, 0);
			r.offsetMax = new Vector2 (1, 1);
			r.pivot = new Vector2 (0.5f, 0.5f);
		}
	}

	public static Canvas GetMyCanvas(this RectTransform transform)
	{
		Transform parent = transform;
		Canvas canvas = null;
		while (parent != null)
		{
			canvas = parent.GetComponent<Canvas>();
			if (canvas != null)
				return canvas;
			
			parent = parent.parent;
		}
		return null;
	}

	#if UNITY_EDITOR

	[UnityEditor.MenuItem("DDL Custom/UI/Set anchors to self boundary")]
	static void SetAnchorsToOwnCorners()
	{
		for(int i = 0; i < UnityEditor.Selection.gameObjects.Length; i++)
		{
			if (UnityEditor.Selection.gameObjects [i] != null) {
				var r = UnityEditor.Selection.gameObjects [i].GetComponent<RectTransform> ();
				if (r != null)
					r.SetAnchorsToOwnCorners ();
			}
		}
	}



	#endif



	#endregion
	
	//=================================================================================================================
	
	#region UI.Image extensions

	public static void SetAlpha(this Image img, float a)
	{
		Color c = img.color;
		c.a = Mathf.Clamp01(a);
		img.color = c;
	}
	
	public static void SetAlpha(this Text txt, float a)
	{
		Color c = txt.color;
		c.a = Mathf.Clamp01(a);
		txt.color = c;
	}
	
/*	public static Canvas GetMyCanvas(this Image img)
	{
		return img.rectTransform.GetMyCanvas();
	}*/



	#endregion
	
	//=================================================================================================================

	#region Canvas
	/// <summary>
	/// Calulates Position for RectTransform.position from a transform.position. Does not Work with WorldSpace Canvas!
	/// </summary>
	/// <param name="_Canvas"> The Canvas parent of the RectTransform.</param>
	/// <param name="_Position">Position of in world space of the "Transform" you want the "RectTransform" to be.</param>
	/// <param name="_Cam">The Camera which is used. Note this is useful for split screen and both RenderModes of the Canvas.</param>
	/// <returns></returns>
	public static Vector3 CalculatePositionFromTransformToRectTransform(this Canvas _Canvas, Vector3 _Position, Camera _Cam)
	{
		Vector3 Return = Vector3.zero;
		if (_Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
		{   
			Return = _Cam.WorldToScreenPoint(_Position);
		}
		else if (_Canvas.renderMode == RenderMode.ScreenSpaceCamera)
		{
			Vector2 tempVector = Vector2.zero;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_Canvas.transform as RectTransform, _Cam.WorldToScreenPoint(_Position), _Cam, out tempVector);
			Return = _Canvas.transform.TransformPoint(tempVector);
		}
		
		return Return;
	}
	
	/// <summary>
	/// Calulates Position for RectTransform.position Mouse Position. Does not Work with WorldSpace Canvas!
	/// </summary>
	/// <param name="_Canvas">The Canvas parent of the RectTransform.</param>
	/// <param name="_Cam">The Camera which is used. Note this is useful for split screen and both RenderModes of the Canvas.</param>
	/// <returns></returns>
	public static Vector3 CalculatePositionFromMouseToRectTransform(this Canvas _Canvas, Camera _Cam)
	{
		Vector3 Return = Vector3.zero;
		
		if (_Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
		{
			Return = Input.mousePosition;
		}
		else if (_Canvas.renderMode == RenderMode.ScreenSpaceCamera)
		{
			Vector2 tempVector = Vector2.zero;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_Canvas.transform as RectTransform, Input.mousePosition, _Cam, out tempVector);
			Return = _Canvas.transform.TransformPoint(tempVector);
		}
		
		return Return;
	}
	
	/// <summary>
	/// Calculates Position for "Transform".position from a "RectTransform".position. Does not Work with WorldSpace Canvas!
	/// </summary>
	/// <param name="_Canvas">The Canvas parent of the RectTransform.</param>
	/// <param name="_Position">Position of the "RectTransform" UI element you want the "Transform" object to be placed to.</param>
	/// <param name="_Cam">The Camera which is used. Note this is useful for split screen and both RenderModes of the Canvas.</param>
	/// <returns></returns>
	public static Vector3 CalculatePositionFromRectTransformToTransform(this Canvas _Canvas, Vector3 _Position, Camera _Cam)
	{
		Vector3 Return = Vector3.zero;
		if (_Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
		{
			Return = _Cam.ScreenToWorldPoint(_Position);
		}
		else if (_Canvas.renderMode == RenderMode.ScreenSpaceCamera)
		{
			RectTransformUtility.ScreenPointToWorldPointInRectangle(_Canvas.transform as RectTransform, _Cam.WorldToScreenPoint(_Position), _Cam, out Return);
		}
		return Return;
	}
    #endregion

	//=================================================================================================================

	#region AnimationCurve

	public static AnimationCurve Clone(this AnimationCurve curve)
	{
		var clone = new AnimationCurve();
		var keys = new Keyframe[curve.keys.Length];
		for(int i = 0; i < keys.Length; i++)
		{
			keys[i] = curve.keys[i];
		}
		clone.keys = keys;
		return clone;
	}

	public static bool isValid(this AnimationCurve curve)
	{
		return curve.keys != null && curve.keys.Length >= 2;
	}

	#endregion


    #region Color
    public static string ToRichTextColor(this Color color)
    {
        Color32 colBytes = color;
        return "#" + colBytes.r.ToString("X2") + colBytes.g.ToString("X2") + colBytes.b.ToString("X2") + colBytes.a.ToString("X2");
    }
    #endregion

    #region String
    public static string UppercaseFirst(this string s)
    {
        // Check for empty string.
        if (string.IsNullOrEmpty(s))
            return string.Empty;

        // Return char and concat substring.
        return char.ToUpper(s[0]) + s.Substring(1);
    }

    public static string LowercaseFirst(this string s)
    {
        // Check for empty string.
        if (string.IsNullOrEmpty(s))
            return string.Empty;

        // Return char and concat substring.
        return char.ToLower(s[0]) + s.Substring(1);
    }

    #endregion

    //=================================================================================================================

    #region Dictionaries

    /// <summary>
    /// Merges the second dictionary into the first if the new key doesnt exist
    /// </summary>
    public static void Merge<TKey, TValue>(this IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
    {
        if (second == null || first == null) return;
        foreach (var item in second)
            if (!first.ContainsKey(item.Key))
                first.Add(item.Key, item.Value);
    }

    #endregion

    //=================================================================================================================

    #region enum extensions

    /// <summary>
    /// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object. The return value indicates whether the conversion succeeded.
    /// </summary>
    public static bool TryParse<T>(this Enum theEnum, string valueToParse, out T returnValue)
    {
        returnValue = default(T);
        if (Enum.IsDefined(typeof(T), valueToParse))
        {
            System.ComponentModel.TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(T));
            returnValue = (T)converter.ConvertFromString(valueToParse);
            return true;
        }
        return false;
    }

    #endregion
    
    //=================================================================================================================
}

//=================================================================================================================