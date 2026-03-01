using Microsoft.Data.Sqlite;

namespace ThoughtNote.Core;

public class Thought
{
    public long Id { get; set; }

    public string Title { get; set; } = "";

    public long? ParentId { get; set; }

    public int? Priority { get; set; }

    public bool IsTask { get; set; }

    public string Tags { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}

public class ThoughtService
{
    private readonly string _dbPath;

    // ⭐これが connectionString の正体
    private readonly string _connectionString;

    // コンストラクタ
    public ThoughtService(string baseDir)
    {
        // DBの場所
        var dbPath =
            Path.Combine(baseDir, "thoughtnote.db");

        // SQLite 接続文字列生成
        _connectionString =
            $"Data Source={dbPath}";

        InitDB();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(
            $"Data Source={_dbPath}");
        conn.Open(); // ⭐これ！！
        return conn;
    }

    private void InitDB()
    {
        using var conn = Open();
        conn.Open();

        var cmd = conn.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS thoughts(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            title TEXT NOT NULL,
            parent_id INTEGER,
            priority INTEGER,
            is_task INTEGER NOT NULL,
            tags TEXT,
            created_at TEXT NOT NULL,            
            is_deleted INTEGER NOT NULL DEFAULT 0 -- ⭐履歴フラグ
        );
        """;

        cmd.ExecuteNonQuery();
    }

    //====================

    public List<Thought> GetActiveThoughts()
    {
        using var con = new SqliteConnection(_connectionString);
        con.Open();

        var cmd = con.CreateCommand();

        // ⭐履歴は出さない！！
        cmd.CommandText =
    @"
SELECT *
FROM thoughts
WHERE is_deleted = 0
ORDER BY id;
";

        var reader = cmd.ExecuteReader();

        var list = new List<Thought>();

        while (reader.Read())
        {
            list.Add(ReadThought(reader));
        }

        return list;
    }


    public long AddThought(
        string title,
        long? parentId,
        int? priority,
        bool isTask,
        string tags)
    {
        using var conn = Open();
        conn.Open();

        var cmd = conn.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO thoughts
        (title,parent_id,priority,is_task,tags,created_at)
        VALUES
        ($title,$parent,$priority,$task,$tags,$date);

        SELECT last_insert_rowid();
        """;

        cmd.Parameters.AddWithValue(
            "$title", title);

        cmd.Parameters.AddWithValue(
            "$parent",
            (object?)parentId ?? DBNull.Value);

        cmd.Parameters.AddWithValue(
            "$priority",
            (object?)priority ?? DBNull.Value);

        cmd.Parameters.AddWithValue(
            "$task", isTask ? 1 : 0);

        cmd.Parameters.AddWithValue(
            "$tags", tags ?? "");

        cmd.Parameters.AddWithValue(
            "$date",
            DateTime.Now.ToString("s"));

        var result = cmd.ExecuteScalar();

        if (result == null)
            throw new Exception("Insert失敗");

        return Convert.ToInt64(result);
    }

    //====================

    public List<Thought> GetAll()
    {
        using var conn = Open();
        conn.Open();

        var cmd = conn.CreateCommand();

        cmd.CommandText =
        "SELECT * FROM thoughts ORDER BY id";

        using var reader =
            cmd.ExecuteReader();

        var list = new List<Thought>();

        while (reader.Read())
        {
            var t = new Thought();

            t.Id =
                reader.GetInt64(0);

            t.Title =
                reader.GetString(1);

            t.ParentId =
                reader.IsDBNull(2)
                ? null
                : reader.GetInt64(2);

            t.Priority =
                reader.IsDBNull(3)
                ? null
                : reader.GetInt32(3);

            t.IsTask =
                reader.GetInt64(4) == 1;

            t.Tags =
                reader.IsDBNull(5)
                ? ""
                : reader.GetString(5);

            t.CreatedAt =
                DateTime.Parse(
                    reader.GetString(6));

            list.Add(t);
        }

        return list;
    }

    public void DeleteThought(long id)
    {
        using var conn = Open();

        //--------------------------------
        // 子ノード存在チェック
        //--------------------------------

        var childCmd =
        conn.CreateCommand();

        childCmd.CommandText =
        "SELECT COUNT(*) FROM thoughts WHERE parent_id=@p";

        childCmd.Parameters.AddWithValue("@p", id);

        var childCount =
        (long)(childCmd.ExecuteScalar() ?? 0);

        //--------------------------------
        // 作成日取得
        //--------------------------------

        var getCmd =
        conn.CreateCommand();

        getCmd.CommandText =
        "SELECT created_at FROM thoughts WHERE id=@id";

        getCmd.Parameters.AddWithValue("@id", id);

        var createdText =
        getCmd.ExecuteScalar()
        ?.ToString();

        if (createdText == null)
            return;

        var created =
        DateTime.Parse(createdText);

        var today =
        DateTime.Today;

        //--------------------------------
        // 完全削除条件
        //--------------------------------

        bool fullDelete =
            childCount == 0
            && created.Date == today;

        if (fullDelete)
        {
            var del =
            conn.CreateCommand();

            del.CommandText =
            "DELETE FROM thoughts WHERE id=@id";

            del.Parameters.AddWithValue("@id", id);

            del.ExecuteNonQuery();

            Console.WriteLine(
            "🧹完全削除しました");

            return;
        }

        //--------------------------------
        // 履歴削除
        //--------------------------------

        var soft =
        conn.CreateCommand();

        soft.CommandText =
        "UPDATE thoughts SET deleted=1 WHERE id=@id";

        soft.Parameters.AddWithValue("@id", id);

        soft.ExecuteNonQuery();

        Console.WriteLine(
        "📚履歴として残しました");
    }
}