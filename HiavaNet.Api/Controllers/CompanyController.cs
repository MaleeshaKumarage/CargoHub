using System.Security.Claims;
using HiavaNet.Application.Company.Commands;
using HiavaNet.Application.Company.Queries;
using HiavaNet.Domain.Companies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiavaNet.Api.Controllers;

/// <summary>
/// Company management. Uses CQRS (MediatR); companies can be linked to the current user (CustomerId).
/// </summary>
[ApiController]
[Route("api/v1/company")]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly IMediator _mediator;

    public CompanyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new company record, linked to the current user when authenticated.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Company>> CreateCompany([FromBody] Company company)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var created = await _mediator.Send(new CreateCompanyCommand(company, customerId), HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetCompanyById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Returns a company by its internal identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Company>> GetCompanyById(Guid id)
    {
        var company = await _mediator.Send(new GetCompanyByIdQuery(id), HttpContext.RequestAborted);
        if (company == null)
            return NotFound();
        return Ok(company);
    }
}

