using UnityEngine;

/// <summary>
/// This script is only to rotate the stars corona around the Y axis to make it feel animated.
/// </summary>
public class RotateOnY : MonoBehaviour
{
    public float Speed;
	
	private void Update () 
	{
        transform.RotateAround(transform.position, transform.up, Speed * Time.deltaTime);
	}
}