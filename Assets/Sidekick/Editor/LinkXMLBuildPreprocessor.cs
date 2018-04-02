using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

class LinkXMLBuildPreprocessor : IPreprocessBuild
{
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildTarget target, string path)
    {
        Debug.Log("Preprocessing link.xml file");
        LinkXMLFactory.Generate(LinkXMLFactory.DEFAULT_TYPES);
    }
}