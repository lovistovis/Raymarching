using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using System.IO;

public class ShaderHandler : MonoBehaviour
{
    [SerializeField] private ComputeShader rayMarchingShader;
    [SerializeField] private Vector3 startPos = new Vector3(2.0f, 1.0f, 0.5f);
    [SerializeField] private int photoResolutionX = 4096;
    [SerializeField] private int photoResolutionY = 4096;
    [SerializeField] private int startFunctionNum;
    [SerializeField] private int startColorNum;
    [SerializeField] private float baseMoveSpeed;
    [SerializeField] private float baseTimeSpeed;
    [SerializeField][Range(1, 10000)] private int baseRayMarchingIterations;
    [SerializeField][Range(0, 2)] private float mouseSensitivity;
    [SerializeField][Range(0, 2)] private float rollSensitivity;
    [SerializeField][Range(0, 10)] private float zoomSensitivity;
    [SerializeField][Range(0, 10)] private float speedSensitivity;
    [SerializeField][Range(0, 10)] private float timeSensitivity;
    [SerializeField][Range(1, 2)] private float speedFactorOnShift;

    private RenderTexture renderTexture;
    private RenderTexture photoRenderTexture;
    private Quaternion cameraRotation = Quaternion.identity;
    private Vector3 cameraDirection = new Vector3(0, 0, 0);
    private Camera mainCamera;

    private float cameraRotationX = 0;
    private float cameraRotationY = 0;
    private float cameraRotationZ = 0;
    private bool moveTime = true;
    private bool saveNextFrame = false;
    private bool alwaysShowFinalColor = true;
    private float moveSpeed;
    private float timeSpeed;
    private int iterations;
    private string frameName;
    private float randomRange = 1000f;
    private float lastTime = 0;
    private int functionNum;
    private int colorNum;
    private int saveFrames = 0;

