using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public class TestScriptableObject : ScriptableObject
	{
        [SerializeField]
        int testInt = 1234;

        void IncrementTestInt(int amount = 1)
        {
            testInt += amount;
        }
	}
}
