using Crux.Core.Runtime;

namespace Crux.AnimatorDynamics.Runtime
{
    public static class Consts
    {
        private const string Slug = "Animator Dynamics/";
        
        internal const string ComponentRootPath = CoreConsts.ComponentRootPath + Slug;
        internal const string AssetRootPath = CoreConsts.AssetRootPath + Slug;

        internal const int ComponentRootOrder = CoreConsts.ComponentPackageOrder + 0;
        internal const int AssetRootOrder = CoreConsts.AssetPackageOrder + 0;
    }
}