using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private float gravityValue = -9.81f;
    private bool groundedPlayer;
    private float verticalVelocity = 0.0f;
    public CharacterController characterController;


    public Camera playerCamera;

    public bool hasBall;

    public interface PlayerControllerPlatform
    {
        Vector2 HandleMovement(CharacterController characterController, Transform playerTransform);
        void HandleLookDir(Transform cameraTransform, Transform playerTransform);

        bool HandleInteractiveButton();
        bool HandleCancelButton();
        void HandleHandTransform(Transform lefthand, Transform righthand, Transform playerTransform);
    }

    /*
    Debug Only
    */
    class PlayerControllerPC : PlayerControllerPlatform
    {
        float rotationX = 0;
        public float lookSpeed = 2.0f;
        public float lookXLimit = 45.0f;

        float movementVelocity = 5.0f;

        public void HandleLookDir(Transform cameraTransform, Transform playerTransform)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            cameraTransform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            playerTransform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }

        public Vector2 HandleMovement(CharacterController characterController, Transform playerTransform)
        {
            // We are grounded, so recalculate move direction based on axes
            Vector3 forward = playerTransform.TransformDirection(Vector3.forward);
            Vector3 right = playerTransform.TransformDirection(Vector3.right);

            float curSpeedX = movementVelocity * Input.GetAxis("Vertical");
            float curSpeedY = 0.6f * movementVelocity * Input.GetAxis("Horizontal");

            Vector3 moveDirection = (forward * curSpeedX) + (right * curSpeedY);

            //Debug.Log(moveDirection.ToString());

            characterController.Move(Time.deltaTime * moveDirection);

            return new Vector2(curSpeedX, curSpeedY);
        }

        public bool HandleInteractiveButton()
        {
            return Input.GetKeyDown(KeyCode.E);
        }

        public bool HandleCancelButton()
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }
        
        public void HandleHandTransform(Transform lefthand, Transform righthand, Transform playerTransform) { }
    }


    class PlayerControllerVR : PlayerControllerPlatform
    {
        float movementVelocity = 5.0f;

        public bool HandleCancelButton()
        {
            InputDevice inputDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            
            bool triggered;
            inputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggered);

            return triggered;
        }

        public bool HandleInteractiveButton()
        {
            InputDevice inputDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            
            // use trigger button for throw and pick logic
            bool triggered;
            inputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggered);
            
            return triggered;
        }

        public void HandleLookDir(Transform cameraTransform, Transform playerTransform)
        {
        }

        public Vector2 HandleMovement(CharacterController characterController, Transform playerTransform)
        {
            // use 
            InputDevice inputDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            Vector2 axisVal = Vector2.zero; 
            inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out axisVal);

            if(FadeLoop.MoveTips.gameObject.activeSelf)
            {
                if(axisVal.x > 0.05f || axisVal.y > 0.05f)
                {
                    FadeLoop.MoveTips.gameObject.SetActive(false);
                    FadeLoop.StartPickTips();
                }
            }

            Vector3 forward =  Camera.main.transform.forward;
            Vector3 right =  Camera.main.transform.right;

            float curSpeedX = movementVelocity * axisVal.y;
            float curSpeedY = 0.6f * movementVelocity * axisVal.x;

            Vector3 moveDirection = (forward * curSpeedX) + (right * curSpeedY);
            //Debug.Log(moveDirection);
            if(moveDirection.magnitude > 0.5f)
            {
                characterController.Move(Time.deltaTime * moveDirection);
            }
            //playerTransform.position = characterController.transform.position;

            return new Vector2(curSpeedX, curSpeedY);
        }

        public void HandleHandTransform(Transform lefthand, Transform righthand, Transform playerTransform)
        {
            InputDevice deviceLeftHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            InputDevice deviceRightHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);

            Vector3 deviceLeftHandPos, deviceRightHandPos;
            Quaternion deviceLeftHandRot, deviceRightHandRot;

            deviceLeftHand.TryGetFeatureValue(CommonUsages.devicePosition, out deviceLeftHandPos);
            deviceLeftHand.TryGetFeatureValue(CommonUsages.deviceRotation, out deviceLeftHandRot);
            deviceRightHand.TryGetFeatureValue(CommonUsages.devicePosition, out deviceRightHandPos);
            deviceRightHand.TryGetFeatureValue(CommonUsages.deviceRotation, out deviceRightHandRot);

            lefthand.localPosition = deviceLeftHandPos;
            lefthand.rotation = deviceLeftHandRot;

            righthand.localPosition = deviceRightHandPos;
            righthand.rotation = deviceRightHandRot;
        }
    }

    public PlayerControllerPlatform playerControllerPlatform;

    // Start is called before the first frame update
    void Start()
    {
#if WMR_HEADSET
        playerControllerPlatform = new PlayerControllerWMR();
#elif VALVE_HEADSET
        playerControllerPlatform = new PlayerControllerValve();
#else
        if (GameMode.GetInstance().enviromentType == GameMode.ENVIRONMENT_TYPE.PC)
        {
            playerControllerPlatform = new PlayerControllerPC();
        }
        else if (GameMode.GetInstance().enviromentType == GameMode.ENVIRONMENT_TYPE.VR)
        {
            playerControllerPlatform = new PlayerControllerVR();
        }
#endif

        characterController = GetComponent<CharacterController>();

        //Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        // set the hand gameobject pos. to controller's pos.
        playerControllerPlatform.HandleHandTransform(GameMode.GetInstance().LeftHand, GameMode.GetInstance().RightHand, this.transform);

        groundedPlayer = characterController.isGrounded;
        if (groundedPlayer && verticalVelocity < 0)
        {
            verticalVelocity = 0f;
        }

        Vector3 movement = playerControllerPlatform.HandleMovement(characterController, transform);
        playerControllerPlatform.HandleLookDir(playerCamera.transform, transform);

        // Add gravity
        verticalVelocity += gravityValue * Time.deltaTime;

        // Vertical Movement
        characterController.Move(verticalVelocity * Time.deltaTime * Vector3.up);

        if(playerControllerPlatform.HandleCancelButton())
        {
// #if UNITY_EDITOR
//             UnityEditor.EditorApplication.isPlaying = false;
// #else
//             Application.Quit();
// #endif
        }
    }
}
