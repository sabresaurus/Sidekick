using UnityEngine;
using System.Collections;
using System.IO;

namespace Sabresaurus.Sidekick
{
    public class UnityObjectDescription
    {
        //string typeName;
        int instanceID;
        string objectName;

        public int InstanceID
        {
            get
            {
                return instanceID;
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
            this.instanceID = sourceObject.GetInstanceID();
            this.objectName = sourceObject.name;
        }

        public UnityObjectDescription(BinaryReader br)
        {
            instanceID = br.ReadInt32();
            objectName = br.ReadString();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(instanceID);
            bw.Write(objectName);
        }
    }
}
