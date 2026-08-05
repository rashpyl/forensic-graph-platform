using ForensicGraph.Application.Common;
using ForensicGraph.Application.Persons;
using ForensicGraph.Domain.Persons;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ForensicGraph.Api.Controllers;

/// <summary>
/// HTTP surface for the <see cref="Person"/> aggregate.
/// The list endpoint doubles as the autocomplete backend for the map UI:
/// pass <c>?q=</c> to substring-match against the concatenated full name.
/// </summary>
[ApiController]
[Route("api/persons")]
[Produces("application/json")]
public sealed class PersonsController : ControllerBase
{
    private readonly PersonService _service;

    public PersonsController(PersonService service)
    {
        _service = service;
    }

    /// <summary>Returns a paged list of persons, optionally filtered by autocomplete query.</summary>
    [HttpGet(Name = "ListPersons")]
    [ProducesResponseType(typeof(PagedResult<PersonDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<PersonDto>> List(
        [FromQuery] PersonListQuery query,
        CancellationToken cancellationToken)
    {
        return _service.ListAsync(query, cancellationToken);
    }

    /// <summary>Returns a single person.</summary>
    [HttpGet("{id:guid}", Name = "GetPerson")]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PersonDto> Get(Guid id, CancellationToken cancellationToken)
    {
        return _service.GetAsync(id, cancellationToken);
    }

    /// <summary>Creates a new person.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PersonDto>> Create(
        [FromBody] PersonWriteDto body,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(body, cancellationToken);
        return CreatedAtRoute("GetPerson", new { id = created.Id }, created);
    }

    /// <summary>Fully replaces a person.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PersonDto> Update(
        Guid id,
        [FromBody] PersonWriteDto body,
        CancellationToken cancellationToken)
    {
        return _service.UpdateAsync(id, body, cancellationToken);
    }

    /// <summary>Deletes a person. Their assignments to events are removed by cascade.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
