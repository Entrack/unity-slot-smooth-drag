using UnityEngine;

public class SS_ThirdPersonControllerJoystick : MonoBehaviour {
    public SS_ThirdPersonController ThirdPersonController;
    public SS_Joystick MoveJoystick;

    private void Start() {
        if (!SS_GUILayout.IsRuntimePlatformMobile()) {
            gameObject.SetActive(false);
        }
    }

    private void Update() {
        if (MoveJoystick != null) {
            ThirdPersonController.UseExternalInput = true;
            ThirdPersonController.ExternalInput = MoveJoystick.position;
        }
    }
}

