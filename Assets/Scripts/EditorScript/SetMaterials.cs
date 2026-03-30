using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Game;

namespace EditorScript
{
    public class SetMaterials : EditorWindow
    {
        private Material targetMaterial;

        [MenuItem("Tools/Star Movement/Batch Update Materials")]
        public static void ShowWindow()
        {
            GetWindow<SetMaterials>("Material Updater");
        }

        private void OnGUI()
        {
            GUILayout.Label("批量更新 StarMovementController 材质", EditorStyles.boldLabel);

            targetMaterial = (Material)EditorObjectField("目标材质", targetMaterial, typeof(Material), false);

            if (GUILayout.Button("更新选中预制体中的材质"))
            {
                UpdateMaterials();
            }
        }

        private void UpdateMaterials()
        {
            if (targetMaterial == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个材质！", "确定");
                return;
            }

            // 获取 Project 视图中选中的所有物体
            Object[] selectedObjects = Selection.GetFiltered(typeof(GameObject), SelectionMode.DeepAssets);
            int updatedCount = 0;

            foreach (Object obj in selectedObjects)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);

                // 加载预制体根节点
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                bool isModified = false;

                // 查找所有包含 StarMovementController 的组件（包括子物体）
                // 假设组件名为 StarMovementController
                var controllers = prefabRoot.GetComponentsInChildren<StarMovementController>(true);

                foreach (var controller in controllers)
                {
                    if (controller.spriteRenderer != null)
                    {
                        controller.spriteRenderer.sharedMaterial = targetMaterial;
                        isModified = true;
                    }
                }

                if (isModified)
                {
                    // 保存更改回预制体
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                    updatedCount++;
                }

                // 卸载预制体内容以释放内存
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("完成", $"已成功更新 {updatedCount} 个预制体。", "太棒了");
        }

        // 辅助函数：快速绘制对象选择框
        private Object EditorObjectField(string label, Object obj, System.Type type, bool allowSceneObjects)
        {
            return EditorGUILayout.ObjectField(label, obj, type, allowSceneObjects);
        }
    }
}