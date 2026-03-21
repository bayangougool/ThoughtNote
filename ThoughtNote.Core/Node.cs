using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ThoughtNote.Core
{
    public class Node
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ParentId { get; set; } = "";

        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int OrderIndex { get; set; }

        public string Type { get; set; } = "";

        public string Status { get; set; } = "";

        public string Body { get; set; } = "";

        public List<Node> Children { get; set; } = new();

        // ★これがタイトル（計算プロパティ）
        public string Title
        {
            get
            {
                if (string.IsNullOrEmpty(Content)) return "";

                var index = Content.IndexOf('\n');
                var line = index == -1 ? Content : Content.Substring(0, index);

                return line.Length > 40 ? line.Substring(0, 40) + "…" : line;
            }
        }
    }
}