using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class HierarchySelectionUtility : EditorWindow
    {
        private string _fieldName = "slideSpriteRenderers";
        private MonoBehaviour _targetComponent;

        private void OnGUI()
        {
            GUILayout.Label("Fill objects by order in hierarchy.", EditorStyles.boldLabel);

            _targetComponent =
                (MonoBehaviour)EditorGUILayout.ObjectField("Target Component", _targetComponent, typeof(MonoBehaviour),
                    true);
            _fieldName = EditorGUILayout.TextField("Field Name", _fieldName);

            if (GUILayout.Button("Fill")) FillArrayFromSelection();
        }

        [MenuItem("Tools/Hierarchy Selection Utility")]
        public static void ShowWindow()
        {
            GetWindow<HierarchySelectionUtility>("Hierarchy Array Filler");
        }

        private void FillArrayFromSelection()
        {
            var selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                Debug.LogWarning("No objects selected.");
                return;
            }

            var type = _targetComponent.GetType();
            var field = type.GetField(_fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                Debug.LogError($"Field name {_fieldName} can't be found in {type.Name}");
                return;
            }

            if (!field.FieldType.IsArray)
            {
                Debug.LogError($"Field name {_fieldName} is not an array.");
                return;
            }

            var elementType = field.FieldType.GetElementType();

            var sceneRoots = selected[0].scene.GetRootGameObjects()
                .OrderBy(go => go.transform.GetSiblingIndex());

            List<GameObject> hierarchyList = new();
            foreach (var root in sceneRoots)
                hierarchyList.AddRange(root.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject));

            var orderedSelection = hierarchyList.Where(go => selected.Contains(go)).ToList();

            var resultArray = Array.CreateInstance(elementType, orderedSelection.Count);
            for (var i = 0; i < orderedSelection.Count; i++)
            {
                object value = null;

                if (elementType == typeof(GameObject))
                    value = orderedSelection[i];
                else if (typeof(Component).IsAssignableFrom(elementType))
                    value = orderedSelection[i].GetComponent(elementType);
                else
                    return;

                resultArray.SetValue(value, i);
            }

            field.SetValue(_targetComponent, resultArray);
            EditorUtility.SetDirty(_targetComponent);

            Debug.Log($"{orderedSelection.Count} objects are filled in {type.Name}.{_fieldName}");
        }
    }
}