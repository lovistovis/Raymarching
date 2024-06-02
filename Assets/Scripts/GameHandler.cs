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

    [SerializeField] float titleFadeInOut;
    [SerializeField] float titleHold;
    [SerializeField] float fullResetRepeatTime;

    [SerializeField] Image overlayImage;
    [SerializeField] Image speakerImage;
    [SerializeField] TextMeshProUGUI[] hotkeyTexts;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] AudioSource highNoiseAudioSource;
    [SerializeField] AudioSource exitAudioSource;

    [SerializeField] int startLevel;
    [SerializeField] Level[] levels;

    private float startTime;
    private int currentLevel = 0;
    private Collider target;
    private bool fullReset = false;
    private IEnumerator titleFadeCoroutine;

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
        if (num == 8)
        {
            SetAlpha(overlayImage, 0f);
        }
    }

    void LoadLevel(int num, bool showTitle = true)
    {
        int pastLevel = currentLevel;
        currentLevel = num;
        Level level = levels[num];
        target = level.target;
        target.gameObject.SetActive(true);
        Camera.main.transform.position = level.cameraEmpty.position;
        ShaderHandler.Instance.SetRotation((Vector2)level.cameraEmpty.localEulerAngles);
        ShaderHandler.Instance.SetLevelFunction(num);
        ShaderHandler.Instance.SetTime(level.time);
        ShaderHandler.Instance.SetTimeSpeed(level.timeSpeed);
        if (pastLevel != currentLevel)
        {
            CleanupLevel(pastLevel);
            if (titleFadeCoroutine != null)
            {
                StopTitleFade();
            }
        }
        if (showTitle)
        {
            titleText.color = level.titleColor;
            titleFadeCoroutine = TitleFade(level.title);
            StartCoroutine(titleFadeCoroutine);
        }
        if (num == 0)
        {
            startTime = Time.time;
        }
        if (num == 8)
        {
            StartCoroutine(ExitSequence());
        }
    }

    public void ReloadLevel()
    {
        LoadLevel(currentLevel, false);
    }

    // Start is called before the first frame update
    void Start()
    {
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
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (fullReset || currentLevel == 8)
            {
                LoadLevel(0, false);
            }
            else
            {
                ReloadLevel();
                StartCoroutine(SetFullResetRepeat());
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        if (currentLevel == 7) { return; }
        float dist = (Camera.main.transform.position - levels[currentLevel].target.transform.position).magnitude;
        highNoiseAudioSource.volume = Mathf.Clamp(1.0f / Mathf.Pow(dist, 2.0f), 0.0f, 0.1f);
    }

    void SetAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    void SetHotkeyTextsAlpha(float alpha)
    {
        foreach (TextMeshProUGUI text in hotkeyTexts)
        {
            text.alpha = alpha;
        }
    }

    void StopTitleFade()
    {
        StopCoroutine(titleFadeCoroutine);
        titleText.alpha = 0f;
    }

    IEnumerator StartFade()
    {
        titleText.alpha = 0f;
        yield return new WaitForSeconds(startDelayLength);
        float start = Time.time;
        float elapsed = 0f;
        while (elapsed <= startInLength)
        {
            elapsed = Time.time - start;
            float alpha = elapsed / startInLength;
            SetAlpha(speakerImage, alpha);
            SetHotkeyTextsAlpha(alpha);
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
            SetHotkeyTextsAlpha(alpha);
            yield return null;
        }
    }

    IEnumerator TitleFade(string title)
    {
        yield return new WaitForSeconds(1f);
        titleText.text = title;
        float start = Time.time;
        float elapsed = 0f;
        while (elapsed <= titleFadeInOut)
        {
            elapsed = Time.time - start;
            titleText.alpha = elapsed / titleFadeInOut;
            yield return null;
        }
        yield return new WaitForSeconds(titleHold);
        start = Time.time;
        elapsed = 0f;
        while (elapsed <= titleFadeInOut)
        {
            elapsed = Time.time - start;
            titleText.alpha = 1f - elapsed / titleFadeInOut;
            yield return null;
        }
    }

    IEnumerator ExitSequence()
    {
        // Speedrun ends upon loading final level
        float finishTime = Time.time - startTime;
        yield return new WaitForSeconds(0.1f);
        exitAudioSource.Play();
        yield return new WaitForSeconds(exitAudioSource.clip.length);
        titleText.text = finishTime.ToString("F3");
        SetAlpha(overlayImage, 1f);
        titleText.color = Color.white;
        titleText.alpha = 1f;
        yield return new WaitForSeconds(10f);
        if (currentLevel == 8)
        {
            Application.Quit();
        }

    }

    IEnumerator SetFullResetRepeat()
    {
        fullReset = true;
        yield return new WaitForSeconds(fullResetRepeatTime);
        fullReset = false;
    }
}
