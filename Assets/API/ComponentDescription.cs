using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Sabresaurus.Sidekick
{
	public class ComponentDescription
    {
        string typeName;
        List<WrappedVariable> fields = new List<WrappedVariable>();

        public ComponentDescription()
        {

        }

        public ComponentDescription(BinaryReader br)
        {
            typeName = br.ReadString();
            int fieldCount = br.ReadInt32();
            for (int i = 0; i < fieldCount; i++)
            {
                fields.Add(WrappedVariable.Read(br));
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(typeName);
            bw.Write(fields.Count);
            for (int i = 0; i < fields.Count; i++)
            {
                fields[i].Write(bw);
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
    }
}