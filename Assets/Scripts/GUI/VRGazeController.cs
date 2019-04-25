using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class VRGazeController : MonoBehaviour {

    [SerializeField] private RadialReticle _reticle;               // The reticle, if applicable.
    [SerializeField] private bool _showDebugRay;                   // Optionally show the debug ray.
    [SerializeField] private float _debugRayLength = 5f;           // Debug ray length.
    [SerializeField] private float _debugRayDuration = 1f;         // How long the Debug ray will remain visible.
    [SerializeField] private float _RayLength = 500f;              // How far into the scene the ray is cast.

    //variables for EventSystem.RaycastAll
    PointerEventData m_pointerEventData;
    [SerializeField] private EventSystem _eventSystem;

    private RaycastResult _currentTarget;
    private VRTargetItem _target;
    private VRTargetItem _previousTarget;
	
	void Update ()
    {
        GazeRaycast();
	}

    private void GazeRaycast()
    {
        // Show the debug ray
        if (_showDebugRay)
        {
            Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * _debugRayLength, Color.blue, _debugRayDuration);
        }

        //Set up PointerEventData
        m_pointerEventData = new PointerEventData(_eventSystem);

        //handle positioning for gaze raycast depending on platform or using Unity editor
        #if UNITY_EDITOR
        m_pointerEventData.position = new Vector2(Screen.width / 2, Screen.height / 2);
        #elif UNITY_IOS || UNITY_ANDROID
        m_pointerEventData.position = new Vector2(UnityEngine.XR.XRSettings.eyeTextureWidth / 2, UnityEngine.XR.XRSettings.eyeTextureHeight / 2);
        #endif

        //Create a list of Raycast Results
        List<RaycastResult> results = new List<RaycastResult>();
        _eventSystem.RaycastAll(m_pointerEventData, results);

        //loop through all items hit
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject.GetComponent<VRTargetItem>())
            {
                _target = results[i].gameObject.GetComponent<VRTargetItem>();
                _currentTarget = results[i];

                // Something was hit, set position to hit position.
                if (_reticle)
                    _reticle.SetPosition(results[i]);
                break;
            }

            //no targets found
            _target = null;
        }

        // If current interactive item is not the same as the last interactive item, then call GazeEnter and start fill
        if (_target && _target != _previousTarget)
        {
            _reticle.ShowRadialImage(true);
            _target.GazeEnter(m_pointerEventData);
            if(_previousTarget)
                _previousTarget.GazeExit(m_pointerEventData);
            _reticle.StartProgress();
            _previousTarget = _target;
        }
        else if (_target && _target == _previousTarget)  //hovering over same item, advance fill progress
        {
            if (_reticle.ProgressRadialImage())        //returns true if selection is completed
                CompleteSelection();
        }
        else
        {
            //no target hit
            if(_previousTarget)
                _previousTarget.GazeExit(m_pointerEventData);

            _target = null;
            _previousTarget = null;
            _reticle.ShowRadialImage(false);
            _reticle.ResetProgress();
            _reticle.SetPosition();
        }
    }

    private void CompleteSelection ()
    {
        //hide radial image
        _reticle.ShowRadialImage(false);

        //radial progress completed, call completion events on target
        _target.GazeComplete(m_pointerEventData);
    }
}
