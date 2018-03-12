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

        public ComponentDescription()
        {

        }

        public ComponentDescription(BinaryReader br)
        {
            typeName = br.ReadString();
            instanceID = br.ReadInt32();
            int fieldCount = br.ReadInt32();
            for (int i = 0; i < fieldCount; i++)
            {
                fields.Add(WrappedVariable.Read(br));
            }
            int propertyCount = br.ReadInt32();
            for (int i = 0; i < propertyCount; i++)
            {
                properties.Add(WrappedVariable.Read(br));
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(typeName);
            bw.Write(instanceID);
            bw.Write(fields.Count);
            for (int i = 0; i < fields.Count; i++)
            {
                fields[i].Write(bw);
            }
            bw.Write(properties.Count);
            for (int i = 0; i < properties.Count; i++)
            {
                properties[i].Write(bw);
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
    }
}