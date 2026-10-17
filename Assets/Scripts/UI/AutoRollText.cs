using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Mask))]
[RequireComponent(typeof(Image))]
public class AutoRollText : MonoBehaviour
{
    public enum RollType
    {
        Pingpong = 0,
        Left = 1
    }

    public RollType rollType = RollType.Pingpong;
    public Text m_text;
    [Header("延迟时间")] public float delay = 5f;
    [Header("移动速度（按 60 FPS 下的每帧像素）")] public float speed = .1f;
    [Header("边界额外移动值")] public float limitDiff = 5f;

    private float textWidth;
    private RectTransform rectTransform;
    private Coroutine rollCoroutine;

    public string text
    {
        get => m_text != null ? m_text.text : string.Empty;
        set
        {
            if (m_text == null)
            {
                return;
            }
            m_text.text = value;
            CalculateTextPosition();
        }
    }

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        if (m_text == null)
        {
            m_text = GetComponentInChildren<Text>(true);
        }

        if (m_text == null || rectTransform == null)
        {
            enabled = false;
            return;
        }

        m_text.rectTransform.pivot = new Vector2(0f, 0.5f);
        CalculateTextPosition();
    }

    private void OnDisable()
    {
        StopRolling();
    }

    private void CalculateTextPosition()
    {
        if (m_text == null || rectTransform == null)
        {
            return;
        }

        TextGenerationSettings settings = m_text.GetGenerationSettings(Vector2.zero);
        textWidth = m_text.cachedTextGeneratorForLayout.GetPreferredWidth(m_text.text, settings) /
                    Mathf.Max(0.01f, m_text.pixelsPerUnit);

        Vector2 size = m_text.rectTransform.sizeDelta;
        size.x = textWidth;
        m_text.rectTransform.sizeDelta = size;

        Vector2 startPosition = new Vector2(-size.x / 2f, 0f);
        startPosition.x += (textWidth - rectTransform.rect.width) / 2f;
        m_text.rectTransform.anchoredPosition = startPosition;

        StopRolling();
        if (isActiveAndEnabled && textWidth > rectTransform.rect.width)
        {
            rollCoroutine = StartCoroutine(RollCoroutine());
        }
    }

    private IEnumerator RollCoroutine()
    {
        Vector2 startPosition = m_text.rectTransform.anchoredPosition;
        Vector2 currentPosition = startPosition;
        float offset = textWidth - rectTransform.rect.width;
        float unitsPerSecond = Mathf.Max(0f, speed) * 60f;

        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        bool movingLeft = true;
        while (true)
        {
            float delta = unitsPerSecond * Time.unscaledDeltaTime;
            currentPosition.x += movingLeft ? -delta : delta;

            if (rollType == RollType.Pingpong)
            {
                if (currentPosition.x <= startPosition.x - offset - limitDiff)
                {
                    currentPosition.x = startPosition.x - offset - limitDiff;
                    movingLeft = false;
                }
                else if (currentPosition.x >= startPosition.x + limitDiff)
                {
                    currentPosition.x = startPosition.x + limitDiff;
                    movingLeft = true;
                }
            }
            else if (currentPosition.x <= startPosition.x - offset - limitDiff)
            {
                m_text.rectTransform.anchoredPosition = currentPosition;
                if (delay > 0f)
                {
                    yield return new WaitForSecondsRealtime(delay);
                }
                currentPosition = startPosition;
            }

            m_text.rectTransform.anchoredPosition = currentPosition;
            yield return null;
        }
    }

    private void StopRolling()
    {
        if (rollCoroutine != null)
        {
            StopCoroutine(rollCoroutine);
            rollCoroutine = null;
        }
    }
}
