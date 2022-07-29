namespace Alyas.Commerce.Search.Commands
{
    using System.Threading.Tasks;
    using Pipelines;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Core.Commands;

    public class PopulateAllProductsCategoryCommand : CommerceCommand
    {
        private readonly CommerceCommander _commerceCommander;

        public PopulateAllProductsCategoryCommand(CommerceCommander commerceCommander)
        {
            this._commerceCommander = commerceCommander;
        }

        public virtual async Task<CommerceCommand> Process(CommerceContext commerceContext, string catalogId)
        {
            await this._commerceCommander.Pipeline<IPopulateAllProductsCategoryPipeline>().Run(catalogId, commerceContext.PipelineContext);
            return this;
        }
    }
}
