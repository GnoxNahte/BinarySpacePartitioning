using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 
/// <br>Shows the property in inspector but greys it out and disable editing of it. </br>
/// <br>Can be used to indicate that the variable should not be edited in the inspector but it can be viewed for debugging.</br>
/// <br>Member variables need to be public OR have [SerializeField]</br>
/// <br> </br>
/// <br>How to use: Add [ReadOnly] to member variables, similar to [SerializeField] / [HideInInspector]</br>
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute
{

}