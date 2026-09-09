using ContentEntity = KanchanaburiPlatform.Domain.Entities.Content;

namespace API.Controllers.Content;

[ApiController]
[Route("api/content-tags")]
[Authorize(Roles = "Admin")]
public sealed class ContentTagsController(IUnitOfWork unit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContentTagDto>>> GetContentTags(Guid contentId)
    {
        if (await unit.Repository<ContentEntity>().GetByIdAsync(contentId) is null) return NotFound("Content not found.");

        var tags = (await unit.Repository<ContentTag>().ListAllAsync())
            .Where(contentTag => contentTag.ContentId == contentId)
            .Join(await unit.Repository<Tag>().ListAllAsync(), contentTag => contentTag.TagId, tag => tag.TagId,
                (contentTag, tag) => new ContentTagDto
                {
                    ContentId = contentTag.ContentId,
                    TagId = tag.TagId,
                    TagName = tag.TagName
                })
            .OrderBy(tag => tag.TagName)
            .ToList();
        return Ok(tags);
    }

    [HttpPost]
    public async Task<ActionResult<ContentTagDto>> CreateContentTag(CreateContentTagDto dto)
    {
        if (await unit.Repository<ContentEntity>().GetByIdAsync(dto.ContentId) is null) return NotFound("Content not found.");
        var tag = await unit.Repository<Tag>().GetByIdAsync(dto.TagId);
        if (tag is null || tag.Status != "Active") return BadRequest("Tag is not available.");
        if (await unit.Repository<ContentTag>().GetByIdAsync(dto.ContentId, dto.TagId) is not null)
            return Conflict("This tag is already assigned to the content.");

        var contentTag = new ContentTag { ContentId = dto.ContentId, TagId = dto.TagId };
        unit.Repository<ContentTag>().Add(contentTag);
        return await unit.Complete()
            ? CreatedAtAction(nameof(GetContentTags), new { contentId = dto.ContentId }, ToDto(contentTag, tag))
            : BadRequest("Problem assigning tag to content.");
    }

    [HttpPut("{contentId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ContentTagDto>>> ReplaceContentTags(Guid contentId, ReplaceContentTagsDto dto)
    {
        if (await unit.Repository<ContentEntity>().GetByIdAsync(contentId) is null) return NotFound("Content not found.");
        var tagIds = dto.TagIds.Distinct().ToList();
        var tags = (await unit.Repository<Tag>().ListAllAsync()).Where(tag => tagIds.Contains(tag.TagId)).ToList();
        if (tags.Count != tagIds.Count || tags.Any(tag => tag.Status != "Active"))
            return BadRequest("One or more tags are not available.");

        var repository = unit.Repository<ContentTag>();
        var existing = (await repository.ListAllAsync()).Where(contentTag => contentTag.ContentId == contentId).ToList();
        foreach (var contentTag in existing) repository.Remove(contentTag);
        foreach (var tagId in tagIds) repository.Add(new ContentTag { ContentId = contentId, TagId = tagId });

        if (!await unit.Complete()) return BadRequest("Problem updating content tags.");
        return Ok(tags.OrderBy(tag => tag.TagName).Select(tag => new ContentTagDto
        {
            ContentId = contentId,
            TagId = tag.TagId,
            TagName = tag.TagName
        }).ToList());
    }

    [HttpDelete("{contentId:guid}/{tagId:guid}")]
    public async Task<IActionResult> DeleteContentTag(Guid contentId, Guid tagId)
    {
        var contentTag = await unit.Repository<ContentTag>().GetByIdAsync(contentId, tagId);
        if (contentTag is null) return NotFound();
        unit.Repository<ContentTag>().Remove(contentTag);
        return await unit.Complete() ? NoContent() : BadRequest("Problem removing tag from content.");
    }

    private static ContentTagDto ToDto(ContentTag contentTag, Tag tag) => new()
    {
        ContentId = contentTag.ContentId,
        TagId = tag.TagId,
        TagName = tag.TagName
    };
}
