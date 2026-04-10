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

        public bool AutoCalculateBounds = true;

        public Bounds ManualBounds = new(Vector3.zero, Vector3.one);

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
                Bounds bounds;
                if (AutoCalculateBounds)
                {
                    targetObject.GetComponentsInChildren(_rendererCache);
                    bounds = new Bounds(targetObject.transform.position, Vector3.zero);
                    foreach (var r in _rendererCache)
                    {
                        bounds.Encapsulate(r.bounds);
                    }
                }
                else
                {
                    bounds = new Bounds(targetObject.transform.position + ManualBounds.center, ManualBounds.size);
                }

                // Move camera
                var rotation = Quaternion.Euler(CameraRotation);
                var camForward = rotation * Vector3.forward;
                var camUp = rotation * Vector3.up;
                var camRight = rotation * Vector3.right;
                var extents = bounds.extents;

                float distance;
                float cameraOffsetV = 0f;
                float cameraOffsetH = 0f;
                if (Camera.orthographic)
                {
                    // Orthographic: corners are symmetric around bounds.center, so no lateral offset needed
                    var aspect = (float)Resolution.x / Resolution.y;
                    float maxHalfHeight = 0f;
                    float minDepthOffset = float.MaxValue;
                    for (int i = 0; i < 8; i++)
                    {
                        var corner = new Vector3(
                            (i & 1) == 0 ? -extents.x : extents.x,
                            (i & 2) == 0 ? -extents.y : extents.y,
                            (i & 4) == 0 ? -extents.z : extents.z
                        );
                        maxHalfHeight = Mathf.Max(maxHalfHeight,
                            Mathf.Abs(Vector3.Dot(corner, camUp)),
                            Mathf.Abs(Vector3.Dot(corner, camRight)) / aspect);
                        minDepthOffset = Mathf.Min(minDepthOffset, Vector3.Dot(corner, camForward));
                    }
                    Camera.orthographicSize = maxHalfHeight;
                    distance = Mathf.Max(-minDepthOffset + Camera.nearClipPlane + 0.01f, maxHalfHeight);
                }
                else
                {
                    var aspect = (float)Resolution.x / Resolution.y;
                    var halfFovV = Camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
                    var halfFovH = Mathf.Atan(Mathf.Tan(halfFovV) * aspect);
                    var tanV = Mathf.Tan(halfFovV);
                    var tanH = Mathf.Tan(halfFovH);

                    // Pass 1: compute distance aiming at bounds.center
                    distance = Camera.nearClipPlane + 0.01f;
                    for (int i = 0; i < 8; i++)
                    {
                        var corner = new Vector3(
                            (i & 1) == 0 ? -extents.x : extents.x,
                            (i & 2) == 0 ? -extents.y : extents.y,
                            (i & 4) == 0 ? -extents.z : extents.z
                        );
                        var depthOffset = Vector3.Dot(corner, camForward);
                        var vertExtent = Mathf.Abs(Vector3.Dot(corner, camUp));
                        var horizExtent = Mathf.Abs(Vector3.Dot(corner, camRight));
                        var requiredForFov = Mathf.Max(vertExtent / tanV, horizExtent / tanH) - depthOffset;
                        var requiredForNearClip = -depthOffset + Camera.nearClipPlane + 0.01f;
                        distance = Mathf.Max(distance, requiredForFov, requiredForNearClip);
                    }

                    // Compute screen-space bounding box to find projection center
                    float minSV = float.MaxValue, maxSV = float.MinValue;
                    float minSH = float.MaxValue, maxSH = float.MinValue;
                    for (int i = 0; i < 8; i++)
                    {
                        var corner = new Vector3(
                            (i & 1) == 0 ? -extents.x : extents.x,
                            (i & 2) == 0 ? -extents.y : extents.y,
                            (i & 4) == 0 ? -extents.z : extents.z
                        );
                        var depth = Vector3.Dot(corner, camForward) + distance;
                        if (depth <= 0f) continue;
                        var sv = Vector3.Dot(corner, camUp) / depth;
                        var sh = Vector3.Dot(corner, camRight) / depth;
                        minSV = Mathf.Min(minSV, sv);
                        maxSV = Mathf.Max(maxSV, sv);
                        minSH = Mathf.Min(minSH, sh);
                        maxSH = Mathf.Max(maxSH, sh);
                    }

                    // Shift camera laterally so the projected bounding box is centered
                    cameraOffsetV = (minSV + maxSV) * 0.5f * distance;
                    cameraOffsetH = (minSH + maxSH) * 0.5f * distance;

                    // Pass 2: recompute distance with centered aim
                    distance = Camera.nearClipPlane + 0.01f;
                    for (int i = 0; i < 8; i++)
                    {
                        var corner = new Vector3(
                            (i & 1) == 0 ? -extents.x : extents.x,
                            (i & 2) == 0 ? -extents.y : extents.y,
                            (i & 4) == 0 ? -extents.z : extents.z
                        );
                        var depthOffset = Vector3.Dot(corner, camForward);
                        var vertExtent = Mathf.Abs(Vector3.Dot(corner, camUp) - cameraOffsetV);
                        var horizExtent = Mathf.Abs(Vector3.Dot(corner, camRight) - cameraOffsetH);
                        var requiredForFov = Mathf.Max(vertExtent / tanV, horizExtent / tanH) - depthOffset;
                        var requiredForNearClip = -depthOffset + Camera.nearClipPlane + 0.01f;
                        distance = Mathf.Max(distance, requiredForFov, requiredForNearClip);
                    }
                }
                Camera.transform.position = bounds.center - camForward * distance + cameraOffsetV * camUp + cameraOffsetH * camRight;
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
