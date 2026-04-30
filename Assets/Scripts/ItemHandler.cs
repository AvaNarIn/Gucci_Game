using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class ItemHandler : MonoBehaviour
{
    protected GridManager gridManager;
    [SerializeField] protected float animationDuration;
    void Awake()
    {
        gridManager = GetComponent<GridManager>();
    }

    public virtual void ApplyingEffects()
    {
        StartCoroutine(ApplyingEffects_Coroutine());
    }

    public virtual void CountingScore()
    {
        StartCoroutine(CountingScore_Coroutine());
    }

    public virtual IEnumerator ApplyingEffects_Coroutine()
    {
        yield return new WaitForSeconds(animationDuration);
    }

    public virtual IEnumerator CountingScore_Coroutine()
    {
        yield return new WaitForSeconds(animationDuration);
    }
}
