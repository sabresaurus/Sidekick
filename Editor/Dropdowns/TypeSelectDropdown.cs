using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public class TypeSelectDropdown : AdvancedDropdown
    {
        private readonly Action<Type> onTypeSelected;
        
        class TypeDropdownItem : AdvancedDropdownItem
        {
            public Type Type { get; }

            public TypeDropdownItem(Type type) : base(type.Name)
            {
                Type = type;
            }
        }

        public TypeSelectDropdown(AdvancedDropdownState state, Action<Type> onTypeSelectedCallback) : base(state)
        {
            Vector2 customMinimumSize = minimumSize;
            customMinimumSize.y = 250;
            minimumSize = customMinimumSize;
            
            onTypeSelected = onTypeSelectedCallback;
        }

        private static string GetGroupName(Assembly assembly)
        {
            string assemblyName = assembly.GetName().Name;

            if (assemblyName == "UnityEngine" || assemblyName.StartsWith("UnityEngine.") ||
                assemblyName == "UnityEditor" || assemblyName.StartsWith("UnityEditor.") ||
                assemblyName == "Unity" || assemblyName.StartsWith("Unity.") ||
                assemblyName == "ExCSS.Unity" ||
                assemblyName.StartsWith("JetBrains.Rider.Unity") ||
                assemblyName == "nunit.framework")
            {
                return "Unity";
            }

            if (assemblyName == "System" || assemblyName.StartsWith("System.") ||
                assemblyName == "netstandard" ||
                assemblyName == "mscorlib" ||
                assemblyName.StartsWith("Mono."))
            {
                return "System";
            }

            return null;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem("Assemblies");

            Dictionary<string, AdvancedDropdownItem> groupDropdowns = new Dictionary<string, AdvancedDropdownItem>();
            List<AdvancedDropdownItem> ungroupedAssemblyDropdowns = new List<AdvancedDropdownItem>();

            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies().OrderBy(assembly => assembly.FullName);
            foreach (Assembly assembly in allAssemblies)
            {
                var groupName = GetGroupName(assembly);
                string assemblyName = assembly.GetName().Name;

                var assemblyDropdownItem = new AdvancedDropdownItem(assemblyName);

                if (groupName != null)
                {
                    if (!groupDropdowns.ContainsKey(groupName))
                    {
                        groupDropdowns[groupName] = new AdvancedDropdownItem(groupName);
                    }

                    groupDropdowns[groupName].AddChild(assemblyDropdownItem);
                }
                else
                {
                    ungroupedAssemblyDropdowns.Add(assemblyDropdownItem);
                }

                Type[] types = assembly.GetTypes();

                foreach (Type type in types)
                {
                    assemblyDropdownItem.AddChild(new TypeDropdownItem(type));
                }
            }

            foreach (var advancedDropdownItem in groupDropdowns)
            {
                root.AddChild(advancedDropdownItem.Value);
            }

            root.AddSeparator();

            foreach (var ungroupedAssemblyDropdown in ungroupedAssemblyDropdowns)
            {
                root.AddChild(ungroupedAssemblyDropdown);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is TypeDropdownItem typeDropdownItem)
            {
                onTypeSelected(typeDropdownItem.Type);
            }
        }
    }
}