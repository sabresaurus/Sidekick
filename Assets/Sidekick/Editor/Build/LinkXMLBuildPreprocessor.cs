// Temporarily commented out as WIP
﻿//using System;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEditor.Build;
//using UnityEngine;

//class LinkXMLBuildPreprocessor : IPreprocessBuild
//{
//    public int callbackOrder { get { return 0; } }

//    public void OnPreprocessBuild(BuildTarget target, string path)
//    {
//        if(Debug.isDebugBuild)
//        {
//            Debug.Log("Preprocessing link.xml file");
//            List<Type> typesToProtect = LinkXMLFactory.GetUnityComponentTypes();
//            typesToProtect.AddRange(LinkXMLFactory.DEFAULT_TYPES);
//            LinkXMLFactory.Generate(typesToProtect);
//        }
//        else
//        {
//            Debug.Log("Not preprocessing link.xml file (Release build)");
//            LinkXMLFactory.Generate(new List<Type>());
//        }
//    }
//}