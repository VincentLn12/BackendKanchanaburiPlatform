namespace Core.Entities;

public class BaseEntity
{
    public bool is_active { get; set; } = false;
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime? updated_at { get; set; } = null;
};

