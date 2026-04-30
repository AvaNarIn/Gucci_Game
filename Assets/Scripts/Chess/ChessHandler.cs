using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChessHandler : ItemHandler
{
    public override IEnumerator ApplyingEffects_Coroutine()
    {
        yield return new WaitForSeconds(animationDuration);
    }

    public override IEnumerator CountingScore_Coroutine()
    {
        yield return new WaitForSeconds(animationDuration);
    }
}
