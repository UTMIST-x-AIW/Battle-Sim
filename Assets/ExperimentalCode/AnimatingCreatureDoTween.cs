using DG.Tweening;
using UnityEngine;

namespace ExperimentalCode
{
   public static class AnimatingCreatureDoTween 
   {

      public static void Grow(GameObject go, Ease ease = Ease.Linear, float duration = 0.2f)
      {
         go.transform.localScale = Vector3.zero;
         go.transform.DOScale(Vector3.one, duration).SetEase(ease);
      }
      
      public static void PlayDeathAnimation(GameObject go)
      {
         SpriteRenderer sprite = go.GetComponent<SpriteRenderer>();
         Sequence deathSequence = DOTween.Sequence();
         
         deathSequence.Append(sprite.DOColor(new Color(0.8f, 1f, 1f, 0.5f), 0.3f));
         deathSequence.Append(go.transform.DOMoveY(go.transform.position.y + 4.0f, 1f)).
            SetEase(Ease.InOutSine);
         deathSequence.Append(sprite.DOFade(0f,0.1f));
         deathSequence.OnComplete(() => {go.SetActive(false);});
      }
     
   }
}

