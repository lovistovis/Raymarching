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
    [SerializeField] private float baseMoveSpeed;
    [FormerlySerializedAs("rayMarchingIterations")][SerializeField][Range(1, 10000)] private int baseRayMarchingIterations;
    [SerializeField][Range(0, 2)] private float mouseSensitivity;
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
    private float timeSpeed = 0.1f;
    private bool moveTime = true;
    private bool saveNextFrame = false;
    private float moveSpeed;
    private int iterations;
    private float randomRange = 1000f;
    private float lastTime = 0;
    private int functionNum = 0;

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

    public void SetLevelFunction(int num)
    {
        functionNum = num;
    }

    public void SetTimeSpeed(float speed)
    {
        timeSpeed = speed;
    }

    public void SetTime(float time)
    {
        lastTime = time;
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
        iterations = baseRayMarchingIterations;

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

        transform.localEulerAngles = new Vector3(cameraRotationY, cameraRotationX, 0);

        //Debug.Log(functionNum);

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
        transform.position += (transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical")) * currentMoveSpeed;

        //if (!Input.GetKey(KeyCode.LeftControl)) { lastTime += Time.deltaTime; }
        //else if (Input.GetKey(KeyCode.LeftAlt)) { lastTime -= Time.deltaTime; }
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
            mainCamera.fieldOfView = 60;
            moveSpeed = baseMoveSpeed;
            timeSpeed = 1f;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            mainCamera.fieldOfView = 60;
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            moveTime = !moveTime;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            timeSpeed = timeSpeed * -1;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            lastTime = 0;
        }

        if (Input.GetKey(KeyCode.M)) { transform.position = startPos; }

        if (Input.GetKey(KeyCode.O))
        {
            saveNextFrame = true;
        }

        //Debug.Log((Time.timeSinceLevelLoad * 100).ToString() + ", " + iterations.ToString());
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        int kernelIndex = rayMarchingShader.FindKernel("CSMain");

        ComputeBuffer loseBuffer = new ComputeBuffer(1, sizeof(float));
        loseBuffer.SetData(new float[] { 0 });

        renderTexture = saveNextFrame ? photoRenderTexture : renderTexture;
        Debug.Log(renderTexture.width + ";" + renderTexture.height);

        Vector3 position = transform.position / 0.001f;
        rayMarchingShader.SetTexture(kernelIndex, "SourceTexture", source);
        rayMarchingShader.SetTexture(kernelIndex, "RenderTexture", renderTexture);
        rayMarchingShader.SetBuffer(kernelIndex, "LoseBuffer", loseBuffer);
        rayMarchingShader.SetInts("Resolution", renderTexture.width, renderTexture.height);
        rayMarchingShader.SetInt("RayMarchingIterations", iterations);
        rayMarchingShader.SetInt("FunctionNum", functionNum);
        rayMarchingShader.SetFloats("CameraPosition", position.x, position.y, position.z);
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
            saveNextFrame = false;
            Texture2D tex = toTexture2D(photoRenderTexture);
            byte[] bytes = tex.EncodeToPNG();
            var dirPath = Application.dataPath + "/../SaveImages/";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            string path = dirPath + "T" + lastTime + "_P" + position.x + ";" + position.y + ";" + position.y + "_F" + mainCamera.fieldOfView + "_R" + cameraRotationX + ";" + cameraRotationY + ".png";
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