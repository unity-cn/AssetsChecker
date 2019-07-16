using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;

public class AssetsChecker : EditorWindow
{
 
	static int MinWidth=480;
 

	[MenuItem ("Window/Assets Checker")]
	static void Init ()
	{  
		AssetsChecker window = (AssetsChecker) EditorWindow.GetWindow (typeof (AssetsChecker));
 
		window.minSize=new Vector2(MinWidth,475);
	}

	void OnGUI ()
	{
		
	}

}
