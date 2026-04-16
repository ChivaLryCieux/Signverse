using UnityEngine;

// Legacy component kept for prefab compatibility.
public class SkillUI : MonoBehaviour
{
    public GameObject highLight;
    public float pickScale = 1.1f;

    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (highLight != null)
        {
            highLight.SetActive(false);
        }

        transform.localScale = baseScale;
    }

    private void OnMouseEnter()
    {
        if (highLight != null)
        {
            highLight.SetActive(true);
        }
    }

    private void OnMouseExit()
    {
        if (highLight != null)
        {
            highLight.SetActive(false);
        }

        transform.localScale = baseScale;
    }

    private void OnMouseDown()
    {
        transform.localScale = baseScale * pickScale;
    }
}
