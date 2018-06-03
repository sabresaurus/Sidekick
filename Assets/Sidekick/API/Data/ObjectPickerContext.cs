using System;
using System.IO;

public class ObjectPickerContext : BaseBinarySerializedData
{
    Guid componentGuid;

    public Guid ComponentGuid
    {
        get
        {
            return componentGuid;
        }
    }

    public ObjectPickerContext(Guid componentGuid)
    {
        this.componentGuid = componentGuid;
    }

    #region Binary Serialization
    public ObjectPickerContext(BinaryReader br) : base(br)
    {
        componentGuid = new Guid(br.ReadString());
    }

    public override void Write(BinaryWriter bw)
    {
        bw.Write(componentGuid.ToString());
    }
    #endregion
}