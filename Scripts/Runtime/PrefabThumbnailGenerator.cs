using UnityEngine;

namespace eviltwo.RuntimeThumbnails
{
    public class PrefabThumbnailGenerator : ObjectThumbnailGenerator
    {
        public GameObject TargetPrefab;

        public override Texture2D GenerateThumbnail()
        {
            var instance = Instantiate(TargetPrefab);
            var previousTarget = TargetObject;
            TargetObject = instance;
            var tex = base.GenerateThumbnail();
            Destroy(instance);
            TargetObject = previousTarget;
            return tex;
        }
    }
}
