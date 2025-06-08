using UnityEngine;

namespace Utils
{
    public static class Physics2DExtensions
    {
        public static RaycastHit2D RaycastWithoutSelfCollision(Vector2 origin, Vector2 direction,
            float maxDistance, GameObject self, ContactFilter2D contactFilter= default)
        {
            RaycastHit2D[] rayHits = new RaycastHit2D[2];
            int hitCount = Physics2D.Raycast(origin, direction, contactFilter, rayHits, maxDistance);

            for (int i = 0; i < hitCount; i++)
            {
                if (rayHits[i].collider.gameObject != self)
                    return rayHits[i];
            }

            return new RaycastHit2D();
        }
    }
}