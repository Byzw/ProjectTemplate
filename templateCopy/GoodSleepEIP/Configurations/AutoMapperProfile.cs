using AutoMapper;
using GoodSleepEIP.Models;

namespace GoodSleepEIP.Configurations
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // CreateMap<CustomerDto, T8EsbCustomer>();        // 客戶相關映射
            // CreateMap<comCustomerLink, T8EsbCustomerLink>();        // 客戶相關映射
            // CreateMap<comCustomerAddr, T8EsbCustomerAddr>();        // 客戶相關映射
            // CreateMap<SalesOrderDto, T8EsbMainSalesOrder>();     // 銷售訂單相關映射
            // CreateMap<SalesOrderDetailDto, T8EsbSubOrder>(); // 銷售訂單明細相關映射
            // 可以在這裡添加其他映射...
        }
    }
}