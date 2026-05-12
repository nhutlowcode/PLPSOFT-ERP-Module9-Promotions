using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Interfaces;

namespace PLPSOFT.ERP.SaaS.Web.Controllers
{
    [ApiController]
    [Route("api/promotions/engine")]
    public class PromotionsEngineController : ControllerBase
    {
        private readonly IPromotionEngineService _engine;

        public PromotionsEngineController(IPromotionEngineService engine)
        {
            _engine = engine;
        }

        [HttpPost("calculate")]
        public async Task<ActionResult<CartDiscountResult>> Calculate([FromBody] CartRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request không hợp lệ.");
            }

            var result = await _engine.CalculateBestDiscountAsync(request);
            return Ok(result);
        }

        [HttpPost("deduct/{promotionId:long}")]
        public async Task<IActionResult> Deduct(long promotionId)
        {
            if (promotionId <= 0)
            {
                return BadRequest("promotionId phải > 0.");
            }

            await _engine.DeductPromotionUsageAsync(promotionId);
            return Ok();
        }
    }
}