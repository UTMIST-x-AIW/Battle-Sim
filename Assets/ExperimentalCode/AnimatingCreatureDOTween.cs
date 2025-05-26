using DG.Tweening;
using UnityEngine;

namespace ExperimentalCode
{
   public class AnimatingCreatureDOTween 
   {

      public static void Grow(GameObject go, Ease ease = Ease.Linear )
      {
         go.transform.localScale = Vector3.zero;
         go.transform.DOScale(Vector3.one, 1f).SetEase(ease).OnComplete(() => Debug.Log("Grow complete"));
      }
      
      public static void PlayDeathAnimation(GameObject go)
      {
        // go.transform.GetComponentInChildren<ParticleSystem>().Play();
         // Fade out (if using a SpriteRenderer or UI Image)
         /*if (go.TryGetComponent<SpriteRenderer>(out var sprite))
         {
            sprite.DOFade(0f, 2f).SetEase(Ease.OutQuad);
            sprite.DOColor(Color.black, 2f);
         }
          // Shrink the object
                   go.transform.DOScale(Vector3.zero, 1f)
                      .SetEase(Ease.InBack)
                      .OnComplete(() => go.SetActive(false) ); */
         Sequence deathSequence = DOTween.Sequence();
         deathSequence.Append(go.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));
         deathSequence.Join(go.GetComponent<SpriteRenderer>().DOFade(0f, 0.5f));

        
      }
   }
}

