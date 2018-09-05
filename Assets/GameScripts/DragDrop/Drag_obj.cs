
using System;
using UnityEngine;
using TouchScript.Gestures;
using TouchScript.Behaviors;
using System.Collections;
using DynamicShadowProjector;

public class Drag_obj : MonoBehaviour
{
    #region Private variables
    private TransformGesture gesture;
    private Transformer transformer;
    private Rigidbody2D rb;
    private ShadowTextureRenderer shadowTexRend;
    #endregion

    #region Public variables
    public Transform targetObject;
    public Transform projector;
    //public Transform shadow;

    public string type = "";

    [HideInInspector]
    public bool isRaisedOnce = false;
    [HideInInspector]
    public bool hasReachedDestination = false;
    [HideInInspector]
    public bool centeringNow = false;
    [HideInInspector]
    public bool isCentered = false;
    #endregion

    private void OnEnable()
    {
        gesture = this.GetComponent<TransformGesture>();
        transformer = this.GetComponent<Transformer>();
        rb = this.GetComponent<Rigidbody2D>();
        shadowTexRend = projector.GetComponent<ShadowTextureRenderer>();

        transformer.enabled = false;
        rb.isKinematic = false;
        gesture.TransformStarted += transformStartedHandler;
        gesture.TransformCompleted += transformCompletedHandler;
    }

    private void OnDisable()
    {
        gesture.TransformStarted -= transformStartedHandler;
        gesture.TransformCompleted -= transformCompletedHandler;
    }

    private void transformStartedHandler(object sender, EventArgs e)
    {
        projector.gameObject.SetActive(true);
        if (!isRaisedOnce)
            Raise();
        if (isCentered)
            isCentered = false;
        rb.isKinematic = true;
        transformer.enabled = true;
        ScaleUp(true);
    }

    private void transformCompletedHandler(object sender, EventArgs e)
    {
        projector.gameObject.SetActive(false);
        transformer.enabled = false;
        rb.isKinematic = false;
        rb.WakeUp();
        ScaleUp(false);
    }

    public void Raise()
    {
        this.transform.position += GameMaster.camDirection * GameMaster.alphaOffset;
        isRaisedOnce = true;
    }

    public void Center(Vector3 center)
    {
        if (!centeringNow)
        {
            if (gesture.ActiveTouches.Count == 0)
            {
                centeringNow = true;
                isCentered = false;

                StartCoroutine(SmoothTransition(transform.position, center));
            }
        }
    }

    public void WaitAndCenter(Vector3 center)
    {
        if (!centeringNow)
        {
            StartCoroutine(SmoothDelayedTransition(transform.position, center));
        }
    }

    IEnumerator SmoothTransition(Vector3 start, Vector3 end)
    {
        float timeSinceStarted = 0f;
        Vector3 tmp = new Vector3();

        while (true)
        {
            timeSinceStarted += Time.deltaTime * 4.0f;  //speed
            tmp = Vector3.Lerp(transform.position, end, timeSinceStarted);
            this.transform.position = new Vector3(tmp.x, tmp.y, this.transform.position.z);

            // If the object has arrived, stop the coroutine
            if ((transform.position.x == end.x) && (transform.position.y == end.y))
            {
                centeringNow = false;
                isCentered = true;
                rb.isKinematic = true;
                yield break;
            }
            // Otherwise, continue next frame
            yield return null;
        }
    }

    IEnumerator SmoothDelayedTransition(Vector3 start, Vector3 end)
    {
        float timeSinceStarted = 0f;
        Vector3 tmp = new Vector3();

        while (gesture.ActiveTouches.Count != 0)
        {
            yield return new WaitForEndOfFrame();
        }
        if (gesture.ActiveTouches.Count == 0)
        {
            while (true)
            {
                timeSinceStarted += Time.deltaTime;
                tmp = Vector3.Lerp(transform.position, end, timeSinceStarted);
                this.transform.position = new Vector3(tmp.x, tmp.y, this.transform.position.z);

                // If the object has arrived, stop the coroutine
                if ((transform.position.x == end.x) && (transform.position.y == end.y))
                {
                    rb.isKinematic = true;
                    yield break;
                }

                // Otherwise, continue next frame
                yield return null;
            }
        }
    }

    public void ScaleUp(bool isUp)
    {
        float offset = 0.01f;
        if (isUp)
        {
            transform.position += GameMaster.camDirection * GameMaster.height;
            targetObject.localScale *= GameMaster.scaling;
            //shadow.localScale *= (GameMaster.scaling + offset);
            shadowTexRend.blurLevel = GameMaster.highBlurLevel;
            shadowTexRend.blurSize = GameMaster.highBlurAmount;
        }
        else
        {
            transform.position -= GameMaster.camDirection * GameMaster.height;
            targetObject.localScale /= GameMaster.scaling;
            //shadow.localScale /= (GameMaster.scaling + offset);
            shadowTexRend.blurLevel = GameMaster.lowBlurLevel;
            shadowTexRend.blurSize = GameMaster.lowBlurAmount;
        }
    }
}