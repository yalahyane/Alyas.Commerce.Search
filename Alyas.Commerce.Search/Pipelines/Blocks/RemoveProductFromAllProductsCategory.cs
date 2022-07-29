namespace Alyas.Commerce.Search.Pipelines.Blocks
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Policies;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Plugin.Catalog;
    using Sitecore.Framework.Pipelines;

    public class RemoveProductFromAllProductsCategory : PipelineBlock<RelationshipArgument, RelationshipArgument, CommercePipelineExecutionContext>
    {
        private readonly CommerceCommander _commerceCommander;
        public RemoveProductFromAllProductsCategory(CommerceCommander commerceCommander)
        {
            this._commerceCommander = commerceCommander;
        }
        public override async Task<RelationshipArgument> Run(RelationshipArgument arg, CommercePipelineExecutionContext context)
        {
            if (!(arg.SourceName.StartsWith(CommerceEntity.IdPrefix<Category>(), StringComparison.InvariantCulture) && arg.TargetName.StartsWith(CommerceEntity.IdPrefix<SellableItem>())))
            {
                return arg;
            }
            var catalogId = ExtractCatalogId(arg.SourceName);

            var allProductsCategoryPolicy = context.GetPolicy<AllProductsCategoryPolicy>();
            var allProductsCategoryId = $"{CommerceEntity.IdPrefix<Category>()}{catalogId}-{allProductsCategoryPolicy.AllProductsCategoryName}";

            if (arg.SourceName.EqualsOrdinalIgnoreCase(allProductsCategoryId))
            {
                return arg;
            }

            var sellableItem = await this._commerceCommander.Pipeline<IFindEntityPipeline>()
                .Run(new FindEntityArgument(typeof(SellableItem), arg.TargetName), context) as SellableItem;
            if (sellableItem == null)
            {
                return arg;
            }

            var category = await this._commerceCommander.Pipeline<IFindEntityPipeline>()
                .Run(new FindEntityArgument(typeof(Category), arg.SourceName), context) as Category;
            if (category == null)
            {
                return arg;
            }

            
            var shopCategoryId = $"{CommerceEntity.IdPrefix<Category>()}{catalogId}-{allProductsCategoryPolicy.ShopCategoryName}";
            var allProducts = await this._commerceCommander.Pipeline<IFindEntitiesInListPipeline>().Run(new FindEntitiesInListArgument(typeof(Category), $"CategoryToSellableItem-{allProductsCategoryId.SimplifyEntityName()}", 0, int.MaxValue){LoadEntities = false}, context).ConfigureAwait(false);

            if (!allProducts.EntityReferences.Any(entityRef =>
                entityRef.EntityId.EqualsOrdinalIgnoreCase(sellableItem.Id)))
            {
                return arg;
            }

            if (string.IsNullOrWhiteSpace(sellableItem.ParentCategoryList) || !allProducts.EntityReferences.Any(entityRef =>
                entityRef.EntityId.EqualsOrdinalIgnoreCase(sellableItem.Id)))
            {
                return arg;
            }
            var allCategories = (await this._commerceCommander.Pipeline<IGetCategoriesPipeline>().Run(new GetCategoriesArgument(catalogId), context)).ToList();
            var allCurrentCatalogCategories = allCategories.Where(x => x.Id.ToLower().Contains(catalogId.ToLower()));
            var parentCategories = allCurrentCatalogCategories.Where(x =>
                sellableItem.ParentCategoryList.Split('|').Any(id =>
                    id.Equals(x.SitecoreId, StringComparison.OrdinalIgnoreCase)));
            foreach (var parentCategory in parentCategories)
            {
                if (await this.IsCategoryChildOfShopCategory(shopCategoryId, parentCategory.Id, context))
                {
                    return arg;
                }
            }

            var allProductsCategory = await this._commerceCommander.Pipeline<IFindEntityPipeline>()
                .Run(new FindEntityArgument(typeof(Category), allProductsCategoryId), context);
            var entitiesArgument = new ListEntitiesArgument(arg.TargetName.Split('|'), CommerceEntity.VersionedListName(allProductsCategory, arg.RelationshipType + "-" + allProductsCategoryId.SimplifyEntityName()));
            await this._commerceCommander.Pipeline<IRemoveListEntitiesPipeline>()
                .Run(entitiesArgument, context.CommerceContext.PipelineContextOptions);
            parentCategories = allCategories.Where(x =>
                sellableItem.ParentCategoryList.Split('|').Any(id =>
                    id.Equals(x.SitecoreId, StringComparison.OrdinalIgnoreCase)));

            parentCategories = parentCategories.Where(x => !x.Id.EqualsOrdinalIgnoreCase(allProductsCategoryId));
            sellableItem.ParentCategoryList = string.Join("|", parentCategories.Select(x => x.SitecoreId));

            await this._commerceCommander.Pipeline<IPersistEntityPipeline>()
                .Run(new PersistEntityArgument(sellableItem), context);
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
