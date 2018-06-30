using System.IO;
using System;
using System.Reflection;

namespace Sabresaurus.Sidekick
{
    /// <summary>
    /// Wraps a method parameter so that it can be sent over the network.
    /// </summary>
    public class WrappedParameter : WrappedBaseObject
    {
        public WrappedParameter(ParameterInfo parameterInfo)
            : base(parameterInfo.Name, parameterInfo.ParameterType, true)
        {
        }

        public WrappedParameter(BinaryReader br)
            : base(br)
        {
        }

        public override void Write(BinaryWriter bw)
        {
            base.Write(bw);
            // Add any parameter specific content here
        }

        public override bool Equals(object obj)
        {
            if (obj is WrappedParameter)
            {
                WrappedParameter otherParameter = (WrappedParameter)obj;

                if (this.variableName != otherParameter.variableName
                    || this.attributes != otherParameter.attributes
                    || this.dataType != otherParameter.dataType
                  )
                {
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

		public override int GetHashCode()
		{
            return variableName.GetHashCode();
		}
	}
}