using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;

namespace ThoughtNote.Core;

public class ThoughtService
{
    private string _dbPath;

    public ThoughtService(string dbPath)
    {
        _dbPath = dbPath;
        Initialize();
    }

    private void Initialize()
    {
        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();
        var cmd = con.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS nodes(
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    parent_id INTEGER,
    title TEXT NOT NULL,
    category TEXT,
    priority INTEGER,
    created_at TEXT NOT NULL
);";
        cmd.ExecuteNonQuery();
    }

    public void AddNode(string title, string category = "idea", int? parentId = null, int priority = 1)
    {
        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();
        var cmd = con.CreateCommand();
        cmd.CommandText = @"
INSERT INTO nodes(parent_id, title, category, priority, created_at)
VALUES(@pid,@title,@cat,@prio,@time);";
        cmd.Parameters.AddWithValue("@pid", (object?)parentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@cat", category);
        cmd.Parameters.AddWithValue("@prio", priority);
        cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.ExecuteNonQuery();
    }

    public List<Node> GetChildren(int? parentId = null)
    {
        var list = new List<Node>();
        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();
        var cmd = con.CreateCommand();
        cmd.CommandText = parentId == null
            ? "SELECT * FROM nodes WHERE parent_id IS NULL"
            : "SELECT * FROM nodes WHERE parent_id = @pid";
        if (parentId != null) cmd.Parameters.AddWithValue("@pid", parentId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Node
            {
                Id = reader.GetInt32(0),
                ParentId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                Title = reader.GetString(2),
                Category = reader.GetString(3),
                Priority = reader.GetInt32(4),
                CreatedAt = DateTime.Parse(reader.GetString(5))
            });
        }
        return list;
    }

    public void PrintTree(int? parentId = null, string indent = "")
    {
        var children = GetChildren(parentId);
        for (int i = 0; i < children.Count; i++)
        {
            var node = children[i];
            bool last = i == children.Count - 1;
            Console.Write(indent);
            Console.Write(last ? "└─" : "├─");
            Console.WriteLine($"{node.Id}: {node.Title} [{node.Category}]");
            PrintTree(node.Id, indent + (last ? "  " : "│ "));
        }
    }
}
