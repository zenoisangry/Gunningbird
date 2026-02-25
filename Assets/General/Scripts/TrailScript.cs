using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class TrailScript : MonoBehaviour
{
    private TrailRenderer m_Renderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_Renderer = GetComponent<TrailRenderer>();
    }

    public void fadeOut(float time)
    {
        StartCoroutine(FadeCoroutine(time));
    }

    IEnumerator FadeCoroutine(float time)
    {
        float startingTime = time;
        float baseScale = m_Renderer.widthMultiplier;
        while (time > 0)
        {
            m_Renderer.widthMultiplier = Mathf.Lerp(0, baseScale, time/startingTime);
            time -= Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
