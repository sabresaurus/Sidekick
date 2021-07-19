using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Assembly = System.Reflection.Assembly;
using UAssembly = UnityEditor.Compilation.Assembly;

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

        private enum Location
        {
            Assets,
            Packages,
            Precompiled,
            Dynamic
        }

        private struct Group
        {
            public readonly Location Location;
            public readonly string Context;

            public Group(Location location, string context = null)
            {
                Location = location;
                Context = context;
            }
            
            public class Comparer : IEqualityComparer<Group>
            {
                public bool Equals(Group x, Group y)
                {
                    return x.Location == y.Location && x.Context == y.Context;
                }

                public int GetHashCode(Group obj)
                {
                    unchecked
                    {
                        return ((int)obj.Location * 397) ^ (obj.Context != null ? obj.Context.GetHashCode() : 0);
                    }
                }
            }
        }

        private static Group GetGroup(Assembly assembly, Dictionary<string, UAssembly> nameToAssembly)
        {
            if (assembly.IsDynamic)
                return new Group(Location.Dynamic);
            
            string assemblyName = assembly.GetName().Name;

            // Compiled
            if (nameToAssembly.TryGetValue(assemblyName, out UAssembly uAssembly))
            {
                // Package Group
                if (uAssembly.sourceFiles.Length > 0)
                {
                    string firstFile = uAssembly.sourceFiles[0];
                    if (firstFile.StartsWith("Packages"))
                    {
                        const int startIndex = 9; // "Packages/".Length
                        
                        // Special case to reduce substrings
                        const string unityPackagePrefix = "com.unity";
                        if (firstFile.IndexOf(unityPackagePrefix, startIndex, unityPackagePrefix.Length, StringComparison.Ordinal) == startIndex)
                            return new Group(Location.Packages, "Unity");
                        
                        return new Group(Location.Packages, GetPackageOwner(firstFile, startIndex));
                    }
                }
                
                return new Group(Location.Assets);
            }

            // Precompiled
            string location = assembly.Location;
            if (!string.IsNullOrEmpty(location))
            {
                location = location.Replace('\\', '/');
                int packageCacheIndex = location.IndexOf("/Library/PackageCache/", StringComparison.Ordinal);
                if (packageCacheIndex >= 0)
                {
                    int startIndex = packageCacheIndex + 22; // + "/Library/PackageCache/".Length
                    return new Group(Location.Packages, GetPackageOwner(location, startIndex));
                }

                if (location.Contains("/Data/MonoBleedingEdge/"))
                    return new Group(Location.Precompiled, "System");

                if (location.Contains("/Library/ScriptAssemblies"))
                    return new Group(Location.Assets);
                
                return new Group(Location.Precompiled, "Unity");
            }

            return new Group(Location.Dynamic);

            string GetPackageOwner(string path, int startIndex)
            {
                int startPackageOwner = path.IndexOf('.', startIndex) + 1;
                int endPackageOwner = path.IndexOf('.', startPackageOwner);
                string packageOwner = path.Substring(startPackageOwner, endPackageOwner - startPackageOwner);
                return ObjectNames.NicifyVariableName(packageOwner);
            }
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem("Assemblies");
            Dictionary<Group, AdvancedDropdownItem> groupDropdowns = new Dictionary<Group, AdvancedDropdownItem>(new Group.Comparer());

            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies().OrderBy(assembly => assembly.FullName);
            // Populate Unity-compiled assemblies.
            // This includes assemblies in the Assets and Packages directories that are not plugins.
            UAssembly[] assemblies = CompilationPipeline.GetAssemblies();
            Dictionary<string, UAssembly> nameToAssembly = new Dictionary<string, UAssembly>();
            foreach (UAssembly assembly in assemblies)
                nameToAssembly.Add(assembly.name, assembly);

            // Append root locations
            foreach (Location location in Enum.GetValues(typeof(Location)))
            {
                var locationRoot = new AdvancedDropdownItem(location.ToString());
                root.AddChild(locationRoot);
                groupDropdowns.Add(new Group(location), locationRoot);
            }
            
            foreach (Assembly assembly in allAssemblies)
            {
                var group = GetGroup(assembly, nameToAssembly);

                if (!groupDropdowns.TryGetValue(group, out AdvancedDropdownItem groupRoot))
                {
                    groupDropdowns.Add(group, groupRoot = new AdvancedDropdownItem(group.Context));
                    groupDropdowns[new Group(group.Location)].AddChild(groupRoot);
                }

                string assemblyName = assembly.GetName().Name;
                var assemblyDropdownItem = new AdvancedDropdownItem(assemblyName);
                groupRoot.AddChild(assemblyDropdownItem);

                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    assemblyDropdownItem.AddChild(new TypeDropdownItem(type));
                }
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