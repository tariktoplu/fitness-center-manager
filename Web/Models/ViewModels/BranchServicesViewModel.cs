namespace Web.Models.ViewModels;

public class BranchServicesViewModel
{
    public int GymBranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public List<BranchServiceItem> Services { get; set; } = new();
}

public class BranchServiceItem
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public bool IsSelected { get; set; } // Bu hizmet bu şubede var mı?
    public decimal Price { get; set; } // Bu şubedeki fiyatı
}