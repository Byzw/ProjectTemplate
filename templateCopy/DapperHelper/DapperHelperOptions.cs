using System.ComponentModel.DataAnnotations;

namespace Dapper
{
    public class DapperHelperOptions
    {
        [Required(ErrorMessage = "connection string is required")]
        public string ConnectionString { get; set; }
    }
}