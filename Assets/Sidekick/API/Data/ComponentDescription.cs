using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick
{
	public class ComponentDescription
    {
        string typeFullName;
        string typeShortName;
        Guid guid;
        List<WrappedVariable> fields = new List<WrappedVariable>();
        List<WrappedVariable> properties = new List<WrappedVariable>();
        List<WrappedMethod> methods = new List<WrappedMethod>();

        public ComponentDescription(Object component)
        {
            Type componentType = component.GetType();
            this.typeFullName = componentType.FullName;
            this.typeShortName = componentType.Name;
            this.guid = ObjectMap.AddOrGetObject(component);
        }

        public ComponentDescription(BinaryReader br)
        {
            typeFullName = br.ReadString();
            typeShortName = br.ReadString();
            guid = new Guid(br.ReadString());
            // Fields
            int fieldCount = br.ReadInt32();
            for (int i = 0; i < fieldCount; i++)
            {
                fields.Add(new WrappedVariable(br));
            }
            // Properties
            int propertyCount = br.ReadInt32();
            for (int i = 0; i < propertyCount; i++)
            {
                properties.Add(new WrappedVariable(br));
            }
            // Methods
            int methodCount = br.ReadInt32();
            for (int i = 0; i < methodCount; i++)
            {
                methods.Add(new WrappedMethod(br));
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(typeFullName);
            bw.Write(typeShortName);
            bw.Write(guid.ToString());
            bw.Write(fields.Count);
            // Fields
            for (int i = 0; i < fields.Count; i++)
            {
                fields[i].Write(bw);
            }
            // Properties
            bw.Write(properties.Count);
            for (int i = 0; i < properties.Count; i++)
            {
                properties[i].Write(bw);
            }
            // Methods
            bw.Write(methods.Count);
            for (int i = 0; i < methods.Count; i++)
            {
                methods[i].Write(bw);
            }
        }

        public string TypeFullName
        {
            get
            {
                return typeFullName;
            }
        }

        public string TypeShortName
        {
            get
            {
                return typeShortName;
            }
        }

        public Guid Guid
        {
            get
            {
                return guid;
            }
        }

        public List<WrappedVariable> Fields
        {
            get
            {
                return fields;
            }

            set
            {
                fields = value;
            }
        }

        public List<WrappedVariable> Properties
        {
            get
            {
                return properties;
            }

            set
            {
                properties = value;
            }
        }

        public List<WrappedMethod> Methods
        {
            get
            {
                return methods;
            }

            set
            {
                methods = value;
            }
        }
    }
}