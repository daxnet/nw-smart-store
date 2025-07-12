using Microsoft.AspNetCore.Mvc;
using Nss.ApiService.Models;
using Nss.ApiService.Services;

namespace Nss.ApiService.Controllers
{
    /// <summary>
    /// Represents the API controller for semantic search operations.
    /// </summary>
    /// <param name="indexService"></param>
    [ApiController]
    [Route("api/search")]
    public class SemanticSearchController(
        QdrantIndexService indexService,
        SearchService searchService) : ControllerBase
    {
        [HttpPost("build-index")]
        public async Task<IActionResult> BuildIndexAsync([FromQuery] string lang = "en", [FromQuery] bool force = false)
        {
            await indexService.CreateIndexAsync(lang, force);
            return Ok("Index created successfully.");
        }

        [HttpPost]
        public async Task StreamQueryAsync([FromBody] QueryModel model)
        {
            if (string.IsNullOrEmpty(model.Query))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                await Response.WriteAsync("Query cannot be null or empty.");
                return;
            }

            Response.ContentType = "text/plain";
            await foreach (var chunk in searchService.GetStreamingChatMessageContentsAsync(model.Query))
            {
                if (string.IsNullOrEmpty(chunk.Content)) continue;
                await Response.WriteAsync(chunk.Content);
                await Response.Body.FlushAsync();
            }
        }

        //[HttpPost("mock")]
        //public async Task MockQueryAsync([FromBody] QueryModel model)
        //{
        //    // This is a mock implementation for testing purposes.
        //    // simply return the query in the model as a streamed response
        //    if (string.IsNullOrEmpty(model.Query))
        //    {
        //        Response.StatusCode = StatusCodes.Status400BadRequest;
        //        await Response.WriteAsync("Query cannot be null or empty.");
        //        return;
        //    }
        //    Response.ContentType = "text/plain";
        //    var returnStr = model.Query;
        //    // return the returnStr char by char
        //    foreach (var c in returnStr)
        //    {
        //        await Response.WriteAsync(c.ToString());
        //        await Response.Body.FlushAsync();
        //    }
        //}
    }
}
