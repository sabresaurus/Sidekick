using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

public static class LinkXMLFactory
{
    public static void Generate(Type[] types)
    {
        Dictionary<Assembly, List<Type>> assemblyTypes = new Dictionary<Assembly, List<Type>>();
        foreach (var type in types)
        {
            Assembly assembly = type.Assembly;
            if(!assemblyTypes.ContainsKey(assembly))
            {
                assemblyTypes.Add(assembly, new List<Type>());
            }

            assemblyTypes[assembly].Add(type);
        }

        //foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        //{
        //    //Debug.Log(assembly.FullName);
        //}
        // Generate a link.xml
        var settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;
        settings.Indent = true;

        using (StringWriter sw = new StringWriter())
        {
            using (XmlWriter writer = XmlWriter.Create(sw, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("linker");

                foreach (var mapping in assemblyTypes)
                {
                    writer.WriteStartElement("assembly");
                    writer.WriteAttributeString("fullname", new AssemblyName(mapping.Key.FullName).Name);

                    foreach (var item in mapping.Value)
                    {
                        writer.WriteStartElement("type");
                        writer.WriteAttributeString("fullname", item.FullName);
                        writer.WriteAttributeString("preserve", "all");
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
            Debug.Log(sw.ToString());
        }
    }
}