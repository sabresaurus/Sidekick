using System;
using UnityEngine;
using System.Collections;
using System.IO;

namespace Sabresaurus.Sidekick
{
    public class UnityObjectDescription
    {
        //string typeName;
        Guid guid;
        string objectName;

        public Guid Guid
        {
            get
            {
                return guid;
            }
        }

        public string ObjectName
        {
            get
            {
                return objectName;
            }
        }

        public UnityObjectDescription(UnityEngine.Object sourceObject)
        {
            this.guid = ObjectMap.AddOrGetObject(sourceObject);
            this.objectName = sourceObject.name;
        }

        public UnityObjectDescription(BinaryReader br)
        {
            guid = new Guid(br.ReadString());
            objectName = br.ReadString();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(guid.ToString());
            bw.Write(objectName);
        }
    }
}
