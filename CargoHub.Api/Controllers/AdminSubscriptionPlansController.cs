using CargoHub.Application.Auth;
using CargoHub.Application.Billing.Admin;
using CargoHub.Application.Billing.AdminPlans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CargoHub.Api.Controllers;

[ApiController]
[Route("api/v1/admin/subscription-plans")]
[Authorize(Roles = RoleNames.SuperAdmin)]
public class AdminSubscriptionPlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminSubscriptionPlansController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminSubscriptionPlanSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var list = await _mediator.Send(new ListAdminSubscriptionPlansQuery(), cancellationToken);
        return Ok(list);
    }

    [HttpGet("{planId:guid}")]
    public async Task<ActionResult<AdminSubscriptionPlanDetailDto>> GetDetail(Guid planId, CancellationToken cancellationToken)
    {
        var detail = await _mediator.Send(new GetAdminSubscriptionPlanDetailQuery(planId), cancellationToken);
        if (detail == null)
            return NotFound(new { message = "Subscription plan not found." });
        return Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateSubscriptionPlanRequest body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateAdminSubscriptionPlanCommand(
                body.Name,
                body.Kind,
                body.ChargeTimeAnchor,
                body.TrialBookingAllowance,
                body.Currency,
                body.IsActive),
            cancellationToken);
        if (!result.Success)
            return BadRequest(new { errorCode = result.ErrorCode, message = result.Message });
        return Created($"/api/v1/admin/subscription-plans/{result.PlanId}", new { id = result.PlanId });
    }

    [HttpPut("{planId:guid}")]
    public async Task<ActionResult> Update(Guid planId, [FromBody] UpdateSubscriptionPlanRequest body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateAdminSubscriptionPlanCommand(
                planId,
                body.Name,
                body.Kind,
                body.ChargeTimeAnchor,
                body.TrialBookingAllowance,
                body.Currency,
                body.IsActive),
            cancellationToken);
        return MapMutation(result);
    }

    [HttpDelete("{planId:guid}")]
    public async Task<ActionResult> Delete(Guid planId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteAdminSubscriptionPlanCommand(planId), cancellationToken);
        return MapMutation(result, noContentOnSuccess: true);
    }

    [HttpPost("{planId:guid}/pricing-periods")]
    public async Task<ActionResult> AddPricingPeriod(
        Guid planId,
        [FromBody] PricingPeriodRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddAdminPricingPeriodCommand(
                planId,
                body.EffectiveFromUtc,
                body.ChargePerBooking,
                body.MonthlyFee,
                body.IncludedBookingsPerMonth,
                body.OverageChargePerBooking),
            cancellationToken);
        return MapMutation(result);
    }

    [HttpPut("pricing-periods/{periodId:guid}")]
    public async Task<ActionResult> UpdatePricingPeriod(
        Guid periodId,
        [FromBody] PricingPeriodRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateAdminPricingPeriodCommand(
                periodId,
                body.EffectiveFromUtc,
                body.ChargePerBooking,
                body.MonthlyFee,
                body.IncludedBookingsPerMonth,
                body.OverageChargePerBooking),
            cancellationToken);
        return MapMutation(result);
    }

    [HttpDelete("pricing-periods/{periodId:guid}")]
    public async Task<ActionResult> DeletePricingPeriod(Guid periodId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteAdminPricingPeriodCommand(periodId), cancellationToken);
        return MapMutation(result, noContentOnSuccess: true);
    }

    [HttpPut("pricing-periods/{periodId:guid}/tiers")]
    public async Task<ActionResult> ReplaceTiers(
        Guid periodId,
        [FromBody] ReplaceTiersRequest body,
        CancellationToken cancellationToken)
    {
        var tiers = (body.Tiers ?? new List<PricingTierRowRequest>())
            .Select(t => new AdminPricingTierInput
            {
                Id = t.Id,
                Ordinal = t.Ordinal,
                InclusiveMaxBookingsInPeriod = t.InclusiveMaxBookingsInPeriod,
                ChargePerBooking = t.ChargePerBooking,
                MonthlyFee = t.MonthlyFee
            })
            .ToList();
        var result = await _mediator.Send(new ReplaceAdminPricingPeriodTiersCommand(periodId, tiers), cancellationToken);
        return MapMutation(result);
    }

    private ActionResult MapMutation(AdminPlanMutationResult result, bool noContentOnSuccess = false)
    {
        if (result.Success)
            return noContentOnSuccess ? NoContent() : Ok(new { ok = true });
        if (result.ErrorCode == "NotFound")
            return NotFound(new { errorCode = result.ErrorCode, message = result.Message });
        return BadRequest(new { errorCode = result.ErrorCode, message = result.Message });
    }

    public sealed class CreateSubscriptionPlanRequest
    {
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "";
        public string ChargeTimeAnchor { get; set; } = "";
        public int? TrialBookingAllowance { get; set; }
        public string Currency { get; set; } = "EUR";
        public bool IsActive { get; set; } = true;
    }

    public sealed class UpdateSubscriptionPlanRequest
    {
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "";
        public string ChargeTimeAnchor { get; set; } = "";
        public int? TrialBookingAllowance { get; set; }
        public string Currency { get; set; } = "EUR";
        public bool IsActive { get; set; } = true;
    }

    public sealed class PricingPeriodRequest
    {
        public DateTime EffectiveFromUtc { get; set; }
        public decimal? ChargePerBooking { get; set; }
        public decimal? MonthlyFee { get; set; }
        public int? IncludedBookingsPerMonth { get; set; }
        public decimal? OverageChargePerBooking { get; set; }
    }

    public sealed class ReplaceTiersRequest
    {
        public List<PricingTierRowRequest>? Tiers { get; set; }
    }

    public sealed class PricingTierRowRequest
    {
        public Guid? Id { get; set; }
        public int Ordinal { get; set; }
        public int? InclusiveMaxBookingsInPeriod { get; set; }
        public decimal? ChargePerBooking { get; set; }
        public decimal? MonthlyFee { get; set; }
    }
}
