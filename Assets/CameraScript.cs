using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;


public class CameraController : MonoBehaviour
{
    Vector3 touchStart;
    public float zoomOutMin = 1;
    public float zoomOutMax = 8;


    private Transform _target; // Yak�nla�t�r�lacak modelin transformu
    private Vector3 _targetPosition = new Vector3(0,0,0);

    public float scrollSpeed = 1f;
    private Vector3 dragOrigin;


    private float _mouseSensitivity = 3.0f;
    private float _rotationY;
    private float _rotationX;

    private float _distanceFromTarget = 3.0f;

    private Vector3 _currentRotation;
    private Vector3 _smoothVelocity = Vector3.zero;
    private float _smoothTime = 0.2f;

    private Vector2 _rotationXMinMax = new Vector2(-45, 85);


    void Start()
    {
        transform.LookAt(_targetPosition);
    }

    void Update()
    {
        if (Input.GetMouseButton(1) || Input.GetMouseButton(0)) // Left mouse button click
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;

            if (EventSystem.current.IsPointerOverGameObject(eventData.pointerId))
            {
                // do nothing
            }
            else
            {
                MotorScript motorScript = GetComponent<MotorScript>();
                if (motorScript.model != null)
                {
                    _target = motorScript.model;
                }
                

                if (Input.GetMouseButton(0))
                {
                    float mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity;
                    float mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity;

                    _rotationY += mouseX;
                    _rotationX += mouseY;

                    _rotationX = Mathf.Clamp(_rotationX, _rotationXMinMax.x, _rotationXMinMax.y);
                    Vector3 nextRotation = new Vector3(_rotationX, _rotationY);

                    _distanceFromTarget = Vector3.Distance(transform.position, _targetPosition);

                    _currentRotation = Vector3.SmoothDamp(_currentRotation, nextRotation, ref _smoothVelocity, _smoothTime);
                    transform.localEulerAngles = _currentRotation;

                    if (_target != null)
                    {
                        transform.position = _target.position - transform.forward * _distanceFromTarget;
                        transform.LookAt(_targetPosition);
                    }
                    else
                    {
                        _targetPosition = new Vector3(0, 0, 0);
                    }
                }
            }
        }

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0f)
        {
            // Kameranın bakış doğrultusunda ilerleme vektörü
            Vector3 moveDirection = Camera.main.transform.forward * scrollInput * scrollSpeed /2 ;

            // Yeni konumu hesapla ve uygula
            transform.position += moveDirection;
        }

        /*if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - difference * 0.01f, zoomOutMin, zoomOutMax);
        }*/

    }
}
