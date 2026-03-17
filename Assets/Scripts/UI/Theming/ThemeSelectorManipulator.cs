using System.Collections.Generic;
using Game.Theming;
using UnityEngine.UIElements;
using Action = System.Action;

namespace UI.Theming
{
    public class ThemeSelectorManipulator : Manipulator
    {
        private static Action _onUpdate;
        private readonly SkinData _skinData;
        private List<Button> _buttons;

        public ThemeSelectorManipulator(SkinData skinData)
        {
            _skinData = skinData;
        }

        ~ThemeSelectorManipulator()
        {
            _onUpdate -= OnUpdate;
        }

        protected override void UnregisterCallbacksFromTarget()
        {
        }

        protected override void RegisterCallbacksOnTarget()
        {
            _buttons = target.Query<Button>().ToList();

            _buttons.ForEach(x =>
            {
                x.clicked += OnButtonClick;

                return;

                void OnButtonClick()
                {
                    var index = _buttons.IndexOf(x);

                    Select(index);
                }
            });

            _onUpdate += OnUpdate;

            OnUpdate();
        }

        private void OnUpdate()
        {
            for (var i = 0; i < SkinApplier.ModuleCount; i++)
            {
                var mask = 1 << i;

                var active = (mask & _skinData.AppliedModules) != 0;

                if (active)
                    _buttons[i].AddToClassList("button--selected");
                else
                    _buttons[i].RemoveFromClassList("button--selected");
            }
        }

        private void Select(int index)
        {
            var mask = 1 << index;

            SkinManager.SkinDataList.ForEach(x =>
            {
                if (x == _skinData)
                    return;

                if ((x.AppliedModules & mask) != 0) x.AppliedModules &= ~mask;
            });

            _skinData.AppliedModules |= mask;

            _onUpdate();
        }
    }
}