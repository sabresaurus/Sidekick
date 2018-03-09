using UnityEngine;
using System.Collections;
using System.Text;
using System.Reflection;

namespace Sabresaurus.Sidekick.Requests
{
    public class GetGameObjectRequest
    {
        string response;

        public string Response
        {
            get { return response; }
        }

        public GetGameObjectRequest(string gameObjectPath)
        {
            Transform foundTransform = TransformHelper.GetFromPath(gameObjectPath);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(foundTransform.name);
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Component[] components = foundTransform.GetComponents<Component>();
            foreach (Component component in components)
            {
                stringBuilder.Append("  ");
                stringBuilder.AppendLine(component.GetType().Name);
                FieldInfo[] fields = component.GetType().GetFields(bindingFlags);
                foreach (FieldInfo field in fields)
                {
                    stringBuilder.Append("    ");
                    stringBuilder.Append(field.Name);
					stringBuilder.Append(" = ");
                    object objectValue = field.GetValue(component);
                    if(objectValue != null)
                    {
                        stringBuilder.Append(objectValue.ToString());
                    }
                    stringBuilder.AppendLine();
                }
            }

            response = stringBuilder.ToString();
        }
    }
}
