using System.Linq;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace UI.Settings
{
    public class ToggleBasedValueManipulator : Manipulator
    {
        private readonly string _identifier;

        private readonly SettingsItem _settingsItem;
        private int _currentValue;

        private LocalizedString _localizedString;
        private Toggle _toggle;

        private Label _valueLabel;

        public ToggleBasedValueManipulator(string identifier)
        {
            _identifier = identifier;
            var matchedCategory = SettingsItems.Settings.ToList().Find(x => x.Items.Exists(y => y.Identifier == identifier));
            _settingsItem = matchedCategory.Items.ToList().Find(x => x.Identifier == identifier);
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

            _localizedString = new LocalizedString("UI",
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