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

    public ThoughtService(string baseDir)
    {
        _dbPath =
            Path.Combine(baseDir, "thoughtnote.db");

        InitDB();
    }

    private SqliteConnection Open()
    {
        return new SqliteConnection(
            $"Data Source={_dbPath}");
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
            created_at TEXT NOT NULL
        );
        """;

        cmd.ExecuteNonQuery();
    }

    //====================

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

}