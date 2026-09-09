using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public sealed class CreateContentTagDto
{
    public Guid ContentId { get; set; }
    public Guid TagId { get; set; }
}

public sealed class ReplaceContentTagsDto
{
    [MaxLength(30)] public List<Guid> TagIds { get; set; } = [];
}

public sealed class ContentTagDto
{
    public Guid ContentId { get; set; }
    public Guid TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
}
