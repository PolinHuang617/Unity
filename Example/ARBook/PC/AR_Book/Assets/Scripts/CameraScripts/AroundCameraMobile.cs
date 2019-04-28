﻿using Developer.CameraExtension;
using UnityEngine;
using Utils;

[RequireComponent(typeof(Camera))]
public class AroundCameraMobile : MonoBehaviour
{
    #region Mono-Singleton

    private static AroundCameraMobile instance;
    public static AroundCameraMobile Instance
    {
        get
        {
            if (!instance)
                instance = FindObjectOfType<AroundCameraMobile>();
            return instance;
        }
    }

    #endregion

    private enum TouchType
    {
        None,
        Move,
        Scale,
        Rotate
    }

    public Transform Target
    {
        get
        {
            if (target == null)
            {
                var neg = new GameObject("targetCenter").transform;
                neg.SetParent(transform.root);
                renderCamOrigVector3 = neg.localPosition;
                renderCamOrigRotate = neg.localRotation;
                target = neg;
               // localTargetPos = neg.localPosition;
            }

            return target;
        }
        //set { target = value; }
    }

    private TouchType touchType = TouchType.None;

    private Vector3 renderCamOrigVector3;
    private Quaternion renderCamOrigRotate;

    //private Vector3 localTargetPos;

    #region Property and Field

    /// <summary>
    /// Around center.
    /// </summary>
    private Transform target;

    /// <summary>
    /// Settings of mouse button, pointer and scrollwheel.
    /// </summary>
    public MouseSettings touchSettings = new MouseSettings(1, 10, 10);

    /// <summary>
    /// Range limit of angle.
    /// </summary>
    public Range angleRangeX = new Range(-90, 90);
    public Range angleRangeY = new Range(-90, 90);

    /// <summary>
    /// Range limit of distance.
    /// </summary>
    public Range distanceRange = new Range(1, 10);

    /// <summary>
    /// Damper for move and rotate.
    /// </summary>
    [Range(0, 10)] public float damper = 5;

    /// <summary>
    /// Camera current angls.
    /// </summary>
    public Vector2 currentAngles { protected set; get; }

    /// <summary>
    /// Current distance from camera to target.
    /// </summary>
    public float currentDistance { protected set; get; }

    /// <summary>
    /// Camera target angls.
    /// </summary>
    protected Vector2 targetAngles;

    /// <summary>
    /// Target distance from camera to target.
    /// </summary>
    protected float targetDistance;

    /// <summary>
    /// Save origin position and angle
    /// </summary>
    protected Vector2 savedAngles;
    protected float savedDistance;
    protected Vector3 savedTransformPosition, savedTargetPosition;

    /// <summary>
    /// Position to save Target position on runtime(Check if Target is in Screen)
    /// </summary>
    protected Vector3 tempPosition;

    #endregion

    #region Protected Method
    
    protected virtual void Start()
    {
        savedAngles = currentAngles = targetAngles = transform.eulerAngles;
        savedDistance = currentDistance = targetDistance = Vector3.Distance(transform.position, Target.position);
        savedTransformPosition = transform.position;
        savedTargetPosition = Target.position;        
    }
   
    protected virtual void LateUpdate()
    {   
#if UNITY_STANDALONE
        if (Input.touchCount > 0)       // Adapt to Surface
            CheckTouchInput();
        else
            CheckMouseInput();
#endif

#if UNITY_ANDROID
        CheckTouchInput();
#endif
    }
    
    public void SetDefault()
    {
        //Target.localPosition = localTargetPos;
        Target.localPosition = renderCamOrigVector3;
        Target.localRotation = renderCamOrigRotate;
    }

    protected void CheckMouseInput()
    {
        if (Input.GetMouseButton(0))
        {
            //Mouse pointer.
            targetAngles.y += Input.GetAxis("Mouse X") * touchSettings.pointerSensitivity * 50;
            targetAngles.x -= Input.GetAxis("Mouse Y") * touchSettings.pointerSensitivity * 50;

            //Range.
            targetAngles.x = Mathf.Clamp(targetAngles.x, angleRangeX.min, angleRangeX.max);
            //targetAngles.y = Mathf.Clamp(targetAngles.y, angleRangeY.min, angleRangeY.max);
        }               

        //拖动
        if (Input.GetMouseButton(2))
        {
            float xAxis = Input.GetAxis("Mouse X") * 0.5f;
            float yAxis = Input.GetAxis("Mouse Y") * 0.5f;

            Target.position -= transform.right.normalized * xAxis + transform.up.normalized * yAxis;
        }

        //Mouse scrollwheel.
        targetDistance -= Input.GetAxis("Mouse ScrollWheel") * touchSettings.wheelSensitivity;
        targetDistance = Mathf.Clamp(targetDistance, distanceRange.min, distanceRange.max);

        //Lerp.
        currentAngles = Vector2.Lerp(currentAngles, targetAngles, damper * Time.deltaTime);
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, damper * Time.deltaTime);

        //Update transform position and rotation.
        tempPosition = Camera.main.WorldToViewportPoint(Target.position);        
        if (tempPosition.x >= 0 && tempPosition.x <= 1 && tempPosition.y >= 0 && tempPosition.y <= 1)
        {            
            transform.rotation = Quaternion.Euler(currentAngles);
            transform.position = Target.position - transform.forward * currentDistance;            
        }                       
    }

    /// <summary>
    /// Check and deal with mouse input. 
    /// </summary>
    protected void CheckTouchInput()
    {
        //单点触摸， 水平上下旋转
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
                touchType = TouchType.Rotate;

            if (touchType == TouchType.Rotate)
            {
                Vector2 deltaPos = touch.deltaPosition;

                //Mouse pointer.
                targetAngles.y += deltaPos.x * touchSettings.pointerSensitivity;
                targetAngles.x -= deltaPos.y * touchSettings.pointerSensitivity;

                //Range.
                targetAngles.x = Mathf.Clamp(targetAngles.x, angleRangeX.min, angleRangeX.max);
                //targetAngles.y = Mathf.Clamp(targetAngles.y, angleRangeY.min, angleRangeY.max);
            }
        }

        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            if (touchZero.phase == TouchPhase.Ended || touchOne.phase == TouchPhase.Ended)
            {
                touchType = TouchType.None;
                return;
            }

            // Find the position in the previous frame of each touch.
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            var dir = Vector2.Dot(touchZero.deltaPosition, touchOne.deltaPosition);

            touchType = dir > 0 ? TouchType.Move : TouchType.Scale;

            if (touchType == TouchType.Move)
            {
                //target.localPosition += new Vector3(touchZero.deltaPosition.x, touchZero.deltaPosition.y, 0) * 0.02f;
                
                Target.position -= (transform.right.normalized * touchZero.deltaPosition.x + transform.up.normalized * touchZero.deltaPosition.y) * 0.005f;

                return;
            }

            if (touchType == TouchType.Scale)
            {
                // Find the magnitude of the vector (the distance) between the touches in each frame.
                float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                // Find the difference in the distances between each frame.
                float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                float scaleF = -deltaMagnitudeDiff / 1000f;

                //Mouse scrollwheel.
                targetDistance -= scaleF * touchSettings.wheelSensitivity;
                targetDistance = Mathf.Clamp(targetDistance, distanceRange.min, distanceRange.max);
            }
        }

        //Lerp.
        currentAngles = Vector2.Lerp(currentAngles, targetAngles, damper * Time.deltaTime);
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, damper * Time.deltaTime);

        //Update transform position and rotation.
        tempPosition = Camera.main.WorldToViewportPoint(Target.position);
        if (tempPosition.x > 0 && tempPosition.x < 1 && tempPosition.y > 0 && tempPosition.y < 1)
        {
            transform.rotation = Quaternion.Euler(currentAngles);
            transform.position = Target.position - transform.forward * currentDistance;
            //Debug.Log("在屏幕内");
        }         
    }

    //void TouchControl()
    //{
    //    if (!isBegin)
    //        return;

    //    //没有触摸，就是触摸点为0
    //    if (Input.touchCount <= 0)
    //    {
    //        //touchType = TouchType.None;
    //        return;
    //    }

    //    //单点触摸， 水平上下旋转
    //    if (Input.touchCount == 1)
    //    {
    //        Touch touch = Input.GetTouch(0);
    //        if (touch.phase == TouchPhase.Began)
    //            touchType = TouchType.Rotate;

    //        if (touchType == TouchType.Rotate)
    //        {
    //            Vector2 deltaPos = touch.deltaPosition;
    //            //target.Rotate(new Vector3(deltaPos.y, -deltaPos.x, 0) * Time.deltaTime * rotateSpeed, Space.Self);

    //            target.Rotate(Vector3.down * deltaPos.x * Time.deltaTime * rotateSpeed, Space.Self); //绕Y轴进行旋转

    //            target.Rotate(Vector3.right * deltaPos.y * Time.deltaTime * rotateSpeed, Space.World); //绕X轴进行旋转，下面我们还可以写绕Z轴进行旋转

    //            return;
    //        }
    //    }

    //    if (Input.touchCount == 2)
    //    {
    //        Touch touchZero = Input.GetTouch(0);
    //        Touch touchOne = Input.GetTouch(1);

    //        if (touchZero.phase == TouchPhase.Ended || touchOne.phase == TouchPhase.Ended)
    //        {
    //            touchType = TouchType.None;
    //            return;
    //        }

    //        // Find the position in the previous frame of each touch.
    //        Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
    //        Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

    //        var dir = Vector2.Dot(touchZero.deltaPosition, touchOne.deltaPosition);

    //        //todo:可以进行更好的判断
    //        if (touchType == TouchType.None)
    //        {
    //            //if (touchZero.phase == TouchPhase.Began && touchOne.phase == TouchPhase.Began)
    //            // {
    //            // }
    //        }

    //        touchType = dir > 0 ? TouchType.Move : TouchType.Scale;

    //        if (touchType == TouchType.Move)
    //        {
    //            target.localPosition += new Vector3(touchZero.deltaPosition.x, touchZero.deltaPosition.y, 0) * 0.02f;
    //            return;
    //        }

    //        if (touchType == TouchType.Scale)
    //        {
    //            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
    //            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

    //            // Find the difference in the distances between each frame.
    //            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

    //            //if (deltaMagnitudeDiff < 0 && LimitCheck())
    //            //    return;

    //            float scaleF = deltaMagnitudeDiff / 1000f;

    //            target.localScale -= target.localScale * scaleF;
    //        }
    //    }
    //}

    #endregion
        
    #region 模型重置判断
    
    public void Reset(bool reset)
    {
        if (reset)
        {
            transform.position = savedTransformPosition;
            currentAngles = targetAngles = savedAngles;
            currentDistance = targetDistance = savedDistance;
            Target.position = savedTargetPosition;

            reset = false;
        }
    }
    #endregion

}