using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThoughtNote.Core;

public class Thought
{
    public long Id { get; set; }

    public long? ParentId { get; set; }

    public string Title { get; set; } = "";

    public string Type { get; set; } = "";

    public int? Priority { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Tags { get; set; } = "";
}
