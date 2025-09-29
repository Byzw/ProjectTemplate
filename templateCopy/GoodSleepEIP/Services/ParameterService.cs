using Dapper;
using GoodSleepEIP.Models;

namespace GoodSleepEIP
{
    public interface IParameterService
    {
        Dictionary<string, string> GetParameters(string category);
        string? GetFirstParameterValue(string category);
        List<Parameter> GetListParameters(List<string> categories);
        List<Parameter> GetPrefixParameters(string CategoryPrefix);
        List<Parameter> GetAllParameters();
        public string GetAllowedExtensionsString();
        string? GetInvoiceDeptByMaterialCategory(string materialCategoryId);
    }

    public class ParameterService : IParameterService
    {
        private readonly IConfiguration configuration;
        private IDapperHelper dapper;

        public ParameterService(IConfiguration _config, IDapperHelper _dapper)
        {
            configuration = _config;
            dapper = _dapper;
        }



        public Dictionary<string, string> GetParameters(string category)
        {
            var parameters = new Dictionary<string, string>();
            try
            {
                if (string.IsNullOrEmpty(category)) return [];

                var sql = $@"SELECT * FROM {DBName.Main}.Parameter WHERE Category = @category ORDER BY Code ASC";
                var result = dapper.Query<Parameter>(sql, new { category }).ToList();
                foreach (var item in result) parameters[item.Code] = item.Description;

                return parameters;
            }
            catch
            {
                return parameters;
            }
        }

        /// <summary>
        /// 取得指定類別的第一筆參數值(Description)
        /// </summary>
        /// <param name="category">參數類別</param>
        /// <returns>第一筆參數的值，如果沒有找到則返回null</returns>
        public string? GetFirstParameterValue(string category)
        {
            try
            {
                if (string.IsNullOrEmpty(category)) return null;

                var allParameters = GetParameters(category);
                if (allParameters.Count > 0)
                {
                    return allParameters.Values.First();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public List<Parameter> GetListParameters(List<string> categories)
        {
            try
            {
                if (categories.Count == 0) return [];

                var sql = $@"SELECT * FROM {DBName.Main}.Parameter WHERE Category IN @categories ORDER BY Code ASC";
                var result = dapper.Query<Parameter>(sql, new { categories }).ToList();

                return result;
            }
            catch
            {
                return new List<Parameter>();
            }
        }

        public List<Parameter> GetPrefixParameters(string CategoryPrefix)
        {
            try
            {
                if (string.IsNullOrEmpty(CategoryPrefix)) return new List<Parameter>();

                var sql = $@"SELECT * FROM {DBName.Main}.Parameter WHERE Category LIKE @CategoryPrefix + '%' ORDER BY Code ASC";
                var result = dapper.Query<Parameter>(sql, new { CategoryPrefix }).ToList();

                return result;
            }
            catch
            {
                return new List<Parameter>();
            }
        }

        public List<Parameter> GetAllParameters()
        {
            try
            {
                var result = dapper.Query<Parameter>($"SELECT * FROM {DBName.Main}.Parameter").ToList();
                return result;
            }
            catch
            {
                return new List<Parameter>();
            }
        }

        public string GetAllowedExtensionsString()
        {
            var allowedExtensions = configuration.GetSection("Attachment:AllowedExtensions").Get<List<string>>();
            if (allowedExtensions == null || allowedExtensions.Count == 0) return "";

            try { return String.Join(",", allowedExtensions); }
            catch { return ""; }
        }

        /// <summary>
        /// 根據產品類別ID取得對應的發票部門ID
        /// </summary>
        /// <param name="materialCategoryId">產品類別ID</param>
        /// <returns>發票部門ID，如果沒有對應則返回null</returns>
        public string? GetInvoiceDeptByMaterialCategory(string materialCategoryId)
        {
            try
            {
                if (string.IsNullOrEmpty(materialCategoryId)) return null;

                // 從 Parameter 表中查詢 MaterialCategoryInvoiceDept 類別的配置
                // Code = 產品類別ID, Description = 發票部門ID
                var sql = $@"SELECT Description FROM {DBName.Main}.Parameter 
                           WHERE Category = 'MaterialCategoryInvoiceDept' AND Code = @materialCategoryId";
                
                var invoiceDeptId = dapper.Query<string>(sql, new { materialCategoryId }).FirstOrDefault();
                
                return invoiceDeptId;
            }
            catch
            {
                return null;
            }
        }
    }
}
