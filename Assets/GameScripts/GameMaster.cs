using UnityEngine;
using System.Collections;

public class GameMaster : MonoBehaviour {

    public static GameMaster gm;

    public static Vector3 camDirection = new Vector3(0, 0, -1);
    public static float height = 2f;
    public static float alphaOffset = 0.1f;
    public static float scaling = 1.2f;
    public static int lowBlurLevel = 1;
    public static int highBlurLevel = 1;
    public static float lowBlurAmount = 2.0f;
    public static float highBlurAmount = 6.0f;

    void Awake()    //singletone
    {
        if (gm == null)
        {
            gm = GameObject.FindGameObjectWithTag("GM").GetComponent<GameMaster>();
        }
    }

}
