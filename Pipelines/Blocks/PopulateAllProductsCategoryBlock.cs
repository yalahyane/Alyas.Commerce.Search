namespace Alyas.Commerce.Search.Pipelines.Blocks
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Policies;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Plugin.Catalog;
    using Sitecore.Framework.Pipelines;

    public class PopulateAllProductsCategoryBlock : PipelineBlock<string, bool, CommercePipelineExecutionContext>
    {
        private readonly CommerceCommander _commerceCommander;
        private readonly List<string> _addedProducts = new List<string>();
        public PopulateAllProductsCategoryBlock(CommerceCommander commerceCommander)
        {
            this._commerceCommander = commerceCommander;
        }

        public override async Task<bool> Run(string catalogId, CommercePipelineExecutionContext context)
        {
            var allProductsCategoryPolicy = context.GetPolicy<AllProductsCategoryPolicy>();
            var shopCategoryId = $"{CommerceEntity.IdPrefix<Category>()}{catalogId}-{allProductsCategoryPolicy.ShopCategoryName}";
            var allProductsCategoryId = $"{CommerceEntity.IdPrefix<Category>()}{catalogId}-{allProductsCategoryPolicy.AllProductsCategoryName}";
            var allProducts = await this._commerceCommander.Pipeline<IFindEntitiesInListPipeline>().Run(new FindEntitiesInListArgument(typeof(SellableItem), $"CategoryToSellableItem-{allProductsCategoryId.SimplifyEntityName()}", 0, int.MaxValue) {LoadEntities = false}, context).ConfigureAwait(false);
            await this.AddCategoryProducts($"{CommerceEntity.IdPrefix<Catalog>()}{catalogId}", shopCategoryId, allProductsCategoryId, allProducts.EntityReferences, context);
            return true;
        }

        public async Task  AddCategoryProducts(string catalogId, string categoryId, string allProductsCategoryId, IList<ListEntityReference> allProducts,  CommercePipelineExecutionContext context)
        {
            var subCategories = await this._commerceCommander.Pipeline<IFindEntitiesInListPipeline>().Run(new FindEntitiesInListArgument(typeof(Category), $"CategoryToCategory-{categoryId.SimplifyEntityName()}", 0, int.MaxValue), context).ConfigureAwait(false);
            foreach (var subCategory in subCategories.List.Items)
            {
                await AddCategoryProducts(catalogId, subCategory.Id, allProductsCategoryId, allProducts, context);
            }
            var products = await this._commerceCommander.Pipeline<IFindEntitiesInListPipeline>().Run(new FindEntitiesInListArgument(typeof(Category), $"CategoryToSellableItem-{categoryId.SimplifyEntityName()}", 0, int.MaxValue), context).ConfigureAwait(false);
            foreach (var product in products.List.Items.Where(x=> !this._addedProducts.Any(prod=>prod.EqualsOrdinalIgnoreCase(x.Id))))
            {
                if (!allProducts.Any(x => x.EntityId.EqualsOrdinalIgnoreCase(product.Id)))
                {
                    await this._commerceCommander.Pipeline<IAssociateSellableItemToParentPipeline>()
                        .Run(new CatalogReferenceArgument(catalogId, allProductsCategoryId, product.Id), context);
                    this._addedProducts.Add(product.Id);
                }
            }
        }
    }
}
