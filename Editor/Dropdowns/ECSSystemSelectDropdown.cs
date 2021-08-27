#if ECS_EXISTS
using System;
using Unity.Entities;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public class ECSSystemSelectDropdown : AdvancedDropdown
    {
        private readonly Action<object> onObjectSelected;

        class ECSSystemDropdownItem : AdvancedDropdownItem
        {
            public ComponentSystemBase System { get; }

            public ECSSystemDropdownItem(ComponentSystemBase system) : base(GetDisplayName(system))
            {
                System = system;
            }

            private static string GetDisplayName(ComponentSystemBase system)
            {
                return system.ToString();
            }
        }

        public ECSSystemSelectDropdown(AdvancedDropdownState state, Action<object> onObjectSelectedCallback) : base(state)
        {
            Vector2 customMinimumSize = minimumSize;
            customMinimumSize.y = 250;
            minimumSize = customMinimumSize;
            
            onObjectSelected = onObjectSelectedCallback;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem("Unity Objects");

            foreach (World world in World.All)
            {
                foreach (ComponentSystemBase componentSystemBase in world.Systems)
                {
                    root.AddChild(new ECSSystemDropdownItem(componentSystemBase));
                }
            }
            
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is ECSSystemDropdownItem windowDropdownItem)
            {
                onObjectSelected(windowDropdownItem.System);
            }
        }
    }
}
#endif