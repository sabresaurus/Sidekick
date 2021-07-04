﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Sabresaurus.Sidekick.Requests;
using UnityEngine.Assertions;

namespace Sabresaurus.Sidekick
{
	public static class TypeUtility
	{
		public static object GetDefaultValue(Type type)
		{
            Assert.IsNotNull(type);
                
			return type.IsValueType ? Activator.CreateInstance(type) : null;
		}

		public static string NameForType(Type type)
		{
			// See https://msdn.microsoft.com/en-us/library/ya5y69ds.aspx
			if(type == typeof(void))
			{
				return "void";
			}
			else if(type == typeof(System.Boolean))
			{
				return "bool";
			}
			else if(type == typeof(System.Byte))
			{
				return "byte";
			}
			else if(type == typeof(System.SByte))
			{
				return "sbyte";
			}
			else if(type == typeof(System.Char))
			{
				return "char";
			}
			else if(type == typeof(System.Decimal))
			{
				return "decimal";
			}
			else if(type == typeof(System.Double))
			{
				return "double";
			}
			else if(type == typeof(System.Single))
			{
				return "float";
			}
			else if(type == typeof(System.Int32))
			{
				return "int";
			}
			else if(type == typeof(System.UInt32))
			{
				return "uint";
			}
			else if(type == typeof(System.Int64))
			{
				return "long";
			}
			else if(type == typeof(System.UInt64))
			{
				return "ulong";
			}
			else if(type == typeof(System.Object))
			{
				return "object";
			}
			else if(type == typeof(System.Int16))
			{
				return "short";
			}
			else if(type == typeof(System.UInt16))
			{
				return "ushort";
			}
			else if(type == typeof(System.String))
			{
				return "string";
			}
			else
			{
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

		public static Type GetElementType(Type type)
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

		public static bool IsBackingField(FieldInfo fieldInfo, Type parentType)
		{
			string fieldName = fieldInfo.Name;
			// Backing fields typically are of the format <PROP_NAME>k__BackingField
			if(fieldName.StartsWith("<") && fieldName.EndsWith(">k__BackingField"))
			{
				string strippedName = fieldName.Remove(fieldName.Length - ">k__BackingField".Length).Remove(0,1);

				// Make sure there's actually a property with the property name
				if(parentType.GetProperty(strippedName) != null)
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
                if(parentType.GetProperty(methodName.Substring(4), bindingFlags) != null)
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
	}
}