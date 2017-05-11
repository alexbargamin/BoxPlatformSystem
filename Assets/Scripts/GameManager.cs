using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{

    #region constants fields

    private const int AMOUNT_OF_BOXES = 10;

    private const int MOVING_TRESHOLD = 5;

    private const float MIN_SIZE_OF_BOX = 2f;

    private const float MAX_SIZE_OF_BOX = 8f;

    private const float THICKNESS_OF_BORDER = 10f;

    #endregion

    #region private fields

    [SerializeField]
    private float m_CreationInterval = 0.2f;

    [SerializeField]
    private float SpeedTransition = 1f;

    [SerializeField]
    private Transform m_LeftBorder;

    [SerializeField]
    private Transform m_RightBorder;

    [SerializeField]
    private GameObject m_TopBorder;

    [SerializeField]
    private Transform m_Floor;

    [SerializeField]
    private GameObject m_BoxPrefab;

    private List<Box> m_BoxList;

    private SpriteRenderer m_SpriteLeftBorder;
    private SpriteRenderer m_SpriteRightBorder;

    private float m_ScreenWidth = 0f;
    private float m_ScreenHeight = 0f;

    private int m_Index = 0;

    private int m_CountStopedFrame = 0;

    private bool m_IsAlreadySorted = false;

    #endregion

    #region private methods

    private void Awake()
    {
        InitializeGame();
    }

    // Use this for initialization
    private void Start()
    {
        InitializeScreen();
    }

    // Update is called once per frame
    private void Update()
    {
        if (IsBoxesMoving() == true)
        {
            m_CountStopedFrame = 0;
        }
        else
        {
            if (m_CountStopedFrame <= MOVING_TRESHOLD)
            {
                m_CountStopedFrame++;
            }
            else if (m_IsAlreadySorted == false)
            {
                SortBoxes();
                m_IsAlreadySorted = true;
            }
        }
    }

    // Create boxes 
    private void InitializeGame()
    {
        m_BoxList = new List<Box>();
        StartCoroutine(CreateBoxes(m_BoxPrefab, m_BoxList, AMOUNT_OF_BOXES, m_CreationInterval));

        m_Index = AMOUNT_OF_BOXES - 1;
    }

    // Initialize UI
    private void InitializeScreen()
    {
        m_ScreenHeight = Camera.main.orthographicSize * 2;
        m_ScreenWidth = Camera.main.aspect * m_ScreenHeight;

        // Set left border
        m_SpriteLeftBorder = m_LeftBorder.GetComponent<SpriteRenderer>();
        float leftBorderHeight = m_SpriteLeftBorder.bounds.size.y;
        m_LeftBorder.localScale = new Vector3(THICKNESS_OF_BORDER, m_ScreenHeight / leftBorderHeight);
        m_LeftBorder.localPosition = new Vector3(-m_ScreenWidth / 2, 0f, 0f);

        // Set right border
        m_SpriteRightBorder = m_RightBorder.GetComponent<SpriteRenderer>();
        float rightBorderHeight = m_SpriteRightBorder.bounds.size.y;
        m_RightBorder.localScale = new Vector3(THICKNESS_OF_BORDER, m_ScreenHeight / rightBorderHeight);
        m_RightBorder.localPosition = new Vector3(m_ScreenWidth / 2, 0f, 0f);

        // Set floor
        SpriteRenderer spriteFloor = m_Floor.GetComponent<SpriteRenderer>();
        float floorWidth = spriteFloor.bounds.size.x;
        m_Floor.localScale = new Vector3(m_ScreenWidth / floorWidth, THICKNESS_OF_BORDER);
        m_Floor.localPosition = new Vector3(0, -m_ScreenHeight / 2, 0f);

        m_TopBorder.SetActive(false);
    }

    private void InstantiateBox(GameObject box_prefab, List<Box> boxList, float boxSize)
    {
        GameObject boxObject = Instantiate(box_prefab);
        boxObject.transform.localScale = new Vector3(boxSize, boxSize, boxSize);

        Box box = boxObject.GetComponent<Box>();
        if (box == null)
        {
            Debug.LogError("Instantiated object does not have Box component");
        }
        else
        {
            box.OnDelete += OnDelete;
            boxList.Add(box);
        }
    }

    // Sorted boxes and put them into left and right columns
    private void SortBoxes()
    {
        List<Box> m_RightBoxList = new List<Box>();
        List<Box> m_LeftBoxList = new List<Box>();

        BoxComparer boxComparer = new BoxComparer();
        m_BoxList.Sort(boxComparer);

        while (m_Index >= 0)
        {
            Box box = m_BoxList[m_Index];
            box.Rigidbody.simulated = true;
            box.Rigidbody.bodyType = RigidbodyType2D.Kinematic;
            box.transform.eulerAngles = Vector3.zero;

            if (m_Index % 2 != 0)
            {
                if (m_RightBoxList.Count == 0)
                {
                    box.TargetPosition = new Vector3(m_ScreenWidth / 2 - box.Size.x / 2 - m_SpriteRightBorder.bounds.size.x / 2, m_ScreenHeight / 2 - box.Size.y / 2, box.transform.localPosition.z);
                }
                else
                {
                    Box lastBox = m_RightBoxList[m_RightBoxList.Count - 1];
                    box.TargetPosition = new Vector3(m_ScreenWidth / 2 - box.Size.x / 2 - m_SpriteRightBorder.bounds.size.x / 2, lastBox.TargetPosition.y - lastBox.Size.y / 2 - box.Size.y / 2, box.transform.localPosition.z);
                }

                m_RightBoxList.Add(box);
            }
            else
            {
                if (m_LeftBoxList.Count == 0)
                {
                    box.TargetPosition = new Vector3(-m_ScreenWidth / 2 + box.Size.x / 2 + m_SpriteLeftBorder.bounds.size.x / 2, m_ScreenHeight / 2 - box.Size.y / 2, box.transform.localPosition.z);
                }
                else
                {
                    Box lastBox = m_LeftBoxList[m_LeftBoxList.Count - 1];
                    box.TargetPosition = new Vector3(-m_ScreenWidth / 2 + box.Size.x / 2 + m_SpriteLeftBorder.bounds.size.x / 2, lastBox.TargetPosition.y - lastBox.Size.y / 2 - box.Size.y / 2, box.transform.localPosition.z);
                }

                m_LeftBoxList.Add(box);
            }

            if (m_Index == 0)
            {
                StartCoroutine(BoxTransition(box, box.TargetPosition, SpeedTransition, OnSortBoxed));
            }
            else
            {
                StartCoroutine(BoxTransition(box, box.TargetPosition, SpeedTransition));
            }

            m_Index--;
        }
    }

    // Set rigibody of each box in left and right columns
    private void AdjustBoxesBeforeArrange()
    {
        //set top border
        m_TopBorder.SetActive(true);

        SetBoxes(m_BoxList, -1f, RigidbodyConstraints2D.FreezePositionX, RigidbodyType2D.Dynamic);
    }

    // Set rigibody of each box in list
    private void SetBoxes(List<Box> boxList, float gravityScale, RigidbodyConstraints2D constraints, RigidbodyType2D bodyType)
    {
        foreach (Box box in boxList)
        {
            box.Rigidbody.gravityScale = gravityScale;
            box.Rigidbody.constraints = constraints;
            box.Rigidbody.simulated = true;
            box.Rigidbody.bodyType = bodyType;
        }
    }

    // Check if all boxes are stopped or some of them is moving
    private bool IsBoxesMoving()
    {
        bool isMoving = false;
        if (m_BoxList != null)
        {
            foreach (Box box in m_BoxList)
            {
                if (box.Rigidbody.velocity != Vector2.zero)
                {
                    isMoving = true;
                    break;
                }
            }
        }

        return isMoving;
    }

    // Add random force in random direction for each rigidbody of box in list
    private void AddRandomForceForAll(List<Box> boxList)
    {
        float randomValue = 1f;
        Vector3 direction = Vector3.one;
        foreach (Box box in boxList)
        {
            randomValue = UnityEngine.Random.Range(0f, 1f);
            if (randomValue < 0.5f)
            {
                box.Rigidbody.AddForce(box.transform.right * (-1f) * 400f * randomValue);
            }
            else
            {
                box.Rigidbody.AddForce(box.transform.right * 400f * randomValue);
            }
        }
    }

    private void InitializeGameBeforeRearrange()
    {
        StopAllCoroutines();

        m_TopBorder.SetActive(false);

        m_IsAlreadySorted = false;
        m_CountStopedFrame = 0;

        m_Index = AMOUNT_OF_BOXES - 1;

        SetBoxes(m_BoxList, 1f, RigidbodyConstraints2D.None, RigidbodyType2D.Dynamic);
    }

    #endregion

    #region coroutines

    private IEnumerator CreateBoxes(GameObject boxPrefab, List<Box> boxList, int amountOfBoxes, float creationInterval)
    {
        float deltaSize = (MAX_SIZE_OF_BOX - MIN_SIZE_OF_BOX) / (AMOUNT_OF_BOXES - 1);

        float newSize = MIN_SIZE_OF_BOX;

        for (int boxCounter = 0; boxCounter < amountOfBoxes; boxCounter++)
        {
            InstantiateBox(boxPrefab, boxList, newSize);

            newSize += deltaSize;

            yield return new WaitForSeconds(creationInterval);
        }
    }

    private IEnumerator BoxTransition(Box box, Vector3 targetPosition, float speed, UnityAction OnTransitionDone = null)
    {
        float t = 0;
        Vector3 startPosition = box.transform.localPosition;
        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
            box.transform.localPosition = newPosition;

            yield return new WaitForEndOfFrame();
        }

        if (OnTransitionDone != null)
        {
            OnTransitionDone();
        }
    }

    private IEnumerator AdjustBoxes(float waitingTime)
    {
        yield return new WaitForSeconds(waitingTime);

        AdjustBoxesBeforeArrange();
    }

    private IEnumerator MoveBoxToTopCenter(Box box)
    {
        Vector3 targetPosition = new Vector3(0, box.transform.localPosition.y, box.transform.localPosition.z);
        yield return StartCoroutine(BoxTransition(box, targetPosition, SpeedTransition));
        yield return new WaitForEndOfFrame();
        box.Rigidbody.gravityScale = 1f;
    }

    private IEnumerator ArrangeBoxes()
    {
        yield return new WaitForSeconds(0.5f);

        for (int i = m_BoxList.Count - 1; i >= 0; i--) {
                yield return StartCoroutine(MoveBoxToTopCenter(m_BoxList[i]));
        }
    }

    #endregion

    #region handlers

    private void OnSortBoxed()
    {
        AdjustBoxesBeforeArrange();
        StartCoroutine(ArrangeBoxes());
    }

    private void OnDelete(Box box)
    {
        m_BoxList.Remove(box);

        InitializeGameBeforeRearrange();

        AddRandomForceForAll(m_BoxList);

        InstantiateBox(m_BoxPrefab, m_BoxList, UnityEngine.Random.Range(MIN_SIZE_OF_BOX, MAX_SIZE_OF_BOX));
    }

    #endregion

}
