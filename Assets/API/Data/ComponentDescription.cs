using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Sabresaurus.Sidekick
{
	public class ComponentDescription
    {
        string typeName;
        int instanceID;
        List<WrappedVariable> fields = new List<WrappedVariable>();
        List<WrappedVariable> properties = new List<WrappedVariable>();
        List<WrappedMethod> methods = new List<WrappedMethod>();

        public ComponentDescription()
        {

        }

        public ComponentDescription(BinaryReader br)
        {
            typeName = br.ReadString();
            instanceID = br.ReadInt32();
            // Fields
            int fieldCount = br.ReadInt32();
            for (int i = 0; i < fieldCount; i++)
            {
                fields.Add(WrappedVariable.Read(br));
            }
            // Properties
            int propertyCount = br.ReadInt32();
            for (int i = 0; i < propertyCount; i++)
            {
                properties.Add(WrappedVariable.Read(br));
            }
            // Methods
            int methodCount = br.ReadInt32();
            for (int i = 0; i < methodCount; i++)
            {
                methods.Add(WrappedMethod.Read(br));
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(typeName);
            bw.Write(instanceID);
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

        public string TypeName
        {
            get
            {
                return typeName;
            }

            set
            {
                typeName = value;
            }
        }

        public int InstanceID
        {
            get
            {
                return instanceID;
            }

            set
            {
                instanceID = value;
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