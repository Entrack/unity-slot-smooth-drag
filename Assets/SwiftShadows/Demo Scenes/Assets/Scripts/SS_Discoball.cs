using UnityEngine;
using LostPolygon.SwiftShadows;

public class SS_Discoball : MonoBehaviour {
    public int LightsCount = 200;
    public float BallSize = 4f;
    public GameObject ShadowPrefab;

    private void Start() {
        if (ShadowPrefab == null)
            return;

        for (int i = 0; i < LightsCount; i++) {
            SwiftShadow shadow = ((GameObject) Instantiate(ShadowPrefab)).GetComponent<SwiftShadow>();
            shadow.LightSourceObject = transform;
            Color randomColor = new SS_ColorHSV(Random.Range(0f, 360f), 0.8f, 1f).ToColor();
            randomColor.a = 0.8f;
            shadow.InitialColor = randomColor;

            shadow.gameObject.transform.parent = transform;
            shadow.gameObject.transform.localPosition = Random.onUnitSphere.normalized * BallSize / transform.lossyScale.magnitude;
        }
    }
}
