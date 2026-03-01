using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace UI.Settings
{
    public class ToggleBasedValueManipulator : Manipulator
    {
        private readonly string _identifier;

        private readonly string _localizationTableName;

        private readonly SettingsItem _settingsItem;
        private int _currentValue;

        private LocalizedString _localizedString;
        private Toggle _toggle;

        private Label _valueLabel;

        public ToggleBasedValueManipulator(SettingsItem item, string localizationTableName)
        {
            _identifier = item.Identifier;
            _settingsItem = item;
            _localizationTableName = localizationTableName;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            _toggle = target.Q<Toggle>();
            _valueLabel = _toggle.Q<Label>();

            _currentValue = SettingsPool.GetValue(_identifier);

            _toggle.value = _currentValue == 1;

            _toggle.RegisterCallback<PointerUpEvent>(_ =>
            {
                SettingsPool.SetValue(_identifier, _toggle.value ? 1 : 0);
                _currentValue = _toggle.value ? 1 : 0;
                UpdateValueDisplay();
            });

            _localizedString = new LocalizedString(_localizationTableName,
                $"settings.{(_currentValue == 1 ? "true" : "false")}");

            _localizedString.StringChanged += value => { _valueLabel.text = value; };
            _localizedString.RefreshString();

            target.RegisterCallback<DetachFromPanelEvent>(_ => OnDetached());
        }

        private void UpdateValueDisplay()
        {
            _localizedString.TableEntryReference = $"settings.{(_currentValue == 1 ? "true" : "false")}";
        }

        private void OnDetached()
        {
            _localizedString?.Clear();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            OnDetached();
        }
    }
}