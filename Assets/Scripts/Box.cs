using UnityEngine;
using System.Collections.Generic;

public class Box : MonoBehaviour {

    #region delegate and event fields

    public delegate void DeleteAction(Box box);
    public event DeleteAction OnDelete;

    #endregion

    #region public fields

    public Vector3 TargetPosition = Vector3.zero;

    #endregion

    #region private fields

    private Rigidbody2D m_Rigidbody;
    private SpriteRenderer sprite;

    #endregion

    #region properties fileds

    public Rigidbody2D Rigidbody {
        get {
            return m_Rigidbody;
        }
    }

    public Vector2 Size
    {
        get
        {
            return sprite.bounds.size;
        }
    }

    #endregion

    #region private methods

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    private void DeleteBox(Box box) {
        if (OnDelete != null)
        {
            OnDelete(box);
        }

        Destroy(gameObject);
    }

    #endregion

    #region handlers

    void OnMouseDown()
    {
        DeleteBox(this);
    }

    #endregion
}

public class BoxComparer : IComparer<Box>
{
    #region public methods

    public int Compare(Box x, Box y)
    {
        if (x == null)
        {
            if (y == null)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
        else
        {
            if (y == null)
            {
                return 1;
            }
            else
            {
                float sizeX = x.transform.localScale.x;
                float sizeY = y.transform.localScale.x;

                if (sizeX > sizeY)
                {
                    return 1;
                }
                else if (sizeX < sizeY)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }
    }

    #endregion
}