    public static ShaderHandler Instance;

    Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    private void ResolutionCheck()
    {
        if (renderTexture == null || renderTexture.width != Screen.width || renderTexture.height != Screen.height)
        {
            renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            renderTexture.enableRandomWrite = true;
        }
        if (photoRenderTexture == null)
        {
            photoRenderTexture = new RenderTexture(photoResolutionX, photoResolutionY, 24);
            photoRenderTexture.enableRandomWrite = true;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        mainCamera = Camera.main;

        transform.position = startPos;
        moveSpeed = baseMoveSpeed;
        timeSpeed = baseTimeSpeed;
        iterations = baseRayMarchingIterations;
        functionNum = startFunctionNum;
        colorNum = startColorNum;

        // Set up render texture
        ResolutionCheck();

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        ResolutionCheck();

        // Camera rotation
        cameraRotationX += Input.GetAxis("Mouse X") * mouseSensitivity * mainCamera.fieldOfView * 0.05f;
        cameraRotationY -= Input.GetAxis("Mouse Y") * mouseSensitivity * mainCamera.fieldOfView * 0.05f;

        cameraRotationY = Mathf.Clamp(cameraRotationY, -90, 90);

        if (Input.GetKey(KeyCode.Period)) { cameraRotationZ += rollSensitivity; }
        if (Input.GetKey(KeyCode.Comma)) { cameraRotationZ -= rollSensitivity; }

        transform.localEulerAngles = new Vector3(cameraRotationY, cameraRotationX, cameraRotationZ);

        frameName = "N" + functionNum + "_C" + colorNum + "_T" + lastTime + "_P" + transform.position.x + ";" + transform.position.y + ";" + transform.position.y + "_F" + mainCamera.fieldOfView + "_R" + cameraRotationX + ";" + cameraRotationY;
        Debug.Log(frameName);

        // Movement
        //float moveSpeed = walkSpeed + Time.timeSinceLevelLoad / 100;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Input.GetKey(KeyCode.E))
        {
            moveSpeed *= Mathf.Abs(1 + scroll * speedSensitivity);
        }
        else if (Input.GetKey(KeyCode.F))
        {
            timeSpeed *= Mathf.Abs(1 + scroll * speedSensitivity);
        }
        else if (Input.GetKey(KeyCode.I))
        {
            iterations += scroll != 0 ? Mathf.RoundToInt(Mathf.Sign(scroll)) : 0;
            iterations = Mathf.RoundToInt(iterations * Mathf.Abs(1 + scroll * speedSensitivity));
        }
        else
        {
            mainCamera.fieldOfView -= scroll * (zoomSensitivity * 0.1f * mainCamera.fieldOfView);
            mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView, 0, 180);
        }

        //position += transform.forward * moveSpeed;
        float currentMoveSpeed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * speedFactorOnShift : moveSpeed;
        transform.position += (transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical")) * currentMoveSpeed / 0.001f;

        if (moveTime) { lastTime += Time.deltaTime * timeSpeed; }

        if (Input.GetKeyDown(KeyCode.End))
        {
            moveTime = true;
        }

        if (Input.GetKeyUp(KeyCode.End))
        {
            moveTime = false;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            moveSpeed = baseMoveSpeed;
            timeSpeed = baseTimeSpeed;
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            cameraRotationZ = 0f;
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            mainCamera.fieldOfView = 60;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            moveTime = !moveTime;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            alwaysShowFinalColor = !alwaysShowFinalColor;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            timeSpeed = timeSpeed * -1;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            lastTime = 0;
        }

        if (Input.GetKeyDown(KeyCode.M)) { transform.position = startPos; }

        if (Input.GetKeyDown(KeyCode.O))
        {
            saveNextFrame = true;
        }

        if (Input.GetKey(KeyCode.C))
        {
            if (Input.GetKeyDown(KeyCode.PageUp)) { colorNum++; }
            else if (Input.GetKeyDown(KeyCode.PageDown)) { colorNum--; }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.PageUp)) { functionNum++; }
            else if (Input.GetKeyDown(KeyCode.PageDown)) { functionNum--; }
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        int kernelIndex = rayMarchingShader.FindKernel("CSMain");

        ComputeBuffer loseBuffer = new ComputeBuffer(1, sizeof(float));
        loseBuffer.SetData(new float[] { 0 });

        renderTexture = saveNextFrame ? photoRenderTexture : renderTexture;

        rayMarchingShader.SetTexture(kernelIndex, "SourceTexture", source);
        rayMarchingShader.SetTexture(kernelIndex, "RenderTexture", renderTexture);
        rayMarchingShader.SetBuffer(kernelIndex, "LoseBuffer", loseBuffer);
        rayMarchingShader.SetInts("Resolution", renderTexture.width, renderTexture.height);
        rayMarchingShader.SetInt("RayMarchingIterations", iterations);
        rayMarchingShader.SetBool("AlwaysShowFinalColor", alwaysShowFinalColor);
        rayMarchingShader.SetInt("FunctionNum", functionNum);
        rayMarchingShader.SetInt("ColorNum", colorNum);
        rayMarchingShader.SetFloats("CameraPosition", transform.position.x, transform.position.y, transform.position.z);
        rayMarchingShader.SetFloat("Time", lastTime);
        rayMarchingShader.SetFloat("Seed", Random.Range(-randomRange, randomRange));
        rayMarchingShader.SetMatrix("CameraToWorld", mainCamera.cameraToWorldMatrix);
        rayMarchingShader.SetMatrix("CameraInverseProjection", mainCamera.projectionMatrix.inverse);

        rayMarchingShader.Dispatch(kernelIndex, renderTexture.width / 8, renderTexture.height / 8, 1);

        float[] data = new float[1];
        loseBuffer.GetData(data);
        if (data[0] == 1)
        {
            //Debug.Log("Lost");
            //SceneManager.LoadScene(0);
        }
        loseBuffer.Dispose();

        if (saveNextFrame)
        {
            if (saveFrames != 1)
            {
                saveFrames++;
                // Write prevous frame to screen
                Graphics.Blit(renderTexture, destination);
                return;
            }
            saveFrames = 0;
            saveNextFrame = false;
            Texture2D tex = toTexture2D(photoRenderTexture);
            byte[] bytes = tex.EncodeToPNG();
            var dirPath = Application.dataPath + "/../SaveImages/";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            string path = dirPath + frameName + ".png";
            File.WriteAllBytes(path, bytes);

            // Write prevous frame to screen
            Graphics.Blit(renderTexture, destination);
        }
        else
        {
            // Copy contents from new render texture to camera destination
            Graphics.Blit(renderTexture, destination);
        }
    }
}