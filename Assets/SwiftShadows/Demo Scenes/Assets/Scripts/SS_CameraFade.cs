using UnityEngine;
using System;

public class SS_CameraFade : MonoBehaviour {
    public GUIStyle m_BackgroundStyle = new GUIStyle(); // Style for background tiling
    public Texture2D m_FadeTexture; // 1x1 pixel texture used for fading
    public Color m_CurrentScreenOverlayColor = new Color(0, 0, 0, 0); // default starting color: black and fully transparrent
    public Color m_TargetScreenOverlayColor = new Color(0, 0, 0, 0); // default target color: black and fully transparrent
    public Color m_DeltaColor = new Color(0, 0, 0, 0); // the delta-color is basically the "speed / second" at which the current color should change
    public int m_FadeGUIDepth = -1000; // make sure this texture is drawn on top of everything

    public float m_FadeDelay = 0;
    public Action m_OnFadeFinish = null;

    private static SS_CameraFade _instance;

    private static SS_CameraFade Instance {
        get {
            if (_instance == null) {
                _instance = FindObjectOfType<SS_CameraFade>();
                if (_instance == null) {
                    _instance = new GameObject("CameraFade").AddComponent<SS_CameraFade>();
                }
            }

            return _instance;
        }
    }

    private void Awake() {
        if (_instance == null) {
            _instance = this;
            Instance.Init();
        }
    }

    // Initialize the texture, background-style and initial color:
    public void Init() {
        Instance.m_FadeTexture = new Texture2D(1, 1);
        Instance.m_BackgroundStyle.normal.background = Instance.m_FadeTexture;
    }

    // Draw the texture and perform the fade:
    private void OnGUI() {
        // If delay is over...
        if (Time.time > Instance.m_FadeDelay) {
            // If the current color of the screen is not equal to the desired color: keep fading!
            if (Instance.m_CurrentScreenOverlayColor != Instance.m_TargetScreenOverlayColor) {
                // If the difference between the current alpha and the desired alpha is smaller than delta-alpha * deltaTime, then we're pretty much done fading:
                if (Mathf.Abs(Instance.m_CurrentScreenOverlayColor.a - Instance.m_TargetScreenOverlayColor.a) < Mathf.Abs(Instance.m_DeltaColor.a) * Time.deltaTime) {
                    Instance.m_CurrentScreenOverlayColor = Instance.m_TargetScreenOverlayColor;
                    SetScreenOverlayColor(Instance.m_CurrentScreenOverlayColor);
                    Instance.m_DeltaColor = new Color(0, 0, 0, 0);

                    if (Instance.m_OnFadeFinish != null) {
                        Instance.m_OnFadeFinish();
                    }

                    Die();
                } else {
                    // Fade!
                    SetScreenOverlayColor(Instance.m_CurrentScreenOverlayColor + Instance.m_DeltaColor * Time.deltaTime);
                }
            }
        }
        // Only draw the texture when the alpha value is greater than 0:
        if (m_CurrentScreenOverlayColor.a > 0) {
            GUI.depth = Instance.m_FadeGUIDepth;
            GUI.Label(new Rect(-10, -10, Screen.width + 10, Screen.height + 10), Instance.m_FadeTexture, Instance.m_BackgroundStyle);
        }
    }

    /// <summary>
    /// Sets the color of the screen overlay instantly.  Useful to start a fade.
    /// </summary>
    /// <param name='newScreenOverlayColor'>
    /// New screen overlay color.
    /// </param>
    private static void SetScreenOverlayColor(Color newScreenOverlayColor) {
        Instance.m_CurrentScreenOverlayColor = newScreenOverlayColor;
        Instance.m_FadeTexture.SetPixel(0, 0, Instance.m_CurrentScreenOverlayColor);
        Instance.m_FadeTexture.Apply();
    }

    /// <summary>
    /// Starts the fade from color newScreenOverlayColor. If isFadeIn, start fully opaque, else start transparent.
    /// </summary>
    /// <param name='newScreenOverlayColor'>
    /// Target screen overlay Color.
    /// </param>
    /// <param name='isFadeIn'>
    /// Whether to fade in or out.
    /// </param>
    /// <param name='fadeDuration'>
    /// Fade duration.
    /// </param>
    public static void StartAlphaFade(Color newScreenOverlayColor, bool isFadeIn, float fadeDuration) {
        if (fadeDuration <= 0.0f) {
            SetScreenOverlayColor(newScreenOverlayColor);
        } else {
            if (isFadeIn) {
                Instance.m_TargetScreenOverlayColor = new Color(newScreenOverlayColor.r, newScreenOverlayColor.g, newScreenOverlayColor.b, 0);
                SetScreenOverlayColor(newScreenOverlayColor);
            } else {
                Instance.m_TargetScreenOverlayColor = newScreenOverlayColor;
                SetScreenOverlayColor(new Color(newScreenOverlayColor.r, newScreenOverlayColor.g, newScreenOverlayColor.b, 0));
            }

            Instance.m_DeltaColor = (Instance.m_TargetScreenOverlayColor - Instance.m_CurrentScreenOverlayColor) / fadeDuration;
        }
    }

    /// <summary>
    /// Starts the fade from color newScreenOverlayColor. If isFadeIn, start fully opaque, else start transparent, after a delay.
    /// </summary>
    /// <param name='newScreenOverlayColor'>
    /// New screen overlay color.
    /// </param>
    /// <param name='fadeDuration'>
    /// Fade duration.
    /// </param>
    /// <param name='isFadeIn'>
    /// Whether to fade in or out.
    /// </param>
    /// <param name='fadeDelay'>
    /// Fade delay.
    /// </param>
    public static void StartAlphaFade(Color newScreenOverlayColor, bool isFadeIn, float fadeDuration, float fadeDelay) {
        if (fadeDuration <= 0.0f) {
            SetScreenOverlayColor(newScreenOverlayColor);
        } else {
            Instance.m_FadeDelay = Time.time + fadeDelay;

            if (isFadeIn) {
                Instance.m_TargetScreenOverlayColor = new Color(newScreenOverlayColor.r, newScreenOverlayColor.g, newScreenOverlayColor.b, 0);
                SetScreenOverlayColor(newScreenOverlayColor);
            } else {
                Instance.m_TargetScreenOverlayColor = newScreenOverlayColor;
                SetScreenOverlayColor(new Color(newScreenOverlayColor.r, newScreenOverlayColor.g, newScreenOverlayColor.b, 0));
            }

            Instance.m_DeltaColor = (Instance.m_TargetScreenOverlayColor - Instance.m_CurrentScreenOverlayColor) / fadeDuration;
        }
    }

    /// <summary>
    /// Starts the fade from color newScreenOverlayColor. If isFadeIn, start fully opaque, else start transparent, after a delay, with Action OnFadeFinish.
    /// </summary>
    /// <param name='newScreenOverlayColor'>
    /// New screen overlay color.
    /// </param>
    /// <param name='isFadeIn'>
    /// Whether to fade in or out.
    /// </param>
    /// <param name='fadeDuration'>
    /// Fade duration.
    /// </param>
    /// <param name='fadeDelay'>
    /// Fade delay.
    /// </param>
    /// <param name='OnFadeFinish'>
    /// On fade finish, doWork().
    /// </param>
    public static void StartAlphaFade(Color newScreenOverlayColor, bool isFadeIn, float fadeDuration, float fadeDelay, Action OnFadeFinish) {
        if (fadeDuration <= 0.0f) {
            SetScreenOverlayColor(newScreenOverlayColor);
        } else {
            Instance.m_OnFadeFinish = OnFadeFinish;
            Instance.m_FadeDelay = Time.time + fadeDelay;

            if (isFadeIn) {
                Instance.m_TargetScreenOverlayColor = new Color(newScreenOverlayColor.r, newScreenOverlayColor.g, newScreenOverlayColor.b, 0);
                SetScreenOverlayColor(newScreenOverlayColor);
            } else {
                Instance.m_TargetScreenOverlayColor = newScreenOverlayColor;
                SetScreenOverlayColor(new Color(newScreenOverlayColor.r, newScreenOverlayColor.g, newScreenOverlayColor.b, 0));
            }
            Instance.m_DeltaColor = (Instance.m_TargetScreenOverlayColor - Instance.m_CurrentScreenOverlayColor) / fadeDuration;
        }
    }

    private void Die() {
        _instance = null;
        Destroy(gameObject);
    }

    private void OnApplicationQuit() {
        _instance = null;
    }
}