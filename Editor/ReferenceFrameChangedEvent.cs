using UnityEditor;
using UnityEngine;
using System.Collections;

namespace TiercelFoundry.RVO2
{
	[CustomEditor(typeof(RVOManager))]
	public class ReferenceFrameChangedEvent : Editor
	{
        public void OnGUI()
        {
            RVOManager targ = target as RVOManager;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Reference Frame");
            CoordFrame newValue = (CoordFrame)EditorGUILayout.EnumPopup(targ.referenceFrame);
            if (newValue != targ.referenceFrame)
            {
                targ.referenceFrame = newValue;
                targ.OnReferenceFrameChanged();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}

