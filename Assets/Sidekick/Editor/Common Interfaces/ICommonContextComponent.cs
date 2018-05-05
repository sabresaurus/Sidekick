using UnityEngine;
using System.Collections;

namespace Sabresaurus.Sidekick
{
    public interface ICommonContextComponent
    {
        void OnEnable(CommonContext commonContext);

        void OnDisable();
    }
}