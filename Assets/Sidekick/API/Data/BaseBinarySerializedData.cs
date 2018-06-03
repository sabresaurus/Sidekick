using System.IO;

public abstract class BaseBinarySerializedData
{
    // Plain constructor for non-binary serialisation to build on
    public BaseBinarySerializedData()
    {

    }

    #region Binary Serialization
    public BaseBinarySerializedData(BinaryReader br)
    {
        // To be implemented by sub classes
    }

    public abstract void Write(BinaryWriter bw);
    #endregion
}