using UnityEngine;

namespace eviltwo.RuntimeThumbnails
{
    public class ThumbnailGenerator : MonoBehaviour
    {
        public Camera Camera;

        public Vector2Int Resolution = new(128, 128);

        private void Reset()
        {
            Camera = GetComponent<Camera>();
            if (Camera == null)
            {
                Camera = FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
            }
        }

        public virtual Texture2D GenerateThumbnail()
        {
            var tempRT = RenderTexture.GetTemporary(Resolution.x, Resolution.y, 32);
            Camera.targetTexture = tempRT;
            Camera.Render();

            var beforeActiveRT = RenderTexture.active;
            RenderTexture.active = tempRT;
            var tex = new Texture2D(Resolution.x, Resolution.y, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, Resolution.x, Resolution.y), 0, 0);
            tex.Apply(false, true);
            Camera.targetTexture = null;
            RenderTexture.active = beforeActiveRT;
            RenderTexture.ReleaseTemporary(tempRT);

            return tex;
        }
    }
}
