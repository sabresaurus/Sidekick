using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Assembly = System.Reflection.Assembly;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UAssembly = UnityEditor.Compilation.Assembly;

namespace Sabresaurus.Sidekick
{
    public class TypeSelectDropdown : AdvancedDropdown
    {
        private readonly Action<Type> onTypeSelected;
        private readonly Type[] constraints;
        private readonly Type[] requiredInterfaces;

        class TypeDropdownItem : AdvancedDropdownItem
        {
            public Type Type { get; }

            public TypeDropdownItem(Type type) : base(type.Name)
            {
                Type = type;
                icon = (Texture2D) EditorGUIUtility.TrIconContent("dll Script Icon").image;
            }
        }

        public TypeSelectDropdown(AdvancedDropdownState state, Action<Type> onTypeSelectedCallback, Type[] constraints = null, Type[] requiredInterfaces = null) : base(state)
        {
            this.constraints = constraints ?? new Type[0];
            this.requiredInterfaces = requiredInterfaces ?? new Type[0];
            
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
                string asmDefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(uAssembly.name);
                
                if(!string.IsNullOrEmpty(asmDefPath))
                {
                    PackageInfo packageInfo = PackageInfo.FindForAssetPath(asmDefPath);

                    if (packageInfo != null)
                        return new Group(Location.Packages, GetPackageAuthor(packageInfo));
                }
                
                return new Group(Location.Assets);
            }

            // Precompiled
            string location = assembly.Location;
            if (!string.IsNullOrEmpty(location))
            {
                location = location.Replace('\\', '/');

                if (location.Contains("/Library/PackageCache/"))
                {
                    var substring = location.Substring(location.IndexOf("/Library/PackageCache/") + "/Library/PackageCache/".Length);
                    if (substring.Contains("@"))
                    {
                        substring = substring.Substring(0, substring.IndexOf("@"));
                    }
                    
                    location = "Packages/" + substring;
                }
                
                PackageInfo packageInfo = PackageInfo.FindForAssetPath(location);

                if (packageInfo != null)
                {
                    return new Group(Location.Packages, GetPackageAuthor(packageInfo));
                }

                if (location.Contains("/Data/MonoBleedingEdge/"))
                    return new Group(Location.Precompiled, "System");

                if (location.Contains("/Library/ScriptAssemblies"))
                    return new Group(Location.Assets);
                
                return new Group(Location.Precompiled, "Unity");
            }

            return new Group(Location.Dynamic);

            string GetPackageAuthor(PackageInfo packageInfo)
            {
                if(!string.IsNullOrEmpty(packageInfo.author.name))
                {
                    return packageInfo.author.name;
                }

                if (packageInfo.name.StartsWith("com.unity."))
                {
                    return "Unity";
                }

                return "Other";
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
                Group group = GetGroup(assembly, nameToAssembly);

                Type[] types = assembly.GetTypes();
                List<Type> filteredTypes = new List<Type>();
                foreach (Type type in types)
                {
                    bool requirementsMet = true;
                    foreach (Type constraint in constraints)
                    {
                        if (!type.IsSubclassOf(constraint) && type != constraint)
                        {
                            requirementsMet = false;
                        }
                    }
                    
                    foreach (Type requiredInterface in requiredInterfaces)
                    {
                        if (!type.GetInterfaces().Contains(requiredInterface))
                        {
                            requirementsMet = false;
                        }
                    }

                    if (requirementsMet)
                    {
                        filteredTypes.Add(type);
                    }
                }

                if(filteredTypes.Count != 0)
                {
                    if (!groupDropdowns.TryGetValue(group, out AdvancedDropdownItem groupRoot))
                    {
                        groupDropdowns.Add(group, groupRoot = new AdvancedDropdownItem(group.Context));
                        groupDropdowns[new Group(group.Location)].AddChild(groupRoot);
                    }
                    
                    string assemblyName = assembly.GetName().Name;
                    var assemblyDropdownItem = new AdvancedDropdownItem(assemblyName);
                    groupRoot.AddChild(assemblyDropdownItem);

                    foreach (Type type in filteredTypes)
                    {
                        assemblyDropdownItem.AddChild(new TypeDropdownItem(type));
                    }
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