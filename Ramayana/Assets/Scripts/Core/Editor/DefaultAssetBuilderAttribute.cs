// Marks a static void Build(AssetBuilderReport) method as a default asset builder for the "All" menu.
// Order convention: Core 0, foundation systems 10 to 90, gameplay features 100 and up. Lower runs first.
using System;

namespace JourneyToLanka.Core.EditorTools
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class DefaultAssetBuilderAttribute : Attribute
    {
        public DefaultAssetBuilderAttribute(int order)
        {
            Order = order;
        }

        public int Order { get; }
    }
}
