using UnityEngine;
using System.Collections;
using TouchScript.Gestures;

public class Host : MonoBehaviour {

    private Drag_obj obj;
    private TransformGesture gesture;

    public Transform subject;

    public void Start()
    {
        obj = subject.GetComponent<Drag_obj>();
        gesture = obj.GetComponent<TransformGesture>();

        obj.WaitAndCenter(transform.position);
        obj.Raise();
    }

    IEnumerator TakeBack()
    {
        while (gesture.ActiveTouches.Count != 0)
        {
            yield return new WaitForEndOfFrame();
        }
        if (gesture.ActiveTouches.Count == 0)   // if we are not holding
        {
            if (obj.hasReachedDestination == false) //if object has not reached destination
            {
                obj.Center(transform.position);     //center it
                yield break;
            }
            else
            {
                this.GetComponent<Slot>().occupied = false;     //else free slot
                this.GetComponent<Slot>().obj = null;
            }
        }
        //return null;
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        Drag_obj otherDrag = other.GetComponent<Drag_obj>();

        if (obj.GetInstanceID() == otherDrag.GetInstanceID())   //if it is our object, who came out
        {
            StartCoroutine(TakeBack());     //get him back
        }
    }
}
