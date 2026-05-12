namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs
{
    public class BranchDto
    {
        public long BranchID { get; set; }
        public long CompanyID { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
    }
}