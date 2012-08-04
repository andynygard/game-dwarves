// ----------------------------------------------------------------------------
// <copyright file="TouchHandlerComponent.cs" company="Acidwashed Games">
//     Copyright 2012 Acidwashed Games. All right reserved.
// </copyright>
// ----------------------------------------------------------------------------

using UnityEngine;

/// <summary>
/// Component capturing touch events and delegating behaviour to game objects.
/// </summary>
public class TouchHandlerComponent : MonoBehaviour
{
    /// <summary>
    /// The max distance that an object can be in order to be touchable.
    /// </summary>
    public float TouchDistance;

    /// <summary>
    /// Initialises the component.
    /// </summary>
    public void Start()
    {
        this.TouchDistance = 100;
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    public void Update()
    {
        // Determine if the screen was touched/clicked
        Vector3 touchPosition;
        if (this.TryGetTouchPosition(out touchPosition))
        {
            TouchableComponent touched = this.GetTouchedComponent(touchPosition);
            if (touched != null)
            {
                touched.OnTouch();
            }
        }
    }

    /// <summary>
    /// Gets the current position of a touch or mouse click.
    /// </summary>
    /// <param name="touchPosition">The position.</param>
    /// <returns>True if a touch is currently being made.</returns>
    private bool TryGetTouchPosition(out Vector3 touchPosition)
    {
#if UNITY_IPHONE || UNITY_ANDRIOD
        if (Input.touchesCount == 1)
        {
            Touch touch = Input.touches[0];
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
            {
                touchPosition = touch.position;
                return true;
            }
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            touchPosition = Input.mousePosition;
            return true;
        }
#endif

        // No valid touch is currently being made
        touchPosition = Vector3.zero;
        return false;
    }

    /// <summary>
    /// Get the component that lies at the given screen position.
    /// </summary>
    /// <param name="touchPosition">The screen position of the touch.</param>
    /// <returns>The touchable component; Null if touchable object exists at the postion.</returns>
    private TouchableComponent GetTouchedComponent(Vector2 touchPosition)
    {
        TouchableComponent touchable = null;

        // Determine which object was hit
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
        if (Physics.Raycast(ray, out hit, this.TouchDistance))
        {
            // A physics object was hit. Get the TouchableComponent of this object (if any)
            touchable = hit.transform.GetComponent<TouchableComponent>();
        }

        return touchable;
    }
}