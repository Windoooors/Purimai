using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace UI.Settings
{
    public class ButtonBasedValueManipulator : Manipulator
    {
        private readonly SettingsItem _settingsItem;
        private readonly string _tableName;
        private Button _button;
        private LocalizedString _buttonTitleLocalizer;

        public ButtonBasedValueManipulator(SettingsItem item, string localizationTableName)
        {
            _settingsItem = item;
            _tableName = localizationTableName;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            _button = target.Q<Button>("button");

            _buttonTitleLocalizer = new LocalizedString(_tableName,
                ((ButtonValueSet)_settingsItem.ValueSet).ButtonTextLocalizationEntry);

            _buttonTitleLocalizer.StringChanged += s => _button.text = s;
            _buttonTitleLocalizer.RefreshString();

            _button.clicked += ((ButtonValueSet)_settingsItem.ValueSet).OnClick;
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            _button.Clear();
        }
    }
}