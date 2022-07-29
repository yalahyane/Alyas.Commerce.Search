namespace Alyas.Commerce.Search.Controllers
{
    using System;
    using System.Web.Http.OData;
    using Commands;
    using Microsoft.AspNetCore.Mvc;
    using Sitecore.Commerce.Core;

    [Microsoft.AspNetCore.OData.EnableQuery]
    public class CommandsController : CommerceController
    {
        public CommandsController(IServiceProvider serviceProvider, CommerceEnvironment globalEnvironment) : base(serviceProvider, globalEnvironment)
        {
        }

        [HttpPut]
        [Route("PopulateAllProductsCategory()")]
        public IActionResult PopulateAllProductsCategory([FromBody] ODataActionParameters value)
        {
            if (!this.ModelState.IsValid)
                return new BadRequestObjectResult(this.ModelState);

            var command = this.Command<PopulateAllProductsCategoryCommand>();
            return new ObjectResult(ExecuteLongRunningCommand(() => command.Process(this.CurrentContext, value["catalogId"].ToString())));
        }
    }
}
