using UnityEditor;
using UnityEngine;

namespace eviltwo.RuntimeThumbnails
{
    [CustomEditor(typeof(ObjectThumbnailGenerator), true)]
    public class ObjectThumbnailGeneratorEditor : ThumbnailGeneratorEditor
    {
        private SerializedProperty _autoCalculateBounds;
        private SerializedProperty _manualBounds;

        protected virtual void OnEnable()
        {
            _autoCalculateBounds = serializedObject.FindProperty(nameof(ObjectThumbnailGenerator.AutoCalculateBounds));
            _manualBounds = serializedObject.FindProperty(nameof(ObjectThumbnailGenerator.ManualBounds));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var iterator = serializedObject.GetIterator();
            iterator.NextVisible(true);
            while (iterator.NextVisible(false))
            {
                if (iterator.propertyPath == _manualBounds.propertyPath)
                {
                    if (!_autoCalculateBounds.boolValue)
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                    continue;
                }
                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();

            DrawPreviewButton();
        }
    }
}
