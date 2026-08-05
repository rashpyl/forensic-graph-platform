using ForensicGraph.Application.Common;
using ForensicGraph.Application.CrimeEvents;
using ForensicGraph.Domain.CrimeEvents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ForensicGraph.Api.Controllers;

/// <summary>
/// HTTP surface for the <see cref="CrimeEvent"/> aggregate:
/// CRUD on the event itself plus the two sub-resource families used by the
/// map UI — person assignments and event-to-event links. All server-managed
/// fields (id, timestamps) come from <see cref="CrimeEventService"/>; the
/// <see cref="CrimeEventWriteDto"/> intentionally omits them so clients cannot
/// forge server state.
/// </summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
public sealed class CrimeEventsController : ControllerBase
{
    private readonly CrimeEventService _service;

    public CrimeEventsController(CrimeEventService service)
    {
        _service = service;
    }

    /// <summary>Returns a paged list of crime events filtered by date range and severity.</summary>
    [HttpGet(Name = "ListCrimeEvents")]
    [ProducesResponseType(typeof(PagedResult<CrimeEventDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<CrimeEventDto>> List(
        [FromQuery] CrimeEventListQuery query,
        CancellationToken cancellationToken)
    {
        return _service.ListAsync(query, cancellationToken);
    }

    /// <summary>Returns a single crime event with its assigned persons and outgoing links.</summary>
    [HttpGet("{id:guid}", Name = "GetCrimeEvent")]
    [ProducesResponseType(typeof(CrimeEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CrimeEventDto> Get(Guid id, CancellationToken cancellationToken)
    {
        return _service.GetAsync(id, cancellationToken);
    }

    /// <summary>Creates a new crime event and returns the persisted representation.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CrimeEventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CrimeEventDto>> Create(
        [FromBody] CrimeEventWriteDto body,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(body, cancellationToken);
        return CreatedAtRoute("GetCrimeEvent", new { id = created.Id }, created);
    }

    /// <summary>Fully replaces a crime event.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CrimeEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CrimeEventDto> Update(
        Guid id,
        [FromBody] CrimeEventWriteDto body,
        CancellationToken cancellationToken)
    {
        return _service.UpdateAsync(id, body, cancellationToken);
    }

    /// <summary>Deletes a crime event. Assignments and outgoing links are removed by cascade.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Assigns a person to this event under a specific role.</summary>
    [HttpPost("{id:guid}/persons")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignPerson(
        Guid id,
        [FromBody] AssignPersonRequest body,
        CancellationToken cancellationToken)
    {
        await _service.AssignPersonAsync(id, body.PersonId, body.Role, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Removes a person / role assignment from this event.
    /// The role is part of the composite key on the join, so it is required.
    /// </summary>
    [HttpDelete("{id:guid}/persons/{personId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnassignPerson(
        Guid id,
        Guid personId,
        [FromQuery] EventRole role,
        CancellationToken cancellationToken)
    {
        await _service.UnassignPersonAsync(id, personId, role, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Creates a directed link from this event to another event.
    /// Self-links and duplicate links are rejected with 409 Conflict.
    /// </summary>
    [HttpPost("{id:guid}/links")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LinkEvent(
        Guid id,
        [FromBody] LinkEventRequest body,
        CancellationToken cancellationToken)
    {
        await _service.LinkEventAsync(id, body.ToEventId, body.Note, cancellationToken);
        return NoContent();
    }

    /// <summary>Removes a directed link from this event to <paramref name="toEventId"/>.</summary>
    [HttpDelete("{id:guid}/links/{toEventId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkEvent(
        Guid id,
        Guid toEventId,
        CancellationToken cancellationToken)
    {
        await _service.UnlinkEventAsync(id, toEventId, cancellationToken);
        return NoContent();
    }
}
