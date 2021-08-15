using System;
using System.Runtime.InteropServices;
using Unity.Entities;

namespace Sabresaurus.Sidekick
{
    public unsafe class ECSAccess
    {
        public static object GetComponentData(EntityManager entityManager, Entity entity, ComponentType componentType)
        {
            EntityDataAccess* dataAccess = entityManager.GetCheckedEntityDataAccess();
            EntityComponentStore* entityComponentStore = dataAccess->EntityComponentStore;
            byte* ptr = entityComponentStore->GetComponentDataWithTypeRO(entity, componentType.TypeIndex);

            return Marshal.PtrToStructure((IntPtr) ptr, componentType.GetManagedType());
        }

        public static void SetComponentData(EntityManager entityManager, Entity entity, ComponentType componentType, object componentData) 
            => SetComponentData(entityManager, entity, componentType.TypeIndex, componentData);

        public static void SetComponentData(EntityManager entityManager, Entity entity, int typeIndex, object componentData)
        {
            EntityDataAccess* dataAccess = entityManager.GetCheckedEntityDataAccess();

            if (!dataAccess->IsInExclusiveTransaction)
                dataAccess->DependencyManager->CompleteReadAndWriteDependency(typeIndex);

            EntityComponentStore* entityComponentStore = dataAccess->EntityComponentStore;

            byte* ptr = entityComponentStore->GetComponentDataWithTypeRW(entity, typeIndex, entityComponentStore->GlobalSystemVersion);

            Marshal.StructureToPtr(componentData, (IntPtr) ptr, false);
        }
    }
}