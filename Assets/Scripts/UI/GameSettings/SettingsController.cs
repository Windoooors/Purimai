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
                    settingsList.ItemObjectList[settingsList.index].Select();
                }
            ).Bind(x => settingsUiLayer.alpha = x));
        }

        private void Hide(bool initializeHide = true)
        {
            SimulatedSensor.Enabled = false;

            SettingsPool.Save();

            LevelListController.GetInstance().levelSelectionUiLayer.gameObject.SetActive(false);

            SimulatedSensor.OnTap = null;
            SimulatedSensor.OnLeave = null;

            LevelListController.GetInstance().levelSelectionUiLayer.gameObject.SetActive(true);

            if (initializeHide)
                LevelListController.GetInstance().Reinitialize();

            AddMotionHandle(LMotion.Create(1f, 0f, initializeHide ? 0.2f : 0)
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
    }
}