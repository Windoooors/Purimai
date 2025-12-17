using System;
using System.Collections;
using Game;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace UI.GameSettings
{
    public class SettingsListItem : ListItemBase
    {
        public LocalizeStringEvent titleLocalizeStringEvent;
        public LocalizeStringEvent titleBackgroundLocalizeStringEvent;
        public LocalizeStringEvent valueLocalizeStringEvent;
        public TextMeshProUGUI valueText;
        public Image background;
        public TextMeshProUGUI titleBackgroundText;

        private SettingsListItemData _data;
        private int _holdDirection;
        private float _holdTime;

        private bool _isHolding;

        private bool _isSelectingByHolding;
        private bool _isSeparated;

        private void Update()
        {
            if (_isHolding)
                _holdTime += Time.deltaTime;

            if (_holdTime > 0.5f && !_isSelectingByHolding)
            {
                _isSelectingByHolding = true;
                StartCoroutine(RepeatedlySelect());
            }
        }

        private void OnEnable()
        {
            if (Data != null)
                ProcessBind();
        }

        public override void ProcessBind()
        {
            if (Data is not SettingsListItemData settingsListItemData)
                throw new Exception("Data type is not SettingsListItemData");

            var settingsItem = settingsListItemData.SettingsItem;

            _data = settingsListItemData;
            _isSeparated = _data.SettingsItem.ValueSet is SeparatedValueSet;

            titleLocalizeStringEvent.SetEntry($"settings.{settingsItem.Identifier}");
            titleBackgroundLocalizeStringEvent.SetEntry($"settings.{settingsItem.Identifier}");
            if (settingsItem.ValueSet is SeparatedValueSet separatedValueSet)
            {
                if (settingsItem.ManagedValueLocalization)
                {
                    valueLocalizeStringEvent.enabled = true;
                    valueLocalizeStringEvent.SetEntry(
                        $"settings.{settingsItem.Identifier}.{separatedValueSet.AvailableValues[SettingsPool.GetValue(_data.SettingsItem.Identifier)]}");
                }
                else
                {
                    valueLocalizeStringEvent.enabled = false;
                    valueText.text =
                        separatedValueSet.AvailableValues[SettingsPool.GetValue(_data.SettingsItem.Identifier)];
                }
            }
            else if (settingsItem.ValueSet is SuccessiveIntegerValueSet)
            {
                valueLocalizeStringEvent.enabled = false;
                valueText.text = SettingsPool.GetValue(_data.SettingsItem.Identifier).ToString();
            }
        }

        private void SelectRight()
        {
            Select(1);
        }

        private void SelectLeft()
        {
            Select(-1);
        }

        private void Select(int direction)
        {
            if (_isSeparated)
            {
                var separatedValueSet = (SeparatedValueSet)_data.SettingsItem.ValueSet;
                var currentValue = SettingsPool.GetValue(_data.SettingsItem.Identifier);

                if ((currentValue > separatedValueSet.AvailableValues.Length - 2 && direction > 0) ||
                    (currentValue < 1 && direction < 0))
                    return;

                switch (direction)
                {
                    case 1:
                        Button.GetButton(2).Press();
                        break;
                    case -1:
                        Button.GetButton(5).Press();
                        break;
                }

                SettingsPool.SetValue(_data.SettingsItem.Identifier, currentValue + direction);

                if (_data.SettingsItem.ManagedValueLocalization)
                {
                    valueLocalizeStringEvent.enabled = true;
                    valueLocalizeStringEvent.SetEntry(
                        $"settings.{_data.SettingsItem.Identifier}.{separatedValueSet.AvailableValues[currentValue + direction]}");
                }
                else
                {
                    valueLocalizeStringEvent.enabled = false;
                    valueText.text =
                        separatedValueSet.AvailableValues[currentValue + direction];
                }
            }
            else
            {
                var successiveValueSet = (SuccessiveIntegerValueSet)_data.SettingsItem.ValueSet;
                var currentValue = SettingsPool.GetValue(_data.SettingsItem.Identifier);

                if ((currentValue > successiveValueSet.ValueUpperLimit - 1 && direction > 0) ||
                    (currentValue < successiveValueSet.ValueLowerLimit + 1 && direction < 0))
                    return;

                switch (direction)
                {
                    case 1:
                        Button.GetButton(2).Press();
                        break;
                    case -1:
                        Button.GetButton(5).Press();
                        break;
                }

                SettingsPool.SetValue(_data.SettingsItem.Identifier, currentValue + direction);

                valueText.text = (currentValue + direction).ToString();
            }
        }

        private IEnumerator RepeatedlySelect()
        {
            while (_isSelectingByHolding)
            {
                yield return new WaitForSeconds(0.05f);
                Select(_holdDirection);
            }
        }

        private void StartHoldingRight()
        {
            if (_isHolding)
                return;

            _isHolding = true;
            _holdDirection = 1;
        }

        private void StartHoldingLeft()
        {
            if (_isHolding)
                return;

            _isHolding = true;
            _holdDirection = -1;
        }

        private void EndHoldingRight()
        {
            if (!_isHolding || _holdDirection != 1) return;

            _isHolding = false;
            _holdTime = 0;
            _isSelectingByHolding = false;
        }

        private void EndHoldingLeft()
        {
            if (!_isHolding || _holdDirection != -1) return;

            _isHolding = false;
            _holdTime = 0;
            _isSelectingByHolding = false;
        }

        private void OnLeave(object sender, TouchEventArgs args)
        {
            if (args.SensorId == "A3")
                EndHoldingRight();
            else if (args.SensorId == "A6")
                EndHoldingLeft();

            SimulatedSensor.OnLeave -= OnLeave;
        }

        public override void ProcessSelect(bool animated = true)
        {
            SimulatedSensor.OnTap += OnTap;
            if (animated)
            {
                AddMotionHandle(LMotion.Create(new Color(1, 1, 1, 0), Color.white, 0.5f).WithEase(Ease.OutExpo)
                    .Bind(x =>
                    {
                        titleBackgroundText.color = new Color(titleBackgroundText.color.r,
                            titleBackgroundText.color.g, titleBackgroundText.color.b,
                            x.a
                        );
                        background.color = x;
                    }));
            }
            else
            {
                ClearMotion();

                titleBackgroundText.color = new Color(titleBackgroundText.color.r,
                    titleBackgroundText.color.g, titleBackgroundText.color.b,
                    1f
                );
                background.color = Color.white;
            }
        }

        public override void ProcessDeselect(bool animated = true)
        {
            SimulatedSensor.OnTap -= OnTap;
            if (animated)
            {
                AddMotionHandle(LMotion.Create(Color.white, new Color(1, 1, 1, 0), 0.5f).WithEase(Ease.OutExpo)
                    .Bind(x =>
                    {
                        titleBackgroundText.color = new Color(titleBackgroundText.color.r,
                            titleBackgroundText.color.g, titleBackgroundText.color.b, x.a);
                        background.color = x;
                    }));
            }
            else
            {
                ClearMotion();

                titleBackgroundText.color = new Color(titleBackgroundText.color.r,
                    titleBackgroundText.color.g, titleBackgroundText.color.b, 0);
                background.color = new Color(1, 1, 1, 0);
            }
        }

        private void OnTap(object sender, TouchEventArgs args)
        {
            if (args.SensorId == "A3")
            {
                StartHoldingRight();
                SelectRight();
            }
            else if (args.SensorId == "A6")
            {
                StartHoldingLeft();
                SelectLeft();
            }

            SimulatedSensor.OnLeave += OnLeave;
        }

        public class SettingsListItemData : ItemDataBase
        {
            public SettingsItem SettingsItem;
        }
    }
}