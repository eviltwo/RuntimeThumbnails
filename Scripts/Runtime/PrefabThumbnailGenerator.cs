using UnityEngine;

namespace eviltwo.RuntimeThumbnails
{
    public class PrefabThumbnailGenerator : ObjectThumbnailGenerator
    {
        public override Texture2D GenerateThumbnail()
        {
            var instance = Instantiate(TargetObject);
            var previousTarget = TargetObject;
            TargetObject = instance;
            var tex = base.GenerateThumbnail();
            if (Application.isPlaying)
            {
                Destroy(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }

            TargetObject = previousTarget;
            return tex;
        }
    }
}
