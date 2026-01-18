using Times.Dto.Common;

public class UpdateProjectRequest
{
	public string? Name { get; set; }
	public string? Code { get; set; }
	public string? Description { get; set; }
	public bool? IsActive { get; set; }

	public Optional<Guid?>? ClientId { get; set; } // note: Optional of nullable Guid
}
