using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneLoaderUI : MonoBehaviour
{
    public static SceneLoaderUI Instance;
    public Image loadingImage;
    public Material fadeMaterial;
    private bool isAnimating = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (loadingImage == null)
        {
            Debug.LogError("Loading Image not assigned!");
            return;
        }
        if (fadeMaterial == null)
        {
            Debug.LogError("Fade Material not assigned!");
            return;
        }
        loadingImage.material = fadeMaterial;
        fadeMaterial.SetFloat("_TransitionProgress", 0f); // Start with original image
        fadeMaterial.SetFloat("_BlurRadius", 0f); // No blur initially
        fadeMaterial.SetFloat("_MistIntensity", 0f); // No mist initially
        loadingImage.color = new Color(1, 1, 1, 1); // Fully visible
    }

    public void StartLoadingAnimation(System.Action onComplete)
    {
        if (!isAnimating)
        {
            StartCoroutine(AnimateLoading(onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    public void StartUnloadingAnimation(System.Action onComplete)
    {
        if (!isAnimating)
        {
            StartCoroutine(AnimateUnloading(onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private IEnumerator AnimateLoading(System.Action onComplete)
    {
        isAnimating = true;
        float duration = 1.5f; // Total duration
        float fastDuration = duration * 0.33f; // First 1/3 of duration for faster start
        float slowDuration = duration - fastDuration; // Remaining 2/3

        float startProgress = 0f;
        float midProgress = 0.5f; // Halfway point
        float endProgress = 1f;

        // Fast initial fill (2x speed)
        for (float t = 0; t < fastDuration; t += Time.deltaTime)
        {
            float progress = t / fastDuration;
            float currentProgress = Mathf.Lerp(startProgress, midProgress, progress);
            fadeMaterial.SetFloat("_TransitionProgress", currentProgress);
            float blur = Mathf.Lerp(0f, 0.02f, progress * 2); // Faster blur increase
            float mist = Mathf.Lerp(0f, 0.3f, progress * 2); // Faster mist increase
            fadeMaterial.SetFloat("_BlurRadius", blur);
            fadeMaterial.SetFloat("_MistIntensity", mist);
            yield return null;
        }

        // Slower fill to complete
        for (float t = 0; t < slowDuration; t += Time.deltaTime)
        {
            float progress = t / slowDuration;
            float currentProgress = Mathf.Lerp(midProgress, endProgress, progress);
            fadeMaterial.SetFloat("_TransitionProgress", currentProgress);
            float blur = Mathf.Lerp(0.02f, 0.02f, progress); // Maintain blur
            float mist = Mathf.Lerp(0.3f, 0.3f, progress); // Maintain mist
            fadeMaterial.SetFloat("_BlurRadius", blur);
            fadeMaterial.SetFloat("_MistIntensity", mist);
            yield return null;
        }

        fadeMaterial.SetFloat("_TransitionProgress", 1f);
        fadeMaterial.SetFloat("_BlurRadius", 0.02f);
        fadeMaterial.SetFloat("_MistIntensity", 0.3f);
        isAnimating = false;

        // Delay 0.2 seconds before unloading
        yield return new WaitForSeconds(0.2f);
        onComplete?.Invoke();
    }

    private IEnumerator AnimateUnloading(System.Action onComplete)
    {
        isAnimating = true;
        float duration = 1.5f;
        float startProgress = 1f;
        float endProgress = 0f;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;
            float currentProgress = Mathf.Lerp(startProgress, endProgress, progress);
            fadeMaterial.SetFloat("_TransitionProgress", currentProgress);
            float blur = Mathf.Lerp(0.02f, 0f, progress);
            float mist = Mathf.Lerp(0.3f, 0f, progress);
            fadeMaterial.SetFloat("_BlurRadius", blur);
            fadeMaterial.SetFloat("_MistIntensity", mist);
            yield return null;
        }
        fadeMaterial.SetFloat("_TransitionProgress", 0f);
        fadeMaterial.SetFloat("_BlurRadius", 0f);
        fadeMaterial.SetFloat("_MistIntensity", 0f);
        isAnimating = false;
        onComplete?.Invoke();
    }
}