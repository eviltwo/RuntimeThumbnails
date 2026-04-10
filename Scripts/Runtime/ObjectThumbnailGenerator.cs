using System.Collections.Generic;
using UnityEngine;

namespace eviltwo.RuntimeThumbnails
{
    public class ObjectThumbnailGenerator : MonoBehaviour
    {
        public Camera Camera;

        public Vector2Int Resolution = new(128, 128);

        public Vector3 CapturePosition = new(1000, 1000, 1000);

        public Vector3 CameraRotation = new(45, 45, 0);

        private void Reset()
        {
            Camera = GetComponent<Camera>();
            if (Camera == null)
            {
                Camera = FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
            }
        }

        public Texture2D GeneratePrefabThumbnail(GameObject targetPrefab)
        {
            var ga = Instantiate(targetPrefab);
            var tex = GenerateObjectThumbnail(ga);
            Destroy(ga);
            return tex;
        }

        private readonly List<Renderer> _rendererCache = new();

        public Texture2D GenerateObjectThumbnail(GameObject targetObject)
        {
            using (new ObjectAdjustScope(targetObject.transform, CapturePosition))
            {
                // Calculate object size
                targetObject.GetComponentsInChildren(_rendererCache);
                var bounds = new Bounds(targetObject.transform.position, Vector3.zero);
                foreach (var r in _rendererCache)
                {
                    bounds.Encapsulate(r.bounds);
                }

                // Move camera
                var distance = bounds.extents.magnitude * 2;
                var rotation = Quaternion.Euler(CameraRotation);
                Camera.transform.position = bounds.center + rotation * Vector3.back * distance;
                Camera.transform.rotation = rotation;

                // Render
                var tempRT = RenderTexture.GetTemporary(Resolution.x, Resolution.y, 32);
                Camera.targetTexture = tempRT;
                Camera.Render();

                // Print to Texture
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

        private class ObjectAdjustScope : System.IDisposable
        {
            private readonly Transform _transform;
            private readonly Transform _parent;
            private readonly Vector3 _position;
            private readonly Quaternion _rotation;

            public ObjectAdjustScope(Transform transform, Vector3 adjustPosition)
            {
                _transform = transform;
                _position = _transform.position;
                _rotation = _transform.rotation;
                _parent = _transform.parent;
                _transform.SetParent(null);
                _transform.SetPositionAndRotation(adjustPosition, Quaternion.identity);
            }

            public void Dispose()
            {
                if (_transform)
                {
                    _transform.SetParent(_parent);
                    _transform.SetPositionAndRotation(_position, _rotation);
                }
            }
        }
    }
}
