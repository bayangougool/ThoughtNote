namespace ThoughtNote.Core
{
    public class Node
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public int? ParentId { get; set; }

        public string Title { get; set; } = "";

        public string Category { get; set; } = "idea";

        public int Priority { get; set; } = 1;

        public DateTime CreatedAt { get; set; }

        public string Body { get; set; } = "";

        public List<Node> Children { get; set; } = new();
    }
}