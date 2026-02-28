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
    private string _dbPath;

    public ThoughtService()
    {
        _dbPath = Path.Combine(
            AppContext.BaseDirectory,
            "thoughtnote.db");

        Initialize();
    }

    private SqliteConnection GetConn()
    {
        return new SqliteConnection(
            $"Data Source={_dbPath}");
    }

    // =====================
    // 初期化
    // =====================

    private void Initialize()
    {
        using var conn = GetConn();
        conn.Open();

        var cmd = conn.CreateCommand();

        cmd.CommandText =
        @"
CREATE TABLE IF NOT EXISTS nodes(
id INTEGER PRIMARY KEY AUTOINCREMENT,
parent_id INTEGER,
title TEXT,
category TEXT,
priority INTEGER,
created_at TEXT,
deleted INTEGER DEFAULT 0
);

CREATE TABLE IF NOT EXISTS tags(
id INTEGER PRIMARY KEY AUTOINCREMENT,
name TEXT UNIQUE,
description TEXT,
is_default INTEGER
);

CREATE TABLE IF NOT EXISTS node_tags(
node_id INTEGER,
tag_id INTEGER
);

CREATE TABLE IF NOT EXISTS tag_comments(
id INTEGER PRIMARY KEY AUTOINCREMENT,
tag_id INTEGER,
comment TEXT
);
";
        cmd.ExecuteNonQuery();

        InsertDefaultTags();
    }

    // =====================
    // デフォルトタグ
    // =====================

    private void InsertDefaultTags()
    {
        using var conn = GetConn();
        conn.Open();

        string[] defaults =
        {
            "健康",
            "習慣",
            "お金",
            "恐怖",
            "逃避",
            "失敗",
            "原因",
            "改善"
        };

        foreach (var tag in defaults)
        {
            var cmd = conn.CreateCommand();

            cmd.CommandText =
            @"INSERT OR IGNORE INTO tags
            (name,description,is_default)
            VALUES(@name,@desc,1)";

            cmd.Parameters.AddWithValue("@name", tag);
            cmd.Parameters.AddWithValue("@desc", $"{tag}タグ");

            cmd.ExecuteNonQuery();
        }
    }

    // =====================
    // トピック作成
    // =====================

    public void CreateTopic(
        string title,
        string category,
        int priority)
    {
        using var conn = GetConn();
        conn.Open();

        var cmd = conn.CreateCommand();

        cmd.CommandText =
        @"INSERT INTO nodes
(title,parent_id,category,priority,created_at)
VALUES(@t,NULL,@c,@p,@d)";

        cmd.Parameters.AddWithValue("@t", title);
        cmd.Parameters.AddWithValue("@c", category);
        cmd.Parameters.AddWithValue("@p", priority);
        cmd.Parameters.AddWithValue("@d",
            DateTime.Now.ToString());

        cmd.ExecuteNonQuery();
        var idCmd = conn.CreateCommand();

        idCmd.CommandText =
        "SELECT last_insert_rowid();";

        var result = idCmd.ExecuteScalar();

        if (result == null || result == DBNull.Value)
        {
            throw new Exception("Insert ID の取得に失敗しました");
        }

        var id = Convert.ToInt64(result);

        if (priority == 1)
        {
            AskReason((int)id,
            "優先度1です。理由を説明してください。");
        }
    }

    // =====================
    // タグ付け
    // =====================

    public void AddTag(int nodeId, string tagName)
    {
        using var conn = GetConn();
        conn.Open();

        var tagCmd = conn.CreateCommand();

        tagCmd.CommandText =
        @"SELECT id FROM tags
        WHERE name=@n";

        tagCmd.Parameters.AddWithValue("@n", tagName);

        var tagId = tagCmd.ExecuteScalar();

        if (tagId == null)
        {
            Console.WriteLine("タグなし");
            return;
        }

        var insert = conn.CreateCommand();

        insert.CommandText =
        @"INSERT INTO node_tags
(node_id,tag_id)
VALUES(@n,@t)";

        insert.Parameters.AddWithValue("@n", nodeId);
        insert.Parameters.AddWithValue("@t", (long)tagId);

        insert.ExecuteNonQuery();

        if (tagName == "改善")
        {
            AskReason(nodeId,
            "なぜ最優先なのか説明できますか？");
        }
    }

    // =====================
    // 理由要求
    // =====================

    private void AskReason(int parentId, string message)
    {
        Console.WriteLine(message);
        Console.Write("> ");

        var reason = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(reason))
            return;

        using var conn = GetConn();
        conn.Open();

        var cmd = conn.CreateCommand();

        cmd.CommandText =
        @"INSERT INTO nodes
(title,parent_id,category,priority,created_at)
VALUES(@t,@p,'idea',0,@d)";

        cmd.Parameters.AddWithValue("@t", reason);
        cmd.Parameters.AddWithValue("@p", parentId);
        cmd.Parameters.AddWithValue("@d",
            DateTime.Now.ToString());

        cmd.ExecuteNonQuery();
    }

    // =====================
    // コメント追加
    // =====================

    public void AddComment(
        string tag,
        string comment)
    {
        using var conn = GetConn();
        conn.Open();

        var cmd = conn.CreateCommand();

        cmd.CommandText =
@"
INSERT INTO tag_comments(tag_id,comment)
SELECT id,@c
FROM tags WHERE name=@n
";

        cmd.Parameters.AddWithValue("@n", tag);
        cmd.Parameters.AddWithValue("@c", comment);

        cmd.ExecuteNonQuery();
    }

    // =====================
    // tree表示
    // =====================

    public void ShowTree()
    {
        using var conn = GetConn();
        conn.Open();

        var cmd = conn.CreateCommand();

        cmd.CommandText =
@"SELECT id,title,category
FROM nodes
WHERE parent_id IS NULL
AND deleted=0";

        using var reader =
            cmd.ExecuteReader();

        Console.WriteLine("==== TREE ====");

        while (reader.Read())
        {
            Console.WriteLine(
            $"{reader.GetInt64(0)} " +
            $"{reader.GetString(1)} " +
            $"({reader.GetString(2)})");
        }

        ShowAIComment();
    }

    // =====================
    // AIコメント
    // =====================

    private void ShowAIComment()
    {
        using var conn = GetConn();
        conn.Open();

        var cmd = conn.CreateCommand();

        cmd.CommandText =
@"
SELECT tc.comment
FROM nodes n
JOIN node_tags nt
ON n.id=nt.node_id
JOIN tag_comments tc
ON nt.tag_id=tc.tag_id

WHERE deleted=0

ORDER BY
CASE WHEN category='task'
THEN RANDOM()*0.3
ELSE RANDOM()
END

LIMIT 1
";

        var result = cmd.ExecuteScalar();

        if (result != null)
        {
            Console.WriteLine();
            Console.WriteLine("AI:");
            Console.WriteLine(result.ToString());
        }
    }
}
