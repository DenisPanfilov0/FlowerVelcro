using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Code.Inventory;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using PlayerPrefs = RedefineYG.PlayerPrefs;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

[System.Serializable]
public class FruitSaveData
{
    public Fruit.FruitType type;
    public Vector3 position;
    public Quaternion rotation;
    public Vector2 velocity;
    public float angularVelocity;
    public bool isKinematic;
    public bool gravityEnabled;
    public Fruit.FruitStatus status;
}

[System.Serializable]
public class FruitSaveWrapper
{
    public List<FruitSaveData> fruits;
    public int savedScore;
    public int maxFruitProgress; // сохраняем максимальный фрукт для UI
}

[System.Serializable]
public class FruitData
{
    public Fruit.FruitType type;
    public Sprite sprite;
    public float radius = 0.25f;
}

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
    public Fruit FruitPrefab;
    public List<FruitData> fruitDataList;

    [Header("----------Fruit Progress UI----------")]
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private List<Image> fruitProgressImages;

    public AudioSource SoundAudioSource, MusicAudioSource;
    public AudioClip loseSound, SmallMergeSound, BigMergeSound, ReleaseSound;

    public int Score;
    public int currentFruitIndex, nextFruitIndex;
    private int maxFruitProgress = 0; // хранение текущего прогресса фруктов

    private Camera maincamera;
    private Vector3 SpawnLoc;
    private Fruit currentFruit;
    private float lastSpawnTime, LastWink;
    [HideInInspector] public bool IsGameOver;

    [SerializeField] private TMP_Text _currencyValue;
    [SerializeField] private TMP_Text _currencyValueEndGame;

    private CurrencyModel _currencyModel;

    [Inject]
    public void Construct(CurrencyModel currencyModel)
    {
        _currencyModel = currencyModel;
    }

    void Start()
    {
        maincamera = Camera.main;
        _currencyValue.text = _currencyModel.GetCurrencyAmount().ToString();
        _currencyValueEndGame.text = _currencyModel.GetCurrencyAmount().ToString();

        InitFruitProgressUI();

        if (PlayerPrefs.HasKey("SavedFruits"))
            LoadFruits();
        else
            StartNewSession();

        if (PlayerPrefs.GetInt("CanPlayMusic", 1) == 0)
        {
            MusicBtn.sprite = musicOff;
            MusicAudioSource.volume = 0;
        }
        if (PlayerPrefs.GetInt("CanPlaySounds", 1) == 0)
        {
            SoundBtn.sprite = soundOff;
        }

        InvokeRepeating(nameof(SaveFruits), 5f, 5f);
        _currencyModel.AmountChanged += CurrencyChange;
    }

    private void OnDestroy()
    {
        _currencyModel.AmountChanged -= CurrencyChange;
    }

    private void CurrencyChange(int value)
    {
        _currencyValue.text = value.ToString();
        _currencyValueEndGame.text = value.ToString();
    }

    private void InitFruitProgressUI()
    {
        foreach (var img in fruitProgressImages)
        {
            img.color = lockedColor;
        }
    }

    private void StartNewSession()
    {
        currentFruitIndex = GetWeightedRandomFruitIndex();
        nextFruitIndex = GetWeightedRandomFruitIndex();
        NextFruitUI.sprite = fruitDataList[nextFruitIndex].sprite;
        SetAimLineAndCurentFruit(new Vector3(0, AimLine.position.y, 0));
        SpawnFruit(new Vector3(0, YSpawnPosition, 0));

        Score = 0;
        ScoreText.text = "0";
        maxFruitProgress = 0;

        // Первый фрукт сразу доступен
        UnlockFruitProgress(0);
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

    void OnApplicationQuit() => SaveFruits();
    void OnApplicationPause(bool pause) { if (pause) SaveFruits(); }

    void SaveFruits()
    {
        if (IsGameOver) return;
        Fruit[] fruits = FindObjectsOfType<Fruit>();
        List<FruitSaveData> saveList = new();

        foreach (var f in fruits)
        {
            if (f.MyRigidbody2D != null && !f.MyRigidbody2D.simulated)
                continue;

            Rigidbody2D rb = f.MyRigidbody2D;
            saveList.Add(new FruitSaveData
            {
                type = f.MyType,
                position = f.transform.position,
                rotation = f.transform.rotation,
                velocity = rb.linearVelocity,
                angularVelocity = rb.angularVelocity,
                isKinematic = rb.isKinematic,
                gravityEnabled = rb.simulated,
                status = f.MyStatus
            });
        }

        FruitSaveWrapper wrapper = new()
        {
            fruits = saveList,
            savedScore = Score,
            maxFruitProgress = maxFruitProgress
        };

        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString("SavedFruits", json);
        PlayerPrefs.Save();
    }

    void LoadFruits()
    {
        string json = PlayerPrefs.GetString("SavedFruits");
        FruitSaveWrapper wrapper = JsonUtility.FromJson<FruitSaveWrapper>(json);
        if (wrapper == null || wrapper.fruits == null || wrapper.fruits.Count == 0)
        {
            StartNewSession();
            return;
        }

        foreach (FruitSaveData data in wrapper.fruits)
        {
            var fruitInfo = fruitDataList.FirstOrDefault(f => f.type == data.type);
            if (fruitInfo == null) continue;

            Fruit fruit = Instantiate(FruitPrefab, data.position, data.rotation);
            fruit.Setup(fruitInfo.type, fruitInfo.sprite, fruitInfo.radius, data.status);
            fruit.Initialize();

            Rigidbody2D rb = fruit.MyRigidbody2D;
            rb.linearVelocity = data.velocity;
            rb.angularVelocity = data.angularVelocity;
            rb.isKinematic = data.isKinematic;
            rb.simulated = data.gravityEnabled;
        }

        Score = wrapper.savedScore;
        maxFruitProgress = wrapper.maxFruitProgress;
        ScoreText.text = Score.ToString();

        // Восстанавливаем прогресс фруктов
        for (int i = 0; i < maxFruitProgress; i++)
            UnlockFruitProgress(i);
    }

    private void UnlockFruitProgress(int index)
    {
        if (index >= 0 && index < fruitProgressImages.Count && fruitProgressImages[index].color != Color.white)
            fruitProgressImages[index].color = Color.white;
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

    void SpawnFruit(Vector3 spawnLoc)
    {
        currentFruitIndex = nextFruitIndex;
        nextFruitIndex = GetWeightedRandomFruitIndex();
        NextFruitUI.sprite = fruitDataList[nextFruitIndex].sprite;

        var fruitInfo = fruitDataList[currentFruitIndex];
        bool isStar = UnityEngine.Random.value <= 0.03f;
        var status = isStar ? Fruit.FruitStatus.Star : Fruit.FruitStatus.Normal;

        currentFruit = Instantiate(FruitPrefab, spawnLoc, quaternion.identity);
        currentFruit.Setup(fruitInfo.type, fruitInfo.sprite, fruitInfo.radius, status);
        currentFruit.Initialize();
        currentFruit.MyRigidbody2D.simulated = false;
    }

    int GetWeightedRandomFruitIndex()
    {
        int maxPossibleIndex = fruitDataList.Count - 1;
        int maxIndex = 0;

        if (Score < 30)
            maxIndex = 0;
        else if (Score < 60)
            maxIndex = Mathf.Min(1, maxPossibleIndex);
        else if (Score < 100)
            maxIndex = Mathf.Min(2, maxPossibleIndex);
        else
            maxIndex = Mathf.Min(3, maxPossibleIndex);

        float[] weights = new float[maxIndex + 1];
        for (int i = 0; i <= maxIndex; i++)
            weights[i] = 1f / Mathf.Pow(i + 1f, 2f);

        float totalWeight = weights.Sum();
        float randomValue = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i <= maxIndex; i++)
        {
            cumulative += weights[i];
            if (randomValue <= cumulative)
                return i;
        }

        return maxIndex;
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

                int starCount = 0;
                if (f1.MyStatus == Fruit.FruitStatus.Star) starCount++;
                if (f2.MyStatus == Fruit.FruitStatus.Star) starCount++;
                if (starCount > 0)
                    _currencyModel.AddStarCurrency(starCount);
                _currencyModel.AddCurrency(3);

                Vector3 position = (f1.transform.position + f2.transform.position) / 2;
                Destroy(f1.gameObject);
                Destroy(f2.gameObject);

                var newInfo = fruitDataList[n];
                Fruit newFruit = Instantiate(FruitPrefab, position, quaternion.identity);
                newFruit.Setup(newInfo.type, newInfo.sprite, newInfo.radius);
                newFruit.Initialize();
                newFruit.Release();

                // обновляем прогресс только если новый фрукт выше текущего прогресса
                if (n >= maxFruitProgress)
                {
                    maxFruitProgress = n + 1;
                    UnlockFruitProgress(n);
                }
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

            Score = 0;
            PlayerPrefs.DeleteKey("SavedFruits");
            PlayerPrefs.Save();

            Invoke(nameof(ShowLosePanel), 1.5f);
        }
    }

    private void SetScore(int scoreIncrement)
    {
        Score += SumNumbers(scoreIncrement);
        ScoreText.text = Score.ToString();
    }

    int SumNumbers(int n)
    {
        int sum = 0;
        for (int i = 1; i <= n; i++) sum += i;
        return sum;
    }

    private bool IsPointerOverUIObject()
    {
        PointerEventData eventData = new(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
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
        PlayerPrefs.DeleteKey("SavedFruits");
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

    public void SetAimLineAndCurentFruit(Vector2 newLoc)
    {
        AimLine.gameObject.SetActive(true);
        float Xpos = Mathf.Clamp(newLoc.x, -3.7f + fruitDataList[currentFruitIndex].radius, 3.7f - fruitDataList[currentFruitIndex].radius);
        AimLine.position = new Vector3(Xpos, 1.56f, 0);
        if (currentFruit)
            currentFruit.transform.position = new Vector3(Xpos, YSpawnPosition, 0);
    }
}
