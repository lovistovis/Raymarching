using UnityEngine;
using UnityEngine.SceneManagement;

public class ShaderHandler : MonoBehaviour
{
    [SerializeField] private ComputeShader rayMarchingShader;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    [SerializeField][Range(1, 10000)] private int rayMarchingIterations;
    [SerializeField][Range(0, 2)] private float mouseSensitivity;
    [SerializeField][Range(0, 10)] private float zoomSensitivity;

    private RenderTexture renderTexture;
    private Vector3 position = new Vector3(2.0f, 1.0f, 0.5f);
    private Quaternion cameraRotation = Quaternion.identity;
    private Vector3 cameraDirection = new Vector3(0, 0, 0);
    private Camera mainCamera;

    private float cameraRotationX = 0;
    private float cameraRotationY = 0;

    private float lastTime = 0;


    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        // Set up render texture
        renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        renderTexture.enableRandomWrite = true;

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Camera rotation
        cameraRotationX += Input.GetAxis("Mouse X") * mouseSensitivity;
        cameraRotationY -= Input.GetAxis("Mouse Y") * mouseSensitivity;

        cameraRotationY = Mathf.Clamp(cameraRotationY, -90, 90);

        transform.localEulerAngles = new Vector3(cameraRotationY, cameraRotationX, 0);


        // Movement
        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        //float moveSpeed = walkSpeed + Time.timeSinceLevelLoad / 100;

        //position += transform.forward * moveSpeed;
        position += (transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical")) * moveSpeed;
        
        if (Input.GetKey(KeyCode.LeftControl)) { lastTime += Time.deltaTime; }

        mainCamera.fieldOfView -= Input.GetAxis("Mouse ScrollWheel") * zoomSensitivity;
        mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView, 0, 180);

        if (Input.GetKeyDown(KeyCode.R)) { mainCamera.fieldOfView = 60; }

        Debug.Log((int)(Time.timeSinceLevelLoad * 100));
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        int kernelIndex = rayMarchingShader.FindKernel("CSMain");

        ComputeBuffer loseBuffer = new ComputeBuffer(1, sizeof(float));
        loseBuffer.SetData(new float[] { 0 });

        rayMarchingShader.SetTexture(kernelIndex, "RenderTexture", renderTexture);
        rayMarchingShader.SetBuffer(kernelIndex, "LoseBuffer", loseBuffer);
        rayMarchingShader.SetInts("Resolution", renderTexture.width, renderTexture.height);
        rayMarchingShader.SetInt("RayMarchingIterations", rayMarchingIterations);
        rayMarchingShader.SetFloats("CameraPosition", position.x, position.y, position.z);
        rayMarchingShader.SetFloat("Time", lastTime);
        rayMarchingShader.SetMatrix("CameraToWorld", mainCamera.cameraToWorldMatrix);
        rayMarchingShader.SetMatrix("CameraInverseProjection", mainCamera.projectionMatrix.inverse);

        rayMarchingShader.Dispatch(kernelIndex, renderTexture.width / 8, renderTexture.height / 8, 1);

        float[] data = new float[1];
        loseBuffer.GetData(data);
        if (data[0] == 1)
        {
            Debug.Log("Lost");
            SceneManager.LoadScene(0);
        }

        loseBuffer.Dispose();

        // Copy contents from new render texture to camera destination
        Graphics.Blit(renderTexture, destination);
    }
}