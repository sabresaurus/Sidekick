[System.Serializable]
public class HierarchyNode
{
    public string ObjectName
    {
        get;
        set;
    }

    public int Depth
    {
        get;
        set;
    }

    public bool ActiveInHierarchy
    {
        get;
        set;
    }
}