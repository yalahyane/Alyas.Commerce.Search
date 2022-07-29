namespace Alyas.Commerce.Search.Pipelines.Blocks
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Policies;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Plugin.Catalog;
    using Sitecore.Framework.Pipelines;

    public class AddProductToAllProductsCategory : PipelineBlock<CatalogReferenceArgument, CatalogReferenceArgument, CommercePipelineExecutionContext>
    {
        private readonly CommerceCommander _commerceCommander;
        public AddProductToAllProductsCategory(CommerceCommander commerceCommander)
        {
            this._commerceCommander = commerceCommander;
        }
        public override async Task<CatalogReferenceArgument> Run(CatalogReferenceArgument arg, CommercePipelineExecutionContext context)
        {
            if (!(arg.ParentId.StartsWith(CommerceEntity.IdPrefix<Category>(), StringComparison.InvariantCulture) && arg.ReferenceId.StartsWith(CommerceEntity.IdPrefix<SellableItem>())))
            {
                return arg;
            }
            var catalogId = ExtractCatalogId(arg.ParentId);

            var allProductsCategoryPolicy = context.GetPolicy<AllProductsCategoryPolicy>();
            var allProductsCategoryId = $"{CommerceEntity.IdPrefix<Category>()}{catalogId}-{allProductsCategoryPolicy.AllProductsCategoryName}";

            if (arg.ParentId.EqualsOrdinalIgnoreCase(allProductsCategoryId))
            {
                return arg;
            }

            var commerceEntity = await this._commerceCommander.Pipeline<IFindEntityPipeline>()
                .Run(new FindEntityArgument(typeof(SellableItem), arg.ReferenceId), context);
            if (commerceEntity == null)
            {
                return arg;
            }

            var sellableItem = (SellableItem)commerceEntity;
            var category = await this._commerceCommander.Pipeline<IFindEntityPipeline>()
                .Run(new FindEntityArgument(typeof(Category), arg.ParentId), context) as Category;
            if (category == null)
            {
                return arg;
            }

            
            var shopCategoryId = $"{CommerceEntity.IdPrefix<Category>()}{catalogId}-{allProductsCategoryPolicy.ShopCategoryName}";
            var allProducts = await this._commerceCommander.Pipeline<IFindEntitiesInListPipeline>().Run(new FindEntitiesInListArgument(typeof(Category), $"CategoryToSellableItem-{allProductsCategoryId.SimplifyEntityName()}", 0, int.MaxValue){LoadEntities = false}, context).ConfigureAwait(false);

            if (allProducts.EntityReferences.Any(entityRef =>
                entityRef.EntityId.EqualsOrdinalIgnoreCase(sellableItem.Id)))
            {
                return arg;
            }

            if (await this.IsCategoryChildOfShopCategory(shopCategoryId, category.Id, context))
            {
                await this._commerceCommander.Command<AssociateSellableItemToParentCommand>().Process(
                    context.CommerceContext, $"{CommerceEntity.IdPrefix<Category>()}{catalogId}", allProductsCategoryId,
                    arg.ReferenceId);
            }
            
            return arg;
        }

        private async Task<bool> IsCategoryChildOfShopCategory(string parentCategoryId, string categoryId, CommercePipelineExecutionContext context)
        {
            var subCategories = await this._commerceCommander.Pipeline<IFindEntitiesInListPipeline>().Run(new FindEntitiesInListArgument(typeof(Category), $"CategoryToCategory-{parentCategoryId.SimplifyEntityName()}", 0, int.MaxValue) {LoadEntities = false}, context).ConfigureAwait(false);
            if (!subCategories.EntityReferences.Any())
            {
                return false;
            }

            if (subCategories.EntityReferences.Any(item => item.EntityId.EqualsOrdinalIgnoreCase(categoryId)))
            {
                return true;
            }

            foreach (var category in subCategories.EntityReferences)
            {
                if (await this.IsCategoryChildOfShopCategory(category.EntityId, categoryId, context))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ExtractCatalogId(string id)
        {
            var strArray = id.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length < 3)
            {
                return string.Empty;
            }
            return strArray[2];
        }
    }
}
