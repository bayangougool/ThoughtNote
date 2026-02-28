using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;

namespace ThoughtNote.Core;

/// <summary>
/// 思考ノートのDB操作とロジック担当
/// CLIやUnityはこれを呼ぶだけ。
/// </summary>
public class ThoughtService
{
    private readonly string _dbPath;

    public ThoughtService(string dbPath)
    {
        _dbPath = dbPath;
        Initialize();
    }

    /// <summary>
    /// DB初期化
    /// </summary>
    private void Initialize()
    {
        using var con =
            new SqliteConnection($"Data Source={_dbPath}");

        con.Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = @"

CREATE TABLE IF NOT EXISTS nodes(

id INTEGER PRIMARY KEY AUTOINCREMENT,

parent_id INTEGER,

title TEXT NOT NULL,

category TEXT,

priority INTEGER,

created_at TEXT NOT NULL,

deleted INTEGER DEFAULT 0

);";

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// ノード追加
    /// parentId null = トピック
    /// </summary>
    public void AddNode(
        string title,
        string category,
        int priority,
        int? parentId)
    {
        using var con =
            new SqliteConnection($"Data Source={_dbPath}");

        con.Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = @"
INSERT INTO nodes
(parent_id,title,category,
priority,created_at,deleted)
VALUES
(@pid,@title,@cat,
@prio,@time,0);
";

        cmd.Parameters.AddWithValue(
            "@pid",
            (object?)parentId ?? DBNull.Value);

        cmd.Parameters.AddWithValue(
            "@title", title);

        cmd.Parameters.AddWithValue(
            "@cat", category);

        cmd.Parameters.AddWithValue(
            "@prio", priority);

        cmd.Parameters.AddWithValue(
            "@time",
            DateTime.Now
            .ToString("yyyy-MM-dd HH:mm:ss"));

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 論理削除
    /// 履歴を残す
    /// </summary>
    public void DeleteNode(int id)
    {
        using var con =
            new SqliteConnection($"Data Source={_dbPath}");

        con.Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        "UPDATE nodes SET deleted=1 WHERE id=@id";

        cmd.Parameters.AddWithValue("@id", id);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 子取得
    /// </summary>
    private List<Node> GetChildren(int? parentId)
    {
        var list = new List<Node>();

        using var con =
            new SqliteConnection($"Data Source={_dbPath}");

        con.Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
parentId == null ?

@"SELECT * FROM nodes
WHERE parent_id IS NULL
AND deleted=0
ORDER BY priority DESC,id"

:

@"SELECT * FROM nodes
WHERE parent_id=@pid
AND deleted=0
ORDER BY priority DESC,id";

        if (parentId != null)
            cmd.Parameters.AddWithValue("@pid",
                parentId);

        using var reader =
            cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new Node
            {
                Id = reader.GetInt32(0),

                ParentId =
                reader.IsDBNull(1)
                ? null
                : reader.GetInt32(1),

                Title = reader.GetString(2),

                Category = reader.GetString(3),

                Priority = reader.GetInt32(4),

                CreatedAt =
                DateTime.Parse(
                    reader.GetString(5))
            });
        }

        return list;
    }

    /// <summary>
    /// ツリー表示
    /// categoryは親のみ表示
    /// </summary>
    public void PrintTree(
        int? parentId = null,
        string indent = "")
    {
        var children =
            GetChildren(parentId);

        for (int i = 0; i < children.Count; i++)
        {
            var node = children[i];

            bool last =
                i == children.Count - 1;

            Console.Write(indent);

            Console.Write(
                last ? "└─" : "├─");

            // トピックのみcategory表示
            if (node.ParentId == null)
            {
                Console.WriteLine(
$"{node.Id}:{node.Title}" +
$" [{node.Category}] " +
$"(P{node.Priority})");
            }
            else
            {
                Console.WriteLine(
$"{node.Id}:{node.Title}" +
$" (P{node.Priority})");
            }

            PrintTree(
                node.Id,
                indent + (last ? "  " : "│ "));
        }
    }

    /// ⭐ 未完了タスク一覧
    public void ShowTasks()
    {
        using var con =
            new SqliteConnection($"Data Source={_dbPath}");

        con.Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = @"

SELECT id,title,priority
FROM nodes

WHERE category='task'
AND deleted=0
AND parent_id IS NULL

ORDER BY priority DESC;

";

        using var reader =
            cmd.ExecuteReader();

        Console.WriteLine(
"\n=== 未完了タスク ===");

        while (reader.Read())
        {
            Console.WriteLine(
$"{reader.GetInt32(0)} " +
$"{reader.GetString(1)} " +
$"(P{reader.GetInt32(2)})");
        }
    }

    /// ⭐ 今日考えたノード（神機能）
    public void ShowToday()
    {
        using var con =
            new SqliteConnection($"Data Source={_dbPath}");

        con.Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = @"

SELECT id,title,created_at
FROM nodes

WHERE deleted=0
AND DATE(created_at)
=DATE('now','localtime')

ORDER BY created_at;

";

        using var reader =
            cmd.ExecuteReader();

        Console.WriteLine(
"\n=== 今日考えたこと ===");

        while (reader.Read())
        {
            Console.WriteLine(
$"{reader.GetInt32(0)} "
+
reader.GetDateTime(2)
.ToString("HH:mm")
+
" "
+
reader.GetString(1));
        }
    }
}
