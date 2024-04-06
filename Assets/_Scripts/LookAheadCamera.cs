using UnityEngine;

namespace Assets._Scripts
{
    public class LookAheadCamera : MonoBehaviour
    {
        //private Player _playerInstance;
        //private Transform _playerTransform;
        //private Vector3 _playerPosition;

        //private Transform _cameraTransform;
        //private Vector3 _targetCameraOffset;
        //private Vector3 _currentCameraOffset;
        //[SerializeField] private float _offsetMagnitude = 1f;
        //[SerializeField, Tooltip("Higher the responsiveness, faster the camera looks ahead of the player"), Range(0, 1)] private float _responsiveness;
        //[SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        //private float _offsetMultiplier;  // 0 when player is not throttling, 1 when player is throttling

        //private void Awake()
        //{
        //    _cameraTransform = transform;
        //}

        //private void Start()
        //{
        //    _playerInstance = Player.TryGetInstance;
        //    _playerTransform = _playerInstance.PlayerTransform;

        //    _offsetMultiplier = 0;
        //}

        //private void LateUpdate()
        //{
        //    _playerPosition = _playerTransform.position;
        //    _playerPosition.y = _cameraTransform.position.y;

        //    if (_playerInstance.IsThrottling)
        //    {
        //        _offsetMultiplier = Mathf.MoveTowards(_offsetMultiplier, 1, Time.deltaTime);
        //    }
        //    else
        //    {
        //        _offsetMultiplier = Mathf.MoveTowards(_offsetMultiplier, 0, Time.deltaTime);
        //    }

        //    float percentage = _playerInstance.ElapsedTime * _responsiveness;

        //    _targetCameraOffset = _playerTransform.forward * _offsetMagnitude * _offsetMultiplier;
        //    _currentCameraOffset = Vector3.Lerp(_currentCameraOffset, _targetCameraOffset, _curve.Evaluate(percentage));
        //    Debug.DrawRay(_playerPosition, _currentCameraOffset, Color.red);
        //    _cameraTransform.position = _playerPosition + _currentCameraOffset;
        //}
    }
}