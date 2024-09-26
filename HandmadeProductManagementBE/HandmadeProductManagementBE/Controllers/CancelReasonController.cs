﻿using HandmadeProductManagement.Contract.Services.Interface;
using HandmadeProductManagement.Core.Base;
using HandmadeProductManagement.Contract.Repositories.Entity;
using Microsoft.AspNetCore.Mvc;
using HandmadeProductManagement.Services.Service;

namespace HandmadeProductManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CancelReasonController : ControllerBase
    {
        private readonly ICancelReasonService _cancelReasonService;

        public CancelReasonController(ICancelReasonService cancelReasonService)
        {
            _cancelReasonService = cancelReasonService;
        }

        // GET: api/CancelReason
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CancelReason>>> GetCancelReasons()
        {
            try
            {
                IList<CancelReason> reasons = await _cancelReasonService.GetAll();
                return Ok(BaseResponse<IList<CancelReason>>.OkResponse(reasons));
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, BaseResponse<string>.FailResponse(ex.Message));
            }
        }

        // GET: api/CancelReason/{id} (string id)
        [HttpGet("{id}")]
        public async Task<ActionResult<CancelReason>> GetCancelReason(string id)
        {
            try
            {
                CancelReason reason = await _cancelReasonService.GetById(id);
                return Ok(BaseResponse<CancelReason>.OkResponse(reason));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(BaseResponse<string>.FailResponse("Promotion not found"));
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, BaseResponse<string>.FailResponse(ex.Message));
            }
        }

        // POST: api/CancelReason
        [HttpPost]
        public async Task<ActionResult<CancelReason>> CreateCancelReason(CancelReason reason)
        {
            try
            {
                CancelReason createdReason = await _cancelReasonService.Create(reason);
                return CreatedAtAction(nameof(GetCancelReason), new { id = createdReason.Id }, createdReason);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, BaseResponse<string>.FailResponse(ex.Message));
            }
        }

        // PUT: api/CancelReason/{id} (string id)
        [HttpPut("{id}")]
        public async Task<ActionResult<CancelReason>> UpdateCancelReason(string id, CancelReason updatedReason)
        {
            try
            {
                CancelReason reason = await _cancelReasonService.Update(id, updatedReason);
                return Ok(BaseResponse<CancelReason>.OkResponse(reason));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(BaseResponse<string>.FailResponse("Promotion not found"));
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, BaseResponse<string>.FailResponse(ex.Message));
            }
        }

        // DELETE: api/CancelReason/{id} (string id)
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCancelReason(string id)
        {
            try
            {
                bool success = await _cancelReasonService.Delete(id);
                if (!success)
                {
                    return NotFound();
                }
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(BaseResponse<string>.FailResponse("Promotion not found"));
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, BaseResponse<string>.FailResponse(ex.Message));
            }
        }

        // PUT: api/CancelReason/{id}/soft-delete
        [HttpPut("{id}/soft-delete")]
        public async Task<ActionResult> SoftDeleteCancelReason(string id)
        {
            try
            {
                bool success = await _cancelReasonService.SoftDelete(id);
                if (!success)
                {
                    return NotFound();
                }
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(BaseResponse<string>.FailResponse("Promotion not found"));
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, BaseResponse<string>.FailResponse(ex.Message));
            }
            
        }

    }
}
