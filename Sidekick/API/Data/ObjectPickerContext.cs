using System;
using System.IO;

public class ObjectPickerContext : BaseBinarySerializedData
{
    Guid componentGuid = Guid.Empty;
    int argumentIndex = -1;

    public Guid ComponentGuid
    {
        get
        {
            return componentGuid;
        }
    }

    public int ArgumentIndex
    {
        get
        {
            return argumentIndex;
        }
    }

    public ObjectPickerContext(Guid componentGuid)
    {
        this.componentGuid = componentGuid;
    }

    public ObjectPickerContext(int argumentIndex)
    {
        this.argumentIndex = argumentIndex;
    }

    #region Binary Serialization
    public ObjectPickerContext(BinaryReader br) : base(br)
    {
        componentGuid = new Guid(br.ReadString());
        argumentIndex = br.ReadInt32();
    }

    public override void Write(BinaryWriter bw)
    {
        bw.Write(componentGuid.ToString());
        bw.Write(argumentIndex);
    }
    #endregion
}