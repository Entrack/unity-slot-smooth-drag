using UnityEngine;
using TouchScript.Gestures;


    public class Slot : MonoBehaviour
    {

        [HideInInspector]
        public int subjectColliderID;
        [HideInInspector]
        public Drag_obj obj;
        [HideInInspector]
        public bool occupied;
        [HideInInspector]
        public bool host = false;

        public string type = "";
        public bool isDestination = false;

        

        public void Start()
        {
            Host hostObject = GetComponent<Host>();
            if (null != hostObject)
            {
                host = true;
                subjectColliderID = hostObject.subject.GetComponent<Collider2D>().GetInstanceID();
            }
            occupied = false;
        }

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (host)
            {
                if (other.GetInstanceID() != subjectColliderID)
                {
                    return;
                }
            }
            if (!occupied)  //if it is not occupied
            {
                Drag_obj otherDrag = other.GetComponent<Drag_obj>();

                if ((type == otherDrag.type) || (type == ""))   //then, if type matches
                {
                    occupied = true;    //it is occupied now
                    obj = otherDrag;
                    if (isDestination)
                    {
                        obj.hasReachedDestination = true;
                    }
                }
            }
        }

        public void OnTriggerExit2D(Collider2D other)
        {
            if (null != obj)        //if we have an object
            {
                Drag_obj otherDrag = other.GetComponent<Drag_obj>();

                if (obj.GetInstanceID() == otherDrag.GetInstanceID())   //if it is our object
                {
                    if (isDestination)      //and object has not reached a cake
                    {
                        obj.hasReachedDestination = false;
                    }
                    obj = null;         //then slot is free
                    occupied = false;
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (host)
            {
                if (other.GetInstanceID() != subjectColliderID)
                {
                    return;
                }
            }
            if (occupied)   //if it is occupied
            {
                if (!obj.isCentered)    //and our object is not centered 
                {
                    obj.Center(this.transform.position);    //we will center it
                }
            }
            else        //if it is not occupied
            {
                Drag_obj otherDrag = other.GetComponent<Drag_obj>();

                if ((type == otherDrag.type) || (type == ""))      //if it our type
                {
                    obj = otherDrag;        //it is ours now
                    occupied = true;

                    obj.Center(this.transform.position);    //and we will center it
                    obj.centeringNow = true;
                }
            }
        }
    }