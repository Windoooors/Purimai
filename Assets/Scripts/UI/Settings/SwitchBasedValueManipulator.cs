using System.Linq;
using UI.Settings;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace UI.Settings
{
    public class SwitchBasedValueManipulator : Manipulator
    {
        private const int HoldingTimeThreshold = 500;
        private Button _addButton;

        private int _currentValue;
        private int _direction;
        private bool _holding;

        private int _holdingTime;
        private readonly string _identifier;

        private LocalizedString _localizedString;

        private IVisualElementScheduledItem _monitorTask;
        private bool _pressed;

        private readonly SettingsItem _settingsItem;
        private Button _subButton;

        private Label _valueLabel;

        public SwitchBasedValueManipulator(string identifier)
        {
            _identifier = identifier;
            var matchedCategory = SettingsItems.Settings.ToList().Find(x => identifier.StartsWith(x.Identifier));
            _settingsItem = matchedCategory.Items.ToList().Find(x => x.Identifier == identifier);
        }

        private void Add()
        {
            switch (_settingsItem.ValueSet)
            {
                case SeparatedValueSet separatedValueSet:
                    if (_currentValue < separatedValueSet.AvailableValues.Length - 1)
                        _currentValue++;
                    break;
                case SuccessiveIntegerValueSet successiveValueSet:
                    if (_currentValue < successiveValueSet.ValueUpperLimit)
                        _currentValue++;
                    break;
            }

            SettingsPool.SetValue(_identifier, _currentValue);
            UpdateValueDisplay();
        }

        private void Sub()
        {
            switch (_settingsItem.ValueSet)
            {
                case SeparatedValueSet:
                    if (_currentValue > 0)
                        _currentValue--;
                    break;
                case SuccessiveIntegerValueSet successiveValueSet:
                    if (_currentValue > successiveValueSet.ValueLowerLimit)
                        _currentValue--;
                    break;
            }

            SettingsPool.SetValue(_identifier, _currentValue);
            UpdateValueDisplay();
        }

        private void OnFrameUpdate()
        {
            if (_holding)
                switch (_direction)
                {
                    case -1:
                        Sub();
                        break;
                    case 1:
                        Add();
                        break;
                }

            if (_pressed)
            {
                _holdingTime += 33;

                if (_holdingTime > HoldingTimeThreshold)
                {
                    _holdingTime = 0;
                    _holding = true;

                    _pressed = false;
                }
            }
        }

        private void UpdateValueDisplay()
        {
            if (_settingsItem.ValueSet is SeparatedValueSet separatedValueSet)
            {
                if (_settingsItem.ManagedValueLocalization)
                {
                    if (_localizedString != null)
                    {
                        _localizedString.TableEntryReference =
                            $"settings.{_identifier}.{separatedValueSet.AvailableValues[_currentValue]}";
                    }
                    else
                    {
                        _localizedString = new LocalizedString("UI",
                            $"settings.{_identifier}.{separatedValueSet.AvailableValues[_currentValue]}");

                        _localizedString.StringChanged += value => { _valueLabel.text = value; };

                        _localizedString.RefreshString();
                    }
                }
                else
                {
                    _valueLabel.text = separatedValueSet.AvailableValues[_currentValue];
                }
            }

            else if (_settingsItem.ValueSet is SuccessiveIntegerValueSet successiveValueSet)
            {
                _valueLabel.text = _currentValue.ToString();
            }
        }


        protected override void RegisterCallbacksOnTarget()
        {
            _subButton = target.Q<Button>("sub-button");
            _addButton = target.Q<Button>("add-button");
            _valueLabel = target.Q<Label>("value-label");

            _addButton.clicked += Add;
            _subButton.clicked += Sub;

            _addButton.Q("cover").RegisterCallback<PointerDownEvent>(_ =>
            {
                _holding = false;
                _holdingTime = 0;

                _pressed = true;
                _direction = 1;
            });

            _subButton.Q("cover").RegisterCallback<PointerDownEvent>(_ =>
            {
                _holding = false;
                _holdingTime = 0;

                _pressed = true;
                _direction = -1;
            });

            _addButton.Q("cover").RegisterCallback<PointerLeaveEvent>(_ =>
            {
                _holding = false;
                _holdingTime = 0;
                _pressed = false;
                _direction = 0;
            });

            _subButton.Q("cover").RegisterCallback<PointerLeaveEvent>(_ =>
            {
                _holding = false;
                _holdingTime = 0;
                _pressed = false;
                _direction = 0;
            });

            _currentValue = SettingsPool.GetValue(_identifier);

            UpdateValueDisplay();

            _monitorTask = target.schedule.Execute(OnFrameUpdate).Every(33);

            target.RegisterCallback<DetachFromPanelEvent>(_ => OnDetached());
        }

        private void OnDetached()
        {
            _monitorTask?.Pause();

            _addButton.Clear();
            _subButton.Clear();

            _localizedString?.Clear();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            OnDetached();
        }
    }
}