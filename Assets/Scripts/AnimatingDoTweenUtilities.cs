using DG.Tweening;
using UnityEngine;

public static class AnimatingDoTweenUtilities
{

    public static void PlayGrow(GameObject go, Ease ease = Ease.Linear, float duration = 0.2f)
    {
        go.transform.localScale = Vector3.zero;
        go.transform.DOScale(Vector3.one, duration).SetEase(ease);
    }

    public static void PlayDeathAnimation(GameObject go, Ease ease = Ease.InOutSine, float duration = 0.2f)
    {
        SpriteRenderer sprite = go.GetComponent<SpriteRenderer>();

        if (!sprite)
        {
            return;
        }

        Sequence deathSequence = DOTween.Sequence();

        deathSequence.Append(sprite.DOColor(new Color(0.8f, 1f, 1f, 0.5f), 0.3f / 1.4f * duration));
        deathSequence.Append(go.transform.DOMoveY(go.transform.position.y + 4.0f, 1f / 1.4f * duration)).
           SetEase(ease);
        deathSequence.Append(sprite.DOFade(0f, 0.1f / 1.4f * duration));
        deathSequence.OnComplete(() => Die(go));
        

    }

    public static void PlayFlashRedAnimation(GameObject go, Ease ease = Ease.Flash, float duration = 0.2f)
    {
        SpriteRenderer sprite = go.GetComponent<SpriteRenderer>();

        if (!sprite)
        {
            return;
        }

        Sequence flashSequence = DOTween.Sequence();
        flashSequence.Append(sprite.DOColor(Color.red, duration)).SetEase(ease);

    }

    private static void Die(GameObject go)
    {        
        // Spawn some resources or particle effects
        // For now, just destroy the tree
        ObjectPoolManager.ReturnObjectToPool(go);
    }
}