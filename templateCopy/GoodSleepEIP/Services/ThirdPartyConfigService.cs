using Dapper;
using GoodSleepEIP.Models;

namespace GoodSleepEIP;

public interface IThirdPartyConfigService
{
    PlatformIntegrationDto? GetServiceConfig(string deptId, string serviceType);
}

public class ThirdPartyConfigService : IThirdPartyConfigService
{
    private readonly IConfiguration configuration;
    private readonly IDapperHelper dapper;

    public ThirdPartyConfigService(IConfiguration _config, IDapperHelper _dapper)
    {
        configuration = _config;
        dapper = _dapper;
    }

    public PlatformIntegrationDto? GetServiceConfig(string deptId, string serviceType)
    {
        try
        {
            // Step 1: 取得公司基本資料
            string sqlstr = @$"SELECT TOP 1 * FROM {DBName.Main}.Company WHERE DeptId = @DeptId AND IsActive = 1";
            var company = dapper.Query<Company>(sqlstr, new { DeptId = deptId }).FirstOrDefault();
            if (company == null)
            {
                return null;
            }
            
            // Step 2: 取得單一平台的服務設定
            sqlstr = @$"SELECT TOP 1 * FROM {DBName.Main}.CompanyServiceIntegration WHERE CompanyId = @CompanyId AND ServiceType = @ServiceType AND IsActive = 1";
            var integration = dapper.Query<CompanyServiceIntegration>(sqlstr, new { CompanyId = company.CompanyId, ServiceType = serviceType }).FirstOrDefault();
            if (integration == null)
            {
                return null;
            }

            // Step 3: 取得該服務設定對應的所有參數
            sqlstr = @$"SELECT * FROM {DBName.Main}.CompanyServiceParam WHERE IntegrationId = @IntegrationId";
            var serviceParams = dapper.Query<CompanyServiceParam>(sqlstr, new { IntegrationId = integration.Id }).ToList();
            
            // Step 4: 組裝結果
            var result = new PlatformIntegrationDto
            {
                CompanyId = company.CompanyId,
                DeptId = company.DeptId,
                TaxId = company.TaxId,
                CompanyName = company.CompanyName,
                ServiceType = serviceType,
                EnvType = integration.EnvType,
                Integrations = new List<PlatformEndpointDto>()
            };

            var endpoint = new PlatformEndpointDto
            {
                EndpointName = integration.EndpointName,
                ApiBaseUrl = integration.ApiBaseUrl,
                Params = new Dictionary<string, string>()
            };

            foreach (var param in serviceParams)
            {
                endpoint.Params[param.ParamKey] = param.ParamValue;
            }
            result.Integrations.Add(endpoint);

            return result;
        }
        catch (Exception ex)
        {
            // Log the exception (not implemented here)
            Console.WriteLine($"Error retrieving service config: {ex.Message}");
            return null;
        }
    }
}

public class PlatformIntegrationDto : Company
{
    public string ServiceType { get; set; } = null!;
    public string EnvType { get; set; } = null!;
    public List<PlatformEndpointDto> Integrations { get; set; } = new();
}

public class PlatformEndpointDto
{
    public string EndpointName { get; set; } = null!;
    public string? ApiBaseUrl { get; set; }
    public Dictionary<string, string> Params { get; set; } = new();
}