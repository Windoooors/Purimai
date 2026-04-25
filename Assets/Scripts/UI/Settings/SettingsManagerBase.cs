using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace UI.Settings
{
    public class SettingsManagerBase : MonoBehaviour
    {
        public VisualTreeAsset numberBasedValueControllerTreeAsset;
        public VisualTreeAsset selectorBasedValueControllerTreeAsset;
        public VisualTreeAsset toggleBasedValueControllerTreeAsset;
        public VisualTreeAsset buttonBasedValueControllerTreeAsset;
        public VisualTreeAsset itemTreeAsset;

        public float itemHeight = 65;

        protected string LocalizationTableName;

        protected void SetUpList(ListView listView, SettingsItem[] items)
        {
            listView.fixedItemHeight = itemHeight;

            listView.makeItem += () => itemTreeAsset.Instantiate();

            listView.pickingMode = PickingMode.Ignore;

            listView.selectionType = SelectionType.None;

            var scrollView = listView.Q<ScrollView>();

            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;

            scrollView.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;

            listView.bindItem += (element, i) =>
            {
                var userData = new ItemUserData();

                var itemData = items[i];

                var elementRoot = element.Q<VisualElement>("settings-item");

                var itemTitleLocalizedString =
                    new LocalizedString(LocalizationTableName, $"settings.{itemData.Identifier}");

                itemTitleLocalizedString.StringChanged += value =>
                {
                    elementRoot.Q<Label>("item-title").text = value;
                    elementRoot.Q<Label>("item-name-watermark").text =
                        $"<color=white><gradient=\"level-item-watermark\">{value}</gradient></color>";
                };

                var itemDescriptionLocalizedString = new LocalizedString($"{LocalizationTableName}.Descriptions",
                    $"settings.{itemData.Identifier}");

                itemDescriptionLocalizedString.StringChanged += value =>
                {
                    elementRoot.Q<Label>("item-description").text = value;
                };

                itemTitleLocalizedString.RefreshString();

                userData.LocalizedString = itemTitleLocalizedString;

                VisualElement valuePanel = null;

                switch (itemData.ValueSet)
                {
                    case SeparatedValueSet set:
                        if (set.IsNumberBased)
                        {
                            valuePanel = numberBasedValueControllerTreeAsset.Instantiate();
                            elementRoot.Add(valuePanel);
                            userData.ValueManipulator =
                                new SwitchBasedValueManipulator(itemData, LocalizationTableName);
                            valuePanel.Q("number-based-value-panel").AddManipulator(userData.ValueManipulator);
                        }
                        else
                        {
                            valuePanel = selectorBasedValueControllerTreeAsset.Instantiate();
                            elementRoot.Add(valuePanel);
                            userData.ValueManipulator =
                                new SwitchBasedValueManipulator(itemData, LocalizationTableName);
                            valuePanel.Q("selector-based-value-panel").AddManipulator(userData.ValueManipulator);
                        }

                        break;
                    case SuccessiveIntegerValueSet:
                        valuePanel = numberBasedValueControllerTreeAsset.Instantiate();
                        elementRoot.Add(valuePanel);
                        userData.ValueManipulator = new SwitchBasedValueManipulator(itemData, LocalizationTableName);
                        valuePanel.Q("number-based-value-panel").AddManipulator(userData.ValueManipulator);
                        break;
                    case BoolValueSet:
                        valuePanel = toggleBasedValueControllerTreeAsset.Instantiate();
                        elementRoot.Add(valuePanel);
                        userData.ValueManipulator = new ToggleBasedValueManipulator(itemData, LocalizationTableName);
                        valuePanel.Q("toggle-based-value-panel").AddManipulator(userData.ValueManipulator);
                        break;
                    case ButtonValueSet:
                        valuePanel = buttonBasedValueControllerTreeAsset.Instantiate();
                        elementRoot.Add(valuePanel);
                        userData.ValueManipulator = new ButtonBasedValueManipulator(itemData, LocalizationTableName);
                        valuePanel.Q("button-based-value-panel").AddManipulator(userData.ValueManipulator);
                        break;
                }

                userData.ValuePanel = valuePanel;

                element.userData = userData;
            };

            listView.unbindItem += (element, i) =>
            {
                var userData = (ItemUserData)element.userData;

                var itemData = items[i];

                switch (itemData.ValueSet)
                {
                    case SeparatedValueSet set:
                        userData.ValuePanel
                            .Q(set.IsNumberBased ? "number-based-value-panel" : "selector-based-value-panel")
                            .RemoveManipulator(userData.ValueManipulator);
                        break;
                    case SuccessiveIntegerValueSet:
                        userData.ValuePanel.Q("number-based-value-panel")
                            .RemoveManipulator(userData.ValueManipulator);
                        break;
                    case BoolValueSet:
                        userData.ValuePanel.Q("toggle-based-value-panel")
                            .RemoveManipulator(userData.ValueManipulator);
                        break;
                    case ButtonValueSet:
                        userData.ValuePanel.Q("button-based-value-panel")
                            .RemoveManipulator(userData.ValueManipulator);
                        break;
                }

                var elementRoot = element.Q<VisualElement>("settings-item");
                elementRoot.Remove(userData.ValuePanel);
                userData.LocalizedString.Clear();
            };

            listView.itemsSource = items;
        }

        private class ItemUserData
        {
            public LocalizedString LocalizedString;

            public Manipulator ValueManipulator;
            public VisualElement ValuePanel;
        }
    }
}