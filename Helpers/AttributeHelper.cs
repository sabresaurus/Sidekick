using UnityEngine;
using System.Collections;

namespace Sabresaurus.Sidekick
{
	public static class AttributeHelper
	{
		public static bool IsSerializable(object[] customAttributes)
		{
			if(customAttributes == null || customAttributes.Length == 0)
			{
				return false;
			}
			// Find any attributes that are SerializeField
			for (int i = 0; i < customAttributes.Length; i++) 
			{
				if(customAttributes[i] is SerializeField)
				{
					return true;
				}
			}
			// No attributes found, assume it can't be serialized
			return false;
		}

        public static bool IsObsolete(object[] customAttributes)
        {
            if (customAttributes == null || customAttributes.Length == 0)
            {
                return false;
            }
            // Find any attributes that are Obsolete
            for (int i = 0; i < customAttributes.Length; i++)
            {
                if (customAttributes[i] is System.ObsoleteAttribute)
                {
                    return true;
                }
            }
            // No attributes found, assume it's ok
            return false;
        }

		public static bool IsObsoleteWithError(object[] customAttributes)
		{
			if(customAttributes == null || customAttributes.Length == 0)
			{
				return false;
			}
			// Find any attributes that are Obsolete
			for (int i = 0; i < customAttributes.Length; i++) 
			{
				if(customAttributes[i] is System.ObsoleteAttribute)
				{
					// If the attribute is marked to trigger an error
					if(((System.ObsoleteAttribute)customAttributes[i]).IsError)
					{
						return true;
					}
				}
			}
            // No attributes found, assume it's ok
			return false;
		}
	}
}