namespace Alyas.Commerce.Search
{
    using System.Reflection;
    using Microsoft.Extensions.DependencyInjection;
    using Pipelines;
    using Pipelines.Blocks;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Plugin.Catalog;
    using Sitecore.Commerce.Plugin.Workflow;
    using Sitecore.Framework.Configuration;
    using Sitecore.Framework.Pipelines.Definitions.Extensions;

    public class ConfigureSitecore : IConfigureSitecore
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.RegisterAllPipelineBlocks(assembly);

            services.Sitecore().Pipelines(config => config

                .ConfigurePipeline<IConfigureServiceApiPipeline>(configure => configure.Add<ConfigureServiceApiBlock>())
                .ConfigurePipeline<IAssociateSellableItemToParentPipeline>(configure =>
                {
                    configure.Add<AddProductToAllProductsCategory>().After<AssociateItemToParentBlock>();
                })
                .ConfigurePipeline<IDeleteRelationshipPipeline>(configure =>
                {
                    configure.Add<RemoveProductFromAllProductsCategory>().After<UpdateCatalogHierarchyBlock>();
                })
                .AddPipeline<IPopulateAllProductsCategoryPipeline, PopulateAllProductsCategoryPipeline>(configure => configure.Add<PopulateAllProductsCategoryBlock>())
               );
            
            services.RegisterAllCommands(assembly);
        }
    }
}