using UnityEngine;
using System.Collections.Generic;

#pragma warning disable 0414
/// <summary>
/// Includes common data types that Sidekick should test against commonly
/// </summary>
public class DataTestScript : MonoBehaviour
{
    public enum TestEnum { Foo, Bar };

    [SerializeField] string testString = "Hello World";
    [SerializeField] bool testBool = true;
    [SerializeField] int testInt = 2147483647;
    [SerializeField] long testLong = 12345678912345678;
    [SerializeField] float testFloat = 0.1234f;
    [SerializeField] double testDouble = 0.12345678f;
    [SerializeField] Vector2 testVector2 = new Vector2(1, 2);
    [SerializeField] Vector3 testVector3 = new Vector3(1, 2, 3);
    [SerializeField] Vector4 testVector4 = new Vector4(1, 2, 3, 4);
    [SerializeField] Vector2Int testVectorInt2 = new Vector2Int(1, 2);
    [SerializeField] Vector3Int testVectorInt3 = new Vector3Int(1, 2, 3);
    [SerializeField] Bounds testBounds = new Bounds(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
    [SerializeField] BoundsInt testBoundsInt = new BoundsInt(new Vector3Int(1, 2, 3), new Vector3Int(4, 5, 6));
    [SerializeField] Quaternion testQuaternion = Quaternion.identity;
    [SerializeField] Rect testRect = new Rect(10, 10, 100, 60);
    [SerializeField] RectInt testRectInt = new RectInt(10, 10, 100, 60);
    [SerializeField] Gradient testGradient = new Gradient();
    [SerializeField] Color testColor = Color.blue;
    [SerializeField] Color32 testColor32 = Color.blue;
    [SerializeField] TestEnum testEnum = TestEnum.Foo;
    [SerializeField] int[] testArray = { 1, 2, 3, 4 };
    [SerializeField] List<int> testList = new List<int>() { 5, 6, 7, 8 };
    [SerializeField] char character = 'a';
    [SerializeField] AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    readonly string readonlyString = "readonly";
    const string constString = "const";
    static string staticString = "static";

    public int AutoImplementedProperty
    {
        get;
        set;
    }

    public TestEnum TestEnumAutoProperty
    {
        get;
        set;
    }

    public List<int> TestList
    {
        get
        {
            return testList;
        }

        set
        {
            testList = value;
        }
    }
}
