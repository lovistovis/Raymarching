using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class ShaderHandler : MonoBehaviour
{
    [SerializeField] private ComputeShader rayMarchingShader;
    [SerializeField] private Vector3 startPos = new Vector3(2.0f, 1.0f, 0.5f);
    [SerializeField] private float baseMoveSpeed;
    [FormerlySerializedAs("rayMarchingIterations")][SerializeField][Range(1, 10000)] private int baseRayMarchingIterations;
    [SerializeField][Range(0, 2)] private float mouseSensitivity;
    [SerializeField][Range(0, 10)] private float zoomSensitivity;
    [SerializeField][Range(0, 10)] private float speedSensitivity;
    [SerializeField][Range(0, 1)] private float speedFactorOnShift;

    private RenderTexture renderTexture;
    private Vector3 position;
    private Quaternion cameraRotation = Quaternion.identity;
    private Vector3 cameraDirection = new Vector3(0, 0, 0);
    private Camera mainCamera;

    private float cameraRotationX = 0;
    private float cameraRotationY = 0;
    private float moveSpeed;
    private int iterations;

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

        position = startPos;
        moveSpeed = baseMoveSpeed;
        iterations = baseRayMarchingIterations;

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
        //float moveSpeed = walkSpeed + Time.timeSinceLevelLoad / 100;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Input.GetKey(KeyCode.E))
        {
            moveSpeed *= Mathf.Abs(1 + scroll * speedSensitivity);
        }
        else if (Input.GetKey(KeyCode.I))
        {
            iterations += scroll != 0 ? Mathf.RoundToInt(Mathf.Sign(scroll)) : 0;
            iterations = Mathf.RoundToInt(iterations * Mathf.Abs(1 + scroll * speedSensitivity));
        }
        else
        {
            mainCamera.fieldOfView -= scroll * zoomSensitivity;
            mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView, 0, 180);
        }

        //position += transform.forward * moveSpeed;
        float currentMoveSpeed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * speedFactorOnShift : moveSpeed;
        position += (transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical")) * currentMoveSpeed;

        if (!Input.GetKey(KeyCode.LeftControl)) { lastTime += Time.deltaTime; }
        else if (Input.GetKey(KeyCode.LeftAlt)) { lastTime -= Time.deltaTime; }

        if (Input.GetKeyDown(KeyCode.R))
        {
            mainCamera.fieldOfView = 60;
            moveSpeed = baseMoveSpeed;
        }

        if (Input.GetKey(KeyCode.M)) { position = startPos; }

        Debug.Log((Time.timeSinceLevelLoad * 100).ToString() + ", " + iterations.ToString());
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        int kernelIndex = rayMarchingShader.FindKernel("CSMain");

        ComputeBuffer loseBuffer = new ComputeBuffer(1, sizeof(float));
        loseBuffer.SetData(new float[] { 0 });

        rayMarchingShader.SetTexture(kernelIndex, "RenderTexture", renderTexture);
        rayMarchingShader.SetBuffer(kernelIndex, "LoseBuffer", loseBuffer);
        rayMarchingShader.SetInts("Resolution", renderTexture.width, renderTexture.height);
        rayMarchingShader.SetInt("RayMarchingIterations", iterations);
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