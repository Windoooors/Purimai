using System;
using System.Collections.Generic;
using Game;
using LitMotion;
using LitMotion.Extensions;
using UI.LevelSelection;
using UnityEngine;

namespace UI.GameSettings
{
    public class SettingsController : UIScriptWithAnimation
    {
        public static SettingsController Instance;

        public static EventHandler OnSettingsChanged;

        public List settingsList;

        public ListItemBase settingsListItem;

        public CanvasGroup settingsUiLayer;

        private void Awake()
        {
            Instance = this;

            var itemDataList = new List<ItemDataBase>();
            foreach (var settingsCategory in SettingsItems.Settings)
            {
                itemDataList.Add(new TitleListItem.TitleData
                {
                    CategoryNameEntryString = "settings." + settingsCategory.Identifier,
                    ManagedLocalization = true
                });

                foreach (var item in settingsCategory.Items)
                    itemDataList.Add(new SettingsListItem.SettingsListItemData
                    {
                        SettingsItem = item
                    });
            }

            settingsList.Initialize(itemDataList.ToArray(), settingsListItem);

            Hide(false);
        }

        public void OnApplicationQuit()
        {
            SettingsPool.Save();
        }

        private void Show()
        {
            foreach (var itemObject in settingsList.ItemObjectPool)
            {
                if (itemObject.Data == null)
                    continue;

                itemObject.ProcessBind();
            }

            Button.GetButton(7).Press();

            Button.HideAll(false, () =>
            {
                var rightButton = Button.GetButton(2);
                var leftButton = Button.GetButton(5);

                Button.GetButton(7).ChangeIcon(UIManager.GetInstance().buttonIcons.back);

                rightButton.ChangeIcon(UIManager.GetInstance().buttonIcons.levelUp);
                leftButton.ChangeIcon(UIManager.GetInstance().buttonIcons.levelDown);

                Button.GetButton(7).Show();
                Button.GetButton(0).Show();
                Button.GetButton(3).Show();
                Button.GetButton(2).Show();
                Button.GetButton(5).Show();
            });

            SimulatedSensor.Enabled = false;

            SimulatedSensor.OnTap = (_, args) =>
            {
                if (args.SensorId == "A8")
                    Hide();
            };

            SimulatedSensor.OnLeave = null;

            settingsUiLayer.gameObject.SetActive(true);

            AddMotionHandle(LMotion.Create(0, 1f, 0.2f).WithOnComplete(() =>
                {
                    LevelListController.GetInstance().levelSelectionUiLayer.gameObject.SetActive(false);
                    SimulatedSensor.Enabled = true;
                    settingsList.GetSelectedItemObject().Select(false);
                }
            ).BindToAlpha(settingsUiLayer));
        }

        private void Hide(bool animated = true)
        {
            if (animated)
            {
                Button.GetButton(7).Press();

                Button.HideAll(false, LevelListController.GetInstance().ShowButton);
            }
            else
            {
                LevelListController.GetInstance().ShowButton();
            }

            SimulatedSensor.Enabled = false;

            SettingsPool.Save();

            LevelListController.GetInstance().levelSelectionUiLayer.gameObject.SetActive(false);

            SimulatedSensor.OnTap = null;
            SimulatedSensor.OnLeave = null;

            LevelListController.GetInstance().levelSelectionUiLayer.gameObject.SetActive(true);

            LevelListController.GetInstance().Initialize();

            OnSettingsChanged?.Invoke(this, EventArgs.Empty);

            if (animated)
            {
                AddMotionHandle(LMotion.Create(1f, 0f, 0.2f)
                    .WithOnComplete(() =>
                        {
                            SimulatedSensor.OnTap += (_, args) =>
                            {
                                if (args.SensorId == "A8")
                                    Show();
                            };

                            SimulatedSensor.Enabled = true;

                            settingsUiLayer.gameObject.SetActive(false);
                        }
                    ).Bind(x =>
                    {
                        LevelListController.GetInstance().levelSelectionUiLayer.alpha = 1 - x;
                        settingsUiLayer.alpha = x;
                    }));
            }
            else
            {
                LevelListController.GetInstance().levelSelectionUiLayer.alpha = 1;
                settingsUiLayer.alpha = 0;
                SimulatedSensor.OnTap += (_, args) =>
                {
                    if (args.SensorId == "A8")
                        Show();
                };

                SimulatedSensor.Enabled = true;

                settingsUiLayer.gameObject.SetActive(false);
            }
        }

        public void RegisterEvent()
        {
            SimulatedSensor.OnTap += (_, args) =>
            {
                if (args.SensorId == "A8")
                    Show();
            };
        }
    }
}