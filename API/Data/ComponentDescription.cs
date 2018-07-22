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
        List<ComponentScope> scopes = new List<ComponentScope>();

        public ComponentScope BehaviourScope
        {
            get
            {
                foreach (ComponentScope scope in scopes)
                {
                    if(scope.TypeShortName == "Behaviour")
                    {
                        return scope;
                    }
                }
                return null;
            }
        }

        public ComponentDescription(object component)
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
            int scopeCount = br.ReadInt32();
            for (int i = 0; i < scopeCount; i++)
            {
                scopes.Add(new ComponentScope(br));
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(typeFullName);
            bw.Write(typeShortName);
            bw.Write(guid.ToString());
            bw.Write(scopes.Count);
            for (int i = 0; i < scopes.Count; i++)
            {
                scopes[i].Write(bw);
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

        public List<ComponentScope> Scopes
        {
            get
            {
                return scopes;
            }

            set
            {
                scopes = value;
            }
        }
    }
}