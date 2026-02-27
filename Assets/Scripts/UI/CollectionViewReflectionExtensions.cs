using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/* From FreedLow@https://discussions.unity.com/t/touch-drag-scrolling-on-listview-not-working/1709855/3 */
namespace UI
{
    public static class CollectionViewReflectionExtensions
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static FieldInfo _baseCollectionScrollViewField;
        private static FieldInfo _scrollViewTouchDraggingAllowedField;
        private static bool _initialized;
        private static bool _initSucceeded;

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                // 1) BaseVerticalCollectionView.m_ScrollView
                _baseCollectionScrollViewField = typeof(BaseVerticalCollectionView)
                    .GetField("m_ScrollView", Flags);

                if (_baseCollectionScrollViewField == null)
                {
                    Debug.LogWarning(
                        "[UI Toolkit Reflection] Field not found: BaseVerticalCollectionView.m_ScrollView");
                    _initSucceeded = false;
                    return;
                }

                // 2) Internal type scrollView
                var scrollViewFieldType = _baseCollectionScrollViewField.FieldType;
                if (scrollViewFieldType == null)
                {
                    Debug.LogWarning("[UI Toolkit Reflection] m_ScrollView field type is null");
                    _initSucceeded = false;
                    return;
                }

                // 3) scrollView.m_TouchDraggingAllowed
                _scrollViewTouchDraggingAllowedField = scrollViewFieldType
                    .GetField("m_TouchDraggingAllowed", Flags);

                if (_scrollViewTouchDraggingAllowedField == null)
                {
                    Debug.LogWarning(
                        $"[UI Toolkit Reflection] Field not found: {scrollViewFieldType.Name}.m_TouchDraggingAllowed");
                    _initSucceeded = false;
                    return;
                }

                if (_scrollViewTouchDraggingAllowedField.FieldType != typeof(bool))
                {
                    Debug.LogWarning(
                        $"[UI Toolkit Reflection] Unexpected type for m_TouchDraggingAllowed: {_scrollViewTouchDraggingAllowedField.FieldType}");
                    _initSucceeded = false;
                    return;
                }

                _initSucceeded = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UI Toolkit Reflection] Initialization failed: {e}");
                _initSucceeded = false;
            }
        }

        /// <summary>
        ///     Tries to set internal ScrollView touch dragging flag used by BaseVerticalCollectionView.
        ///     Returns false if Unity internals changed or reflection is unavailable.
        /// </summary>
        public static bool TrySetTouchDraggingAllowed(this BaseVerticalCollectionView collectionView, bool allowed)
        {
            if (collectionView == null)
                return false;

            EnsureInitialized();
            if (!_initSucceeded)
                return false;

            try
            {
                var scrollViewObj = _baseCollectionScrollViewField.GetValue(collectionView);
                if (scrollViewObj == null)
                {
                    Debug.LogWarning("[UI Toolkit Reflection] m_ScrollView is null on collectionView");
                    return false;
                }

                _scrollViewTouchDraggingAllowedField.SetValue(scrollViewObj, allowed);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UI Toolkit Reflection] TrySetTouchDraggingAllowed failed: {e}");
                return false;
            }
        }
    }
}