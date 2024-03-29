﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick
{
	public static class TypeUtility
	{
		public static object GetDefaultValue(Type type)
		{
            Assert.IsNotNull(type);
                
			return type.IsValueType ? Activator.CreateInstance(type) : null;
		}

		public static string GetVisibilityName(FieldInfo field)
		{
			string visibility;

			if (field.IsPublic)
			{
				visibility = "public";
			}
			else if (field.IsAssembly)
			{
				visibility = "internal";
			}
			else if (field.IsFamily)
			{
				visibility = "protected";
			}
			else if (field.IsFamilyOrAssembly)
			{
				visibility = "protected internal";
			}
			else if (field.IsFamilyAndAssembly)
			{
				visibility = "private protected";
			}
			else
			{
				visibility = "private";
			}

			return visibility;
		}

		public static string NameForType(Type type)
		{
			// See https://msdn.microsoft.com/en-us/library/ya5y69ds.aspx
			if (type == null)
			{
				return "null";
			}
			else if(type == typeof(void))
			{
				return "void";
			}
			else if(type == typeof(Boolean))
			{
				return "bool";
			}
			else if(type == typeof(Byte))
			{
				return "byte";
			}
			else if(type == typeof(SByte))
			{
				return "sbyte";
			}
			else if(type == typeof(Char))
			{
				return "char";
			}
			else if(type == typeof(Decimal))
			{
				return "decimal";
			}
			else if(type == typeof(Double))
			{
				return "double";
			}
			else if(type == typeof(Single))
			{
				return "float";
			}
			else if(type == typeof(Int32))
			{
				return "int";
			}
			else if(type == typeof(UInt32))
			{
				return "uint";
			}
			else if(type == typeof(Int64))
			{
				return "long";
			}
			else if(type == typeof(UInt64))
			{
				return "ulong";
			}
			else if(type == typeof(System.Object))
			{
				return "object";
			}
			else if(type == typeof(Int16))
			{
				return "short";
			}
			else if(type == typeof(UInt16))
			{
				return "ushort";
			}
			else if(type == typeof(String))
			{
				return "string";
			}
			else
			{
				bool isArray = type.IsArray;
				bool isGenericList = IsGenericList(type);
				bool isGenericDictionary  = IsGenericDictionary(type);
				if (isGenericDictionary)
				{
					Type[] elementTypes = GetElementTypes(type);
					string[] elementTypeNames = elementTypes.Select(type => NameForType(type)).ToArray();
					return $"Dictionary<{elementTypeNames[0]},{elementTypeNames[1]}>";
				}

				if (isArray || isGenericList)
				{
					Type elementType = GetFirstElementType(type);
					string elementTypeName = NameForType(elementType);
					if (isGenericList)
					{
						return "List<" + elementTypeName + ">";
					}
					else
					{
						return elementTypeName + "[]";
					}
				}

				return type.Name;
			}
		}
		public static bool IsGenericList(Type type)
		{
			// Check if it's a List<T>
			if(type.IsGenericType)
			{
				Type genericDefinition = type.GetGenericTypeDefinition();
				if(genericDefinition == typeof(List<>))
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsGenericDictionary(Type type)
		{
			// Check if it's a List<T>
			if(type.IsGenericType)
			{
				Type genericDefinition = type.GetGenericTypeDefinition();
				if(genericDefinition == typeof(Dictionary<,>))
				{
					return true;
				}
			}

			return false;
		}
		
		public static Type GetFirstElementType(Type type)
		{
			if(type.IsArray)
			{
				return type.GetElementType();
			}

			Type[] genericArguments = type.GetGenericArguments();

			if(genericArguments.Length == 1)
			{
				return genericArguments[0];
			}
			else
			{
                // Not array or list
                return type;
			}
		}

		public static Type[] GetElementTypes(Type type)
		{
			if(type.IsArray)
			{
				return new[] {type.GetElementType()};
			}

			Type[] genericArguments = type.GetGenericArguments();

			if(genericArguments != null)
			{
				return genericArguments;
			}

			return null;
		}

		public static bool IsBackingField(FieldInfo fieldInfo, Type parentType)
		{
			BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

			string fieldName = fieldInfo.Name;
			// Backing fields typically are of the format <PROP_NAME>k__BackingField
			if(fieldName.StartsWith("<") && fieldName.EndsWith(">k__BackingField"))
			{
				string strippedName = fieldName.Remove(fieldName.Length - ">k__BackingField".Length).Remove(0,1);

				// Make sure there's actually a property with the property name
				if(parentType.GetProperty(strippedName, bindingFlags) != null)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}

		public static bool IsPropertyMethod(MethodInfo methodInfo, Type parentType)
		{
			BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

			string methodName = methodInfo.Name;
			// The compiler generates methods for getters and setters with a prefix
			if(methodName.StartsWith("get_") || methodName.StartsWith("set_"))
			{
				// Check that no property exists with the name after the prefix
				// Don't use SpecialName here as compilers aren't required to populate it

				ParameterInfo[] parameterInfos = methodInfo.GetParameters();

				Type propertyType;
				if (methodName.StartsWith("get_"))
				{
					if (parameterInfos.Length != 0)
					{
						return false;
					}
					propertyType = methodInfo.ReturnType;
				}
				else
				{
					if (parameterInfos.Length != 1)
					{
						return false;
					}
					propertyType = parameterInfos[0].ParameterType;
				}
				PropertyInfo propertyInfo = parentType.GetProperty(methodName.Substring(4), bindingFlags, null, propertyType, Type.EmptyTypes, null);
				if (propertyInfo == null)
				{
					return false;
				}

				if (propertyInfo.GetIndexParameters().Length != 0)
				{
					// Indexer, takes parameters unlike ordinary properties so don't treat it like a normal property
					return false;
				}

				return true;
			}

			return false;
		}

        /// <summary>
        /// If supplied with a List<Foo> and newElementType of Bar it will convert it to List<Bar>. Also supports arrays and will work if it's not a collection
        /// </summary>
        public static object ChangeElementType(object objectOrCollection, Type newElementType)
        {
            if (objectOrCollection is IList)
            {
                // TODO: Investigate if this array copying could be simplified
                IList sourceList = (IList)objectOrCollection;
                int count = sourceList.Count;
                if (objectOrCollection.GetType().IsArray)
                {
                    Type arrayType = newElementType.MakeArrayType();
                    // Copying to an array
                    object newArray = Activator.CreateInstance(arrayType, count);
                    for (int i = 0; i < count; i++)
                    {
                        ((Array)newArray).SetValue(Convert.ChangeType(sourceList[i], newElementType), i);
                    }
                    return newArray;
                }
                else
                {
                    Type listType = typeof(List<>).MakeGenericType(newElementType);

                    object newList = Activator.CreateInstance(listType, 0);
                    for (int i = 0; i < count; i++)
                    {
                        ((IList)newList).Add(Convert.ChangeType(sourceList[i], newElementType));
                    }
                    return newList;
                }
            }
            else
            {
                return Convert.ChangeType(objectOrCollection, newElementType);
            }
        }

        public static string GetTooltip(FieldInfo field, VariablePane.VariableAttributes variableAttributes)
        {
	        string tooltip = "";
	        object[] customAttributes = field.GetCustomAttributes(false);
	        foreach (var customAttribute in customAttributes)
	        {
		        tooltip += $"[{customAttribute.GetType().Name.RemoveEnd("Attribute")}]\n";
	        }

	        if ((variableAttributes & VariablePane.VariableAttributes.Constant) != 0)
	        {
		        tooltip += $"{GetVisibilityName(field)} const {NameForType(field.FieldType)} {field.Name}";
	        }
	        else if ((variableAttributes & VariablePane.VariableAttributes.Static) != 0)
	        {
		        tooltip += $"{GetVisibilityName(field)} static {NameForType(field.FieldType)} {field.Name}";
	        }
	        else
	        {
		        tooltip += $"{GetVisibilityName(field)} {NameForType(field.FieldType)} {field.Name}";    
	        }
	        
	        return tooltip;
        }

        public static string GetTooltip(PropertyInfo propertyInfo, VariablePane.VariableAttributes variableAttributes)
        {
	        string tooltip = "";
	        object[] customAttributes = propertyInfo.GetCustomAttributes(false);
	        foreach (var customAttribute in customAttributes)
	        {
		        tooltip += $"[{customAttribute.GetType().Name.RemoveEnd("Attribute")}]\n";
	        }

	        string methodsSuffix;
	        if ((variableAttributes & VariablePane.VariableAttributes.ReadOnly) != 0)
	        {
		        methodsSuffix = "{ get; }";
	        }
	        else if ((variableAttributes & VariablePane.VariableAttributes.WriteOnly) != 0)
	        {
		        methodsSuffix = "{ set; }";
	        }
	        else
	        {
		        methodsSuffix = "{ get; set; }";
	        }

	        if ((variableAttributes & VariablePane.VariableAttributes.Static) != 0)
	        {
		        tooltip += $"static {NameForType(propertyInfo.PropertyType)} {propertyInfo.Name} {methodsSuffix}";
	        }
	        else
	        {
		        tooltip += $"{NameForType(propertyInfo.PropertyType)} {propertyInfo.Name} {methodsSuffix}";
	        }
	        
	        return tooltip;
        }

        public static string GetMethodIdentifier(MethodInfo methodInfo)
        {
	        return methodInfo.DeclaringType.FullName + "." 
	                                                 + methodInfo.Name 
	                                                 + string.Join(",",methodInfo.GetParameters().Select(item => item.Name)) 
	                                                 + string.Join(",",methodInfo.GetGenericArguments().Select(item => item.Name));
        }

        public static bool IsNull(object o)
        {
	        if (o == null)
		        return true;

	        // UnityEngine.Object overrides the == operator so we need to cast those objects to see if the backing
	        // native object has been destroyed 
	        if (o is Object unityObject && unityObject == null)
		        return true;
	        
	        return false;
        }
        
        public static bool IsNotNull(object o)
        {
	        return !IsNull(o);
        }
	}
}