namespace ThoughtNote.Core;

public class Node
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public string Title { get; set; } = "";

    public string Category { get; set; } = "idea";

    public int Priority { get; set; } = 1;

    public DateTime CreatedAt { get; set; }
}