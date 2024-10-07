﻿using HandmadeProductManagement.ModelViews.PromotionModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandmadeProductManagement.ModelViews.CategoryModelViews;

namespace HandmadeProductManagement.Contract.Services.Interface
{
    public interface ICategoryService
    {
        Task<IList<CategoryDto>> GetAll();
        Task<CategoryDto> GetById(string id);
        Task<CategoryDto> Create(CategoryForCreationDto category);
        Task<CategoryDto> Update(string id, CategoryForUpdateDto category);
        Task<bool> SoftDelete(string id);
    }
}
