namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs
{
    public class CustomerGroupDto
    {
        public long GroupID { get; set; }
        public long CompanyID { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string GroupCode { get; set; } = string.Empty;
    }
}