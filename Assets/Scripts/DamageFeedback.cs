using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DamageFeedback : MonoBehaviour
{
    [SerializeField] private Graphic flashGraphic;
    [SerializeField] private RectTransform shakeTarget;

    [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private float flashDuration = 2f;
    [SerializeField] private float shakeDuration = 2f;
    [SerializeField] private float shakeStrength = 8f;
    [SerializeField] private float punchScale = 1.08f;
    [SerializeField] private float scaleDuration = 2f;

    private Color baseColor;
    private Vector3 baseScale;
    private Vector2 baseAnchoredPos;

    private Coroutine routine;
    private System.Random rng;

    private void Awake()
    {
        rng = new System.Random();

        if (shakeTarget == null) shakeTarget = transform as RectTransform;
        baseScale = shakeTarget != null ? shakeTarget.localScale : transform.localScale;

        if (flashGraphic != null)
            baseColor = flashGraphic.color;   // запоминаем исходный цвет
    }

    public void Play()
    {
        if (shakeTarget != null) baseAnchoredPos = shakeTarget.anchoredPosition;
        if (flashGraphic != null) baseColor = flashGraphic.color;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        float t = 0f;
        float total = Mathf.Max(flashDuration, shakeDuration, scaleDuration);

        if (flashGraphic != null) flashGraphic.color = hitColor;
        if (shakeTarget != null) shakeTarget.localScale = baseScale * punchScale;

        while (t < total)
        {
            t += Time.deltaTime;

            if (shakeTarget != null)
            {
                float p = Mathf.Clamp01(t / shakeDuration);
                float amp = (1f - p) * shakeStrength;
                float rx = (float)(rng.NextDouble() * 2.0 - 1.0) * amp;
                float ry = (float)(rng.NextDouble() * 2.0 - 1.0) * amp;
                shakeTarget.anchoredPosition = baseAnchoredPos + new Vector2(rx, ry);
            }

            if (shakeTarget != null)
            {
                float sp = Mathf.Clamp01(t / scaleDuration);
                shakeTarget.localScale = Vector3.Lerp(baseScale * punchScale, baseScale, sp);
            }

            if (flashGraphic != null)
            {
                float fp = Mathf.Clamp01(t / flashDuration);
                flashGraphic.color = Color.Lerp(hitColor, baseColor, fp);
            }

            yield return null;
        }

        // Финальный гарантированный сброс
        if (shakeTarget != null)
        {
            shakeTarget.anchoredPosition = baseAnchoredPos;
            shakeTarget.localScale = baseScale;
        }
        if (flashGraphic != null)
            flashGraphic.color = baseColor;

        routine = null;
    }

    private void OnDisable()
    {
        // Если объект выключается (например, уничтожается), принудительно возвращаем цвет
        if (flashGraphic != null)
            flashGraphic.color = baseColor;
        if (shakeTarget != null)
        {
            shakeTarget.localScale = baseScale;
            shakeTarget.anchoredPosition = baseAnchoredPos;
        }
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }
}