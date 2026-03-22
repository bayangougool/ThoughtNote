using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ThoughtNote.Core
{
    public class Node
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string? ParentId { get; set; }

        public string Content { get; set; } = "";

        public int OrderIndex { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public List<Node> Children { get; set; } = new();

        public string Title
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Content)) return "";

                var firstLine = Content.Split('\n').FirstOrDefault() ?? "";
                return firstLine.Length > 40 ? firstLine.Substring(0, 40) + "…" : firstLine;
            }
        }
    }
}