using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHandler : MonoBehaviour
{
    [SerializeField] bool doStart;

    [SerializeField] float startDelayLength;
    [SerializeField] float startInLength;
    [SerializeField] float startHoldLength;
    [SerializeField] float startEndLength;

    [SerializeField] float infoFadeInOut;
    [SerializeField] float infoHold;

    [SerializeField] Image overlayImage;
    [SerializeField] Image speakerImage;
    [SerializeField] TextMeshProUGUI infoText;

    [SerializeField] int startLevel;
    [SerializeField] Level[] levels;


    private int currentLevel = 0;
    private Collider target;

    public static GameHandler Instance;


    [System.Serializable]
    struct Level
    {
        public string title;
        public Color titleColor;
        public Transform cameraEmpty;
        public Collider target;
        public float time;
        public float timeSpeed;
    }

    public void HitTarget(Collider other)
    {
        if (other == levels[currentLevel].target)
        {
            LoadLevel(currentLevel + 1);
        }
    }

    void Awake()
    {
        Instance = this;
    }

    void CleanupLevel(int num)
    {
        Level level = levels[num];
        target = level.target;
        target.gameObject.SetActive(false);
    }

    void LoadLevel(int num, bool title = true)
    {
        currentLevel = num;
        Level level = levels[num];
        target = level.target;
        if (num != 5)
        {
            target.gameObject.SetActive(true);
        }
        Camera.main.transform.position = level.cameraEmpty.position;
        ShaderHandler.Instance.SetRotation((Vector2)level.cameraEmpty.localEulerAngles);
        ShaderHandler.Instance.SetLevelFunction(num);
        ShaderHandler.Instance.SetTime(level.time);
        ShaderHandler.Instance.SetTimeSpeed(level.timeSpeed);
        if (title)
        {
            infoText.color = level.titleColor;
            StartCoroutine(TitleFade(level.title));
        }
        if (num != 0)
        {
            CleanupLevel(num - 1);
        }
    }

    public void ReloadLevel()
    {
        LoadLevel(currentLevel, false);
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("start");
        foreach (Level level in levels)
        {
            level.target.gameObject.SetActive(false);
        }

        currentLevel = startLevel;
        if (doStart)
        {
            StartCoroutine(StartFade());
        }
        else
        {
            SetAlpha(overlayImage, 0f);
            LoadLevel(currentLevel);
        }
    }

    // Update is called once per frame
    void Update()
    {
        /*if (Random.Range(0f, 1f) < 0.001) {
            LoadLevel(currentLevel+1);
        }*/
        //LoadLevel(1);
        if (Input.GetKey(KeyCode.M)) { LoadLevel(currentLevel); }
    }

    void SetAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    IEnumerator StartFade()
    {
        infoText.alpha = 0f;
        yield return new WaitForSeconds(startDelayLength);
        float start = Time.time;
        float elapsed = 0f;
        while (elapsed <= startInLength)
        {
            elapsed = Time.time - start;
            float alpha = elapsed / startInLength;
            SetAlpha(speakerImage, alpha);
            yield return null;
        }
        yield return new WaitForSeconds(startHoldLength);
        LoadLevel(currentLevel);
        start = Time.time;
        elapsed = 0f;
        while (elapsed <= startEndLength)
        {
            elapsed = Time.time - start;
            float alpha = 1f - elapsed / startEndLength;
            SetAlpha(overlayImage, alpha);
            SetAlpha(speakerImage, alpha);
            yield return null;
        }
    }

    IEnumerator TitleFade(string title)
    {
        yield return new WaitForSeconds(1f);
        infoText.text = title;
        float start = Time.time;
        float elapsed = 0f;
        while (elapsed <= infoFadeInOut)
        {
            elapsed = Time.time - start;
            infoText.alpha = elapsed / infoFadeInOut;
            yield return null;
        }
        yield return new WaitForSeconds(infoHold);
        start = Time.time;
        elapsed = 0f;
        while (elapsed <= infoFadeInOut)
        {
            elapsed = Time.time - start;
            infoText.alpha = 1f - elapsed / infoFadeInOut;
            yield return null;
        }
    }
}
