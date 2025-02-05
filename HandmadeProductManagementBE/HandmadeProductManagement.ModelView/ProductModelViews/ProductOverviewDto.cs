﻿namespace HandmadeProductManagement.ModelViews.ProductModelViews
{
    public class ProductOverviewDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public string Status { get; set; }
        public decimal Rating { get; set; }
        public int SoldCount { get; set; }
        public int LowestPrice { get; set; }
    }
}
