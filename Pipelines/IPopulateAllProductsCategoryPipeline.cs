namespace Alyas.Commerce.Search.Pipelines
{
    using Sitecore.Commerce.Core;
    using Sitecore.Framework.Pipelines;

    public interface IPopulateAllProductsCategoryPipeline : IPipeline<string, bool, CommercePipelineExecutionContext>
    {
    }
}
