#if ECS_EXISTS
using Unity.Entities;
#endif

namespace Sabresaurus.Sidekick
{
    public class ECSContext
    {
#if ECS_EXISTS
        public EntityManager EntityManager;
        public Entity Entity;
        public ComponentType ComponentType;
#endif        
    }
}
