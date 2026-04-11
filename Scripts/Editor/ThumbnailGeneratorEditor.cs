using UnityEditor;
using UnityEngine;

namespace eviltwo.RuntimeThumbnails
{
    [CustomEditor(typeof(ThumbnailGenerator), true)]
    public class ThumbnailGeneratorEditor : Editor
    {
        private Texture2D _previewTexture;

        private void OnDisable()
        {
            if (_previewTexture != null)
            {
                DestroyImmediate(_previewTexture);
                _previewTexture = null;
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawPreviewButton();
        }

        protected void DrawPreviewButton()
        {
            EditorGUILayout.Space();

            if (GUILayout.Button("Preview Thumbnail"))
            {
                if (_previewTexture != null)
                {
                    DestroyImmediate(_previewTexture);
                    _previewTexture = null;
                }
                var generator = (ThumbnailGenerator)target;
                _previewTexture = generator.GenerateThumbnail();
            }

            if (_previewTexture != null)
            {
                float aspect = (float)_previewTexture.width / _previewTexture.height;
                float size = Mathf.Min(128f, EditorGUIUtility.currentViewWidth - 20f);
                var rect = GUILayoutUtility.GetRect(size, size / aspect, GUILayout.ExpandWidth(false));
                EditorGUI.DrawTextureTransparent(rect, _previewTexture);
            }
        }
    }
}
