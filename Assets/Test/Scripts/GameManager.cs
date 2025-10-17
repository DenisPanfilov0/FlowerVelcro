using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("--------------UI----------------")]
    public GameObject LosePanel;
    public GameObject SettingsPanel;
    public TextMeshProUGUI ScoreText, ReplayScoreText;
    public Image SoundBtn, MusicBtn;
    public Sprite soundOn, soundOff, musicOn, musicOff;
    public Image NextFruitUI;
    public Sprite[] fruitsUI;

    [Header("------------------------------")]
    public Transform AimLine;
    public float YSpawnPosition, TimeBetweenSpawnes;

    [Header("-------------Fruit System-------------")]
    public Fruit FruitPrefab; // Один универсальный префаб
    public List<FruitData> fruitDataList; // Список типов и спрайтов

    public AudioSource SoundAudioSource, MusicAudioSource;
    public AudioClip loseSound, SmallMergeSound, BigMergeSound, ReleaseSound;
    public int Score;
    public int currentFruitIndex, nextFruitIndex;
    Camera maincamera;
    Vector3 SpawnLoc;
    Fruit currentFruit;
    float lastSpawnTime, LastWink;
    [HideInInspector] public bool IsGameOver;

    void Start()
    {
        maincamera = Camera.main;
        currentFruitIndex = UnityEngine.Random.Range(0, fruitDataList.Count);
        nextFruitIndex = UnityEngine.Random.Range(0, fruitDataList.Count);

        NextFruitUI.sprite = fruitDataList[nextFruitIndex].sprite;
        SetAimLineAndCurentFruit(new Vector3(0, AimLine.position.y, 0));
        SpawnFruit(new Vector3(0, YSpawnPosition, 0));

        if (PlayerPrefs.GetInt("CanPlayMusic", 1) == 0)
        {
            MusicBtn.sprite = musicOff;
            MusicAudioSource.volume = 0;
        }
        if (PlayerPrefs.GetInt("CanPlaySounds", 1) == 0)
        {
            SoundBtn.sprite = soundOff;
        }
    }

    private bool IsPointerOverUIObject()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    void Update()
    {
        GameOver();

        if (Time.time > LastWink + 2)
        {
            LastWink = Time.time;
            Fruit[] winkFruits = FindObjectsOfType<Fruit>();
            if (winkFruits.Length > 0)
                winkFruits[UnityEngine.Random.Range(0, winkFruits.Length)].DoWink();
        }

        if (IsPointerOverUIObject()) return;

        if (Time.time > TimeBetweenSpawnes + lastSpawnTime && currentFruit == null)
        {
            lastSpawnTime = Time.time;
            SetAimLineAndCurentFruit(new Vector3(AimLine.position.x, AimLine.position.y, 0));
            SpawnLoc = new Vector2(AimLine.position.x, YSpawnPosition);
            SpawnFruit(SpawnLoc);
        }

        FruitSpawner();
    }

    void FruitSpawner()
    {
        if (Input.GetMouseButton(0) && !IsPointerOverUIObject())
        {
            Vector3 mousePositionWorld = maincamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
            SetAimLineAndCurentFruit(mousePositionWorld);
        }

        if (!IsPointerOverUIObject() && Input.GetMouseButtonUp(0) && !SettingsPanel.activeSelf)
        {
            if (currentFruit)
            {
                lastSpawnTime = Time.time;
                currentFruit.Release();
                PlayClip(ReleaseSound);
                currentFruit = null;
                AimLine.gameObject.SetActive(false);
            }
        }
    }

    void GameOver()
    {
        if (IsGameOver && !LosePanel.activeSelf)
        {
            Fruit[] loseFruits = FindObjectsOfType<Fruit>();
            foreach (var f in loseFruits)
                f.GameOver();

            Invoke(nameof(ShowLosePanel), 1.5f);
        }
    }

    public void SetAimLineAndCurentFruit(Vector2 newLoc)
    {
        AimLine.gameObject.SetActive(true);
        float Xpos = newLoc.x;

        float radius = fruitDataList[currentFruitIndex].radius;
        if (Xpos > 3.7f - radius)
        {
            Xpos = 3.7f - radius;
        }
        else if (Xpos < -3.7f + radius)
        {
            Xpos = -3.7f + radius;
        }

        AimLine.position = new Vector3(Xpos, 2.56f, 0);
        if (currentFruit) currentFruit.transform.position = new Vector3(Xpos, YSpawnPosition, 0);
    }

    private void SetScore(int scoreIncrement)
    {
        Score += SumNumbers(scoreIncrement);
        ScoreText.text = Score.ToString();
    }

    void SpawnFruit(Vector3 spawnLoc)
    {
        currentFruitIndex = nextFruitIndex;
        nextFruitIndex = UnityEngine.Random.Range(0, fruitDataList.Count);
        NextFruitUI.sprite = fruitDataList[nextFruitIndex].sprite;

        var fruitInfo = fruitDataList[currentFruitIndex];
        currentFruit = Instantiate(FruitPrefab, spawnLoc, quaternion.identity);
        currentFruit.Setup(fruitInfo.type, fruitInfo.sprite, fruitInfo.radius);
        currentFruit.Initialize();
    }

    public void MergeFruit(Fruit f1, Fruit f2)
    {
        if (f1 && f2)
        {
            int n = (int)f1.MyType + 1;
            if (n < fruitDataList.Count)
            {
                PlayClip(n < 5 ? SmallMergeSound : BigMergeSound);
                SetScore(n);

                Vector3 position = (f1.transform.position + f2.transform.position) / 2;
                Destroy(f1.gameObject);
                Destroy(f2.gameObject);

                var newInfo = fruitDataList[n];
                Fruit newFruit = Instantiate(FruitPrefab, position, quaternion.identity);
                newFruit.Setup(newInfo.type, newInfo.sprite, newInfo.radius);
                newFruit.Release();
            }
        }
    }

    int SumNumbers(int n)
    {
        int sum = 0;
        for (int i = 1; i <= n; i++) sum += i;
        return sum;
    }

    public void PlayClip(AudioClip clip)
    {
        if (PlayerPrefs.GetInt("CanPlaySounds", 1) == 1)
            SoundAudioSource.PlayOneShot(clip);
    }

    void ShowLosePanel()
    {
        if (!LosePanel.activeSelf)
        {
            ReplayScoreText.text = Score.ToString();
            PlayClip(loseSound);
            LosePanel.SetActive(true);
        }
    }

    public void ReplayLevelBtn()
    {
        Advertisements.Instance.ShowInterstitial();
        SceneManager.LoadScene(0);
    }

    public void ShowSettingsPanel() => SettingsPanel.SetActive(true);
    public void HideSettingsPanel() => Invoke(nameof(DelayedHideSettingsPanel), 0.2f);
    void DelayedHideSettingsPanel() => SettingsPanel.SetActive(false);

    public void SoundButton()
    {
        bool isOn = SoundBtn.sprite == soundOn;
        SoundBtn.sprite = isOn ? soundOff : soundOn;
        PlayerPrefs.SetInt("CanPlaySounds", isOn ? 0 : 1);
    }

    public void MusicButton()
    {
        bool isOn = MusicBtn.sprite == musicOn;
        MusicBtn.sprite = isOn ? musicOff : musicOn;
        PlayerPrefs.SetInt("CanPlayMusic", isOn ? 0 : 1);
        MusicAudioSource.volume = isOn ? 0 : 0.4f;
    }
}

[System.Serializable]
public class FruitData
{
    public Fruit.FruitType type;
    public Sprite sprite;
    public float radius = 0.25f;
}

public enum FruitType
{
    Apple,
    Orange,
    Lemon,
    Watermelon,
    Pineapple,
    Strawberry,
    Banana
}
