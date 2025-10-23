using UnityEngine;

public class Fruit : MonoBehaviour
{
    public enum FruitType
    {
        Cherry = 0,
        Strawberry = 1,
        Grape = 2,
        Nasi = 3,
        Orange = 4,
        Apple = 5,
        Pear = 6,
        Peach = 7,
        Pineapple = 8,
        Melon = 9,
        WaterMelon = 10,
        Fruit1 = 11,
        Fruit2 = 12,
        Fruit3 = 13,
    }

    public enum FruitStatus
    {
        Normal,
        Star
    }

    public bool bActive = true;
    public FruitType MyType;
    public FruitStatus MyStatus = FruitStatus.Normal;

    [HideInInspector] public Rigidbody2D MyRigidbody2D;
    [HideInInspector] public CapsuleCollider2D MyCollider;
    [HideInInspector] public GameManager MyGM;
    [HideInInspector] public float raduis;

    public SpriteRenderer MySpriteRenderer;
    public Sprite NormalSprite;

    [SerializeField] private GameObject StarEffect;

    private void Awake()
    {
        MyRigidbody2D = GetComponent<Rigidbody2D>();
        MyCollider = GetComponent<CapsuleCollider2D>();
        MyGM = FindObjectOfType<GameManager>();
    }

    public void Setup(FruitType fruitInfoType, Sprite fruitInfoSprite, float fruitInfoRadius, FruitStatus status = FruitStatus.Normal)
    {
        MyType = fruitInfoType;
        MyStatus = status;
        MySpriteRenderer.sprite = fruitInfoSprite;
        MySpriteRenderer.sortingOrder = 10;
        raduis = fruitInfoRadius;

        if (StarEffect != null)
            StarEffect.SetActive(MyStatus == FruitStatus.Star);
    }

    public void Initialize()
    {
        MySpriteRenderer.gameObject.GetComponent<Animator>().enabled = true;
    }

    public void DoWink()
    {
        Invoke("StopWink", .3f);
    }

    public void Release()
    {
        MyRigidbody2D.simulated = true;
        bActive = true;
    }

    public void GameOver() { }

    private void StopWink() { }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.TryGetComponent<Fruit>(out Fruit fruit))
        {
            if (fruit.MyType == MyType && bActive)
            {
                bActive = false;
                fruit.bActive = false;
                MyGM.MergeFruit(this, fruit);
            }
        }
    }
}
