using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Sabresaurus.Sidekick.Responses;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick.Requests
{
    [Flags]
    public enum InfoFlags : byte
    {
        Fields = 1,
        Properties = 2,
        Methods = 4,
    }

    /// <summary>
    /// Gets reflected information about components on a game object or another object specified by search data. Flags specify what information to include.
    /// </summary>
    public class GetObjectRequest : BaseRequest
    {
        public const BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        enum SearchType
        {
            Path, // e.g. SceneName//Main Camera, or Assets/texture.png
            ObjectGUID, // any System.Object in the ObjectMap
        }

        SearchType searchType;
        string objectPath; // Path in scene or assets
        Guid guid; // GUID directly to any System.Object known by the ObjectMap
        InfoFlags flags;

        public GetObjectRequest(string objectPath, InfoFlags flags)
        {
            this.searchType = SearchType.Path;
            this.objectPath = objectPath;
            this.flags = flags;
        }
        
        public GetObjectRequest(Guid objectGuid, InfoFlags flags)
        {
            this.searchType = SearchType.ObjectGUID;
            this.guid = objectGuid;
            this.flags = flags;
        }

        public GetObjectRequest(BinaryReader br)
        {
            this.searchType = (SearchType) br.ReadInt32();
            if (searchType == SearchType.Path)
            {
                this.objectPath = br.ReadString();
            }
            else if (searchType == SearchType.ObjectGUID)
            {
                this.guid = new Guid(br.ReadString());
            }
            else
            {
                throw new NotImplementedException(searchType + " not implemented");
            }

            this.flags = (InfoFlags) br.ReadInt32();
        }

        public override void Write(BinaryWriter bw)
        {
            base.Write(bw);
            bw.Write((int) searchType);
            if (searchType == SearchType.Path)
            {
                bw.Write(objectPath);
            }
            else if (searchType == SearchType.ObjectGUID)
            {
                bw.Write(this.guid.ToString());
            }
            else
            {
                throw new NotImplementedException(searchType + " not implemented");
            }

            bw.Write((int) flags);
        }

        public override BaseResponse GenerateResponse()
        {
            GetObjectResponse response = new GetObjectResponse();
            List<object> components = new List<object>();
            if (searchType == SearchType.Path)
            {
                Object foundObject = HierarchyHelper.GetFromPath(objectPath);
                if (foundObject is GameObject)
                {
                    GameObject foundGameObject = (GameObject) foundObject;

                    // Not technically a component, but include the GameObject
                    components.Add(foundGameObject);
                    components.AddRange(foundGameObject.GetComponents<Component>());
                }
                else
                {
                    components.Add(foundObject);
                }

                response.ObjectName = foundObject.name;
            }
            else if (searchType == SearchType.ObjectGUID)
            {
                object foundObject = ObjectMap.GetObjectFromGUID(guid);
                if (foundObject is GameObject)
                {
                    GameObject foundGameObject = (GameObject) foundObject;

                    // Not technically a component, but include the GameObject
                    components.Add(foundGameObject);
                    components.AddRange(foundGameObject.GetComponents<Component>());
                }
                else
                {
                    components.Add(foundObject);
                }

                Object foundUnityObject = foundObject as Object;
                if (foundUnityObject != null)
                {
                    response.ObjectName = foundUnityObject.name;
                }
            }
            else
            {
                throw new NotImplementedException(searchType + " not implemented");
            }

            response.Components = new List<ComponentDescription>(components.Count);
            foreach (object component in components)
            {
                //Guid guid = ObjectMap.AddOrGetObject(component);
                ObjectMap.AddOrGetObject(component);

                ComponentDescription description = new ComponentDescription(component);
                Type componentType = component.GetType();

                while (componentType != null
                       && !InspectionExclusions.GetExcludedTypes().Contains(componentType))
                {
                    ComponentScope componentScope = new ComponentScope(componentType);
                    if ((flags & InfoFlags.Fields) == InfoFlags.Fields)
                    {
                        FieldInfo[] fieldInfos = componentType.GetFields(BINDING_FLAGS);
                        foreach (FieldInfo fieldInfo in fieldInfos)
                        {
                            if (TypeUtility.IsBackingField(fieldInfo, componentType))
                            {
                                // Skip backing fields for auto-implemented properties
                                continue;
                            }

                            object objectValue = fieldInfo.GetValue(component);

                            WrappedVariable wrappedVariable = new WrappedVariable(fieldInfo, objectValue);
                            componentScope.Fields.Add(wrappedVariable);
                        }
                    }

                    if (componentType == typeof(GameObject)) // Special handling for GameObject.name to always be included
                    {
                        PropertyInfo nameProperty = componentType.GetProperty("name", BindingFlags.Public | BindingFlags.Instance);
                        WrappedVariable wrappedName = new WrappedVariable(nameProperty, nameProperty.GetValue(component, null));
                        componentScope.Properties.Add(wrappedName);
                    }

                    if ((flags & InfoFlags.Properties) == InfoFlags.Properties)
                    {
                        PropertyInfo[] properties = componentType.GetProperties(BINDING_FLAGS);
                        foreach (PropertyInfo property in properties)
                        {
                            Type declaringType = property.DeclaringType;
                            if (declaringType == typeof(Component)
                                || declaringType == typeof(UnityEngine.Object))
                            {
                                continue;
                            }

                            object[] attributes = property.GetCustomAttributes(false);
                            bool isObsoleteWithError = AttributeHelper.IsObsoleteWithError(attributes);
                            if (isObsoleteWithError)
                            {
                                continue;
                            }

                            // Skip properties that cause exceptions at edit time
                            if (Application.isPlaying == false)
                            {
                                if (typeof(MeshFilter).IsAssignableFrom(declaringType))
                                {
                                    if (property.Name == "mesh")
                                    {
                                        continue;
                                    }
                                }

                                if (typeof(Renderer).IsAssignableFrom(declaringType))
                                {
                                    if (property.Name == "material" || property.Name == "materials")
                                    {
                                        continue;
                                    }
                                }
                            }


                            string propertyName = property.Name;

                            MethodInfo getMethod = property.GetGetMethod(true);
                            if (getMethod != null)
                            {
                                //MethodImplAttributes methodImplAttributes = getMethod.GetMethodImplementationFlags();
                                //if ((methodImplAttributes & MethodImplAttributes.InternalCall) != 0)
                                //{
                                //    continue;
                                //}


                                object objectValue = getMethod.Invoke(component, null);

                                WrappedVariable wrappedVariable = new WrappedVariable(property, objectValue);
                                componentScope.Properties.Add(wrappedVariable);
                            }
                        }
                    }

                    if ((flags & InfoFlags.Methods) == InfoFlags.Methods)
                    {
                        MethodInfo[] methodInfos = componentType.GetMethods(BINDING_FLAGS);
                        foreach (var methodInfo in methodInfos)
                        {
                            if (TypeUtility.IsPropertyMethod(methodInfo, componentType))
                            {
                                // Skip automatically generated getter/setter methods
                                continue;
                            }

                            MethodImplAttributes methodImplAttributes = methodInfo.GetMethodImplementationFlags();
                            if ((methodImplAttributes & MethodImplAttributes.InternalCall) != 0 && methodInfo.Name.StartsWith("INTERNAL_"))
                            {
                                // Skip any internal method if it also begins with INTERNAL_
                                continue;
                            }

                            WrappedMethod wrappedMethod = new WrappedMethod(methodInfo);
                            componentScope.Methods.Add(wrappedMethod);
                        }
                    }

                    description.Scopes.Add(componentScope);

                    componentType = componentType.BaseType;
                }

                response.Components.Add(description);
            }

            return response;
        }
    }
}