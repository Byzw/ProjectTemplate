using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GoodSleepEIP.Swagger
{
    public class AdditionalSchemasDocumentFilter : IDocumentFilter
    {
        private readonly List<Type> _typesToAdd;

        // 將類型清單作為建構函數參數傳入
        public AdditionalSchemasDocumentFilter(List<Type> typesToAdd)
        {
            _typesToAdd = typesToAdd;
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // 根據傳入的類型清單生成 schema
            foreach (var type in _typesToAdd)
            {
                context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
            }
        }
    }
}
