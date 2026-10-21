using Ignite.Unity.Scenes;
using UnityEditor;
using UnityEngine;

namespace Ignite.Unity.Editor.Scenes
{
    /// <summary>Hiện field đường dẫn scene thành ô kéo thả SceneAsset, nhưng vẫn lưu dạng string để bản build không phụ thuộc kiểu chỉ có trong Editor.</summary>
    [CustomPropertyDrawer(typeof(ScenePathAttribute))]
    public sealed class ScenePathDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "[ScenePath] chỉ dùng cho string");
                return;
            }

            var current = AssetDatabase.LoadAssetAtPath<SceneAsset>(property.stringValue);
            EditorGUI.BeginChangeCheck();
            var picked = (SceneAsset)EditorGUI.ObjectField(position, label, current, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = picked != null ? AssetDatabase.GetAssetPath(picked) : string.Empty;
        }
    }
}
