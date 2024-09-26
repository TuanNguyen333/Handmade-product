using HandmadeProductManagement.Core.Base;

namespace HandmadeProductManagement.ModelViews.PromotionModelViews;

public class PromotionForManipulationDto 
{
    public string? Name { get; set; } 
    public string? Description { get; set; }
    public float? DiscountRate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
}