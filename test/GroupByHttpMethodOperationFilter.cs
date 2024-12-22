namespace test;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

//this class is for swagger to group crud functions by their type POST/GET/PUT/DELETE

public class GroupByHttpMethodOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var httpMethod = context.ApiDescription.HttpMethod?.ToUpperInvariant();
        if (!string.IsNullOrEmpty(httpMethod))
        {
            operation.Tags = new List<OpenApiTag> { new OpenApiTag { Name = $"{httpMethod} Methods" } };
        }
    }
}