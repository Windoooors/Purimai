using System;
using System.Collections.Generic;
using Game;
using LitMotion;
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

            Hide(true);
        }

        private void Show()
        {
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
            ).Bind(x => settingsUiLayer.alpha = x));
        }

        private void Hide(bool hideOnInitialization = false)
        {
            SimulatedSensor.Enabled = false;

            SettingsPool.Save();

            LevelListController.GetInstance().levelSelectionUiLayer.gameObject.SetActive(false);

            SimulatedSensor.OnTap = null;
            SimulatedSensor.OnLeave = null;

            LevelListController.GetInstance().levelSelectionUiLayer.gameObject.SetActive(true);

            LevelListController.GetInstance().Initialize();

            OnSettingsChanged?.Invoke(this, EventArgs.Empty);

            if (!hideOnInitialization)
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
    }
}