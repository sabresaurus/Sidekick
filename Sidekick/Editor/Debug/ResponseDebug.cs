#if SIDEKICK_DEBUG
using UnityEngine;
using System.Collections;
using Sabresaurus.Sidekick.Responses;
using System.Text;

namespace Sabresaurus.Sidekick
{
    public static class ResponseDebug
    {
        public static string GetDebugStringForResponse(BaseResponse response)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(response.GetType().Name);

            if (response is GetObjectResponse)
            {
                GetObjectResponse gameObjectResponse = (GetObjectResponse)response;

                stringBuilder.AppendLine(gameObjectResponse.GameObjectName);
                foreach (var component in gameObjectResponse.Components)
                {
                    stringBuilder.Append(" ");
                    stringBuilder.AppendLine(component.TypeFullName);
                    foreach (var scope in component.Scopes)
                    {
                        foreach (var field in scope.Fields)
                        {
                            stringBuilder.Append("  ");
                            stringBuilder.Append(field.VariableName);
                            stringBuilder.Append(" ");
                            stringBuilder.Append(field.DataType);
                            stringBuilder.Append(" = ");
                            stringBuilder.Append(field.Value);
                            stringBuilder.AppendLine();
                        }
                        foreach (var property in scope.Properties)
                        {
                            stringBuilder.Append("  ");
                            stringBuilder.Append(property.VariableName);
                            stringBuilder.Append(" ");
                            stringBuilder.Append(property.DataType);
                            stringBuilder.Append(" = ");
                            stringBuilder.Append(property.Value);
                            stringBuilder.AppendLine();
                        }
                        foreach (var method in scope.Methods)
                        {
                            stringBuilder.Append("  ");
                            stringBuilder.Append(method.MethodName);
                            stringBuilder.Append(" ");
                            stringBuilder.Append(method.ReturnType);
                            stringBuilder.Append(" ");
                            stringBuilder.Append(method.ParameterCount);
                            stringBuilder.Append(" ");
                            if (method.Parameters.Count > 0)
                            {
                                stringBuilder.Append(method.Parameters[0].DataType);
                            }
                            stringBuilder.AppendLine();
                        }
                    }
                }
            }
            else if (response is GetHierarchyResponse)
            {
                GetHierarchyResponse hierarchyResponse = (GetHierarchyResponse)response;
                foreach (var scene in hierarchyResponse.Scenes)
                {
                    stringBuilder.AppendLine(scene.SceneName);

                    foreach (var item in scene.HierarchyNodes)
                    {
                        for (int i = 0; i < item.Depth + 1; i++)
                        {
                            stringBuilder.Append(" ");
                        }
                        stringBuilder.Append(item.ObjectName);
                        stringBuilder.Append(" - ");
                        stringBuilder.Append(item.ActiveInHierarchy);
                        stringBuilder.AppendLine();
                    }
                }
            }

            return stringBuilder.ToString();
        }
    }
}
#endif