namespace Alyas.Commerce.Search.Pipelines
{
    using Microsoft.Extensions.Logging;
    using Sitecore.Commerce.Core;
    using Sitecore.Framework.Pipelines;

    public class PopulateAllProductsCategoryPipeline : CommercePipeline<string, bool>, IPopulateAllProductsCategoryPipeline
    {
        public PopulateAllProductsCategoryPipeline(IPipelineConfiguration<IPopulateAllProductsCategoryPipeline> configuration, ILoggerFactory loggerFactory) : base(configuration, loggerFactory)
        {
        }
    }
}
