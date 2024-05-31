using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
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

    [SerializeField] int startLevel;
    [SerializeField] Level[] levels;
    
    [SerializeField] TextMeshProUGUI infoText;

    private int currentLevel = 0;
    private Collider target;

    public static GameManager Instance;

    [System.Serializable]
    struct Level {
        public string title;
        public Transform cameraEmpty;
        public Collider target;
        public float time;
        public float timeSpeed;
    }

    void Awake() {
        Instance = this;
    }

    void LoadLevel(int num) {
        currentLevel = num;
        Level level = levels[num];
        target = level.target;
        target.enabled = true;
        Camera.main.transform.position = level.cameraEmpty.position;
        Camera.main.transform.rotation = level.cameraEmpty.rotation;
        Debug.Log(num);
       
        ShaderHandler.Instance.SetLevelFunction(num);
        ShaderHandler.Instance.SetTime(level.time);
        ShaderHandler.Instance.SetTimeSpeed(level.timeSpeed);
        StartCoroutine(TitleFade(level.title));
    }

    // Start is called before the first frame update
    void Start() {
        Debug.Log("start");
        currentLevel = startLevel;
        if (doStart) {
            StartCoroutine(StartFade());
        } else {
            SetAlpha(overlayImage, 0f);
            LoadLevel(currentLevel);
        }

        foreach (Level level in levels) {
            level.target.enabled = false;
        }
    }

    // Update is called once per frame
    void Update() {
        /*if (Random.Range(0f, 1f) < 0.001) {
            LoadLevel(currentLevel+1);
        }*/
    }

    void SetAlpha(Image image, float alpha) {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    IEnumerator StartFade() {
        infoText.alpha = 0f;
        yield return new WaitForSeconds(startDelayLength);
        float start = Time.time;
        float elapsed = 0f;
        while (elapsed <= startInLength) {
            elapsed = Time.time - start;
            float alpha = elapsed / startInLength;
            SetAlpha(speakerImage, alpha);
            yield return null;
        }
        yield return new WaitForSeconds(startHoldLength);
        LoadLevel(currentLevel);
        start = Time.time;
        elapsed = 0f;
        while (elapsed <= startEndLength) {
            elapsed = Time.time - start;
            float alpha = 1f - elapsed / startEndLength;
            SetAlpha(overlayImage, alpha);
            SetAlpha(speakerImage, alpha);
            yield return null;
        }
    }

    IEnumerator TitleFade(string title) {
        yield return new WaitForSeconds(1f);
        infoText.text = title;
        float start = Time.time;
        float elapsed = 0f;
        while (elapsed <= infoFadeInOut) {
            elapsed = Time.time - start;
            infoText.alpha = elapsed / infoFadeInOut;
            yield return null;
        }
        yield return new WaitForSeconds(infoHold);
        start = Time.time;
        elapsed = 0f;
        while (elapsed <= infoFadeInOut) {
            elapsed = Time.time - start;
            infoText.alpha = 1f - elapsed / infoFadeInOut;
            yield return null;
        }
    }
}
