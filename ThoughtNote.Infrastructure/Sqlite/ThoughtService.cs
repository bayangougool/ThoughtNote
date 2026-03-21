using Microsoft.Data.Sqlite;
using ThoughtNote.Core;

namespace ThoughtNote.Infrastructure.Sqlite

{
    /// <summary>
    /// 思考ノートの全ロジック
    /// SQLite 管理
    /// ノード CRUD
    /// ゴミ箱
    /// タグ
    /// コメント生成
    /// </summary>
    public class ThoughtService
    {
        private readonly string _connectionString;

        //==============================
        // コンストラクタ
        //==============================
        public ThoughtService(string baseDir)
        {
            Directory.CreateDirectory(baseDir);

            var dbPath =
                Path.Combine(baseDir, "thoughtnote.db");

            _connectionString =
                $"Data Source={dbPath}";

            InitDB();
        }

        //==============================
        // DB初期化
        //==============================
        private void InitDB()
        {
            using var con =
                new SqliteConnection(_connectionString);

            con.Open();

            // Thought
            var cmd = con.CreateCommand();

            cmd.CommandText =
            """
        CREATE TABLE IF NOT EXISTS thoughts(

        id INTEGER PRIMARY KEY AUTOINCREMENT,

        parent_id INTEGER,

        title TEXT NOT NULL,

        type TEXT,

        priority INTEGER,

        created_at TEXT NOT NULL,

        deleted INTEGER NOT NULL DEFAULT 0

        );
        """;

            cmd.ExecuteNonQuery();

            // ⭐ deleted 無かった旧DB救済
            TryAddColumn(con,
                "thoughts",
                "deleted INTEGER NOT NULL DEFAULT 0");

            // tags
            cmd = con.CreateCommand();

            cmd.CommandText =
            """
        CREATE TABLE IF NOT EXISTS tags(

        id INTEGER PRIMARY KEY AUTOINCREMENT,

        name TEXT UNIQUE,

        description TEXT

        );
        """;

            cmd.ExecuteNonQuery();

            // thought_tags
            cmd = con.CreateCommand();

            cmd.CommandText =
            """
        CREATE TABLE IF NOT EXISTS thought_tags(

        thought_id INTEGER,

        tag_id INTEGER

        );
        """;

            cmd.ExecuteNonQuery();

            SeedDefaultTags(con);
        }

        //==============================
        // カラム追加（安全）
        //==============================
        private void TryAddColumn(
            SqliteConnection con,
            string table,
            string columnDef)
        {
            try
            {
                var c = con.CreateCommand();

                c.CommandText =
                    $"ALTER TABLE {table} ADD COLUMN {columnDef};";

                c.ExecuteNonQuery();
            }
            catch
            {
                // already exists 無視
            }
        }

        //==============================
        // デフォルトタグ
        //==============================
        private void SeedDefaultTags(SqliteConnection con)
        {
            var tags = new Dictionary<string, string>
            {
                ["健康"] =
                "食事・運動・睡眠",

                ["習慣"] =
                "怠惰改善",

                ["お金"] =
                "節約・収入",

                ["恐怖"] =
                "不安の整理",

                ["逃避"] =
                "自己防衛",

                ["失敗"] =
                "落ち込んだ時",

                ["原因"] =
                "止まった理由探し",

                ["改善"] =
                "最優先"
            };

            foreach (var t in tags)
            {
                var cmd = con.CreateCommand();

                cmd.CommandText =
                """
            INSERT OR IGNORE INTO tags
            (name,description)
            VALUES($n,$d);
            """;

                cmd.Parameters.AddWithValue("$n", t.Key);
                cmd.Parameters.AddWithValue("$d", t.Value);

                cmd.ExecuteNonQuery();
            }
        }

        //==============================
        // 追加
        //==============================
        public long AddThought(
            string title,
            long? parentId,
            string type,
            int? priority,
            String tags)
        {
            using var con =
                new SqliteConnection(_connectionString);

            con.Open();

            var cmd = con.CreateCommand();

            cmd.CommandText =
            """
        INSERT INTO thoughts
        (title,parent_id,type,priority,created_at,tags)

        VALUES
        ($t,$p,$type,$pri,$c,$tags);
        """;

            cmd.Parameters.AddWithValue("$t", title);

            cmd.Parameters.AddWithValue(
                "$p",
                parentId.HasValue ?
                parentId.Value :
                DBNull.Value);

            cmd.Parameters.AddWithValue(
                "$type",
                type ?? "");

            cmd.Parameters.AddWithValue(
                "$pri",
                priority.HasValue ?
                priority.Value :
                DBNull.Value);

            cmd.Parameters.AddWithValue(
                "$tags",
                tags ?? "");

            cmd.Parameters.AddWithValue(
                "$c",
                DateTime.Now.ToString("yyyy-MM-dd"));

            cmd.ExecuteNonQuery();

            var idCmd = con.CreateCommand();

            idCmd.CommandText =
                "SELECT last_insert_rowid();";

            var obj = idCmd.ExecuteScalar();

            return obj != null ? (long)obj : 0;
        }


        //====================

        public List<Thought> GetAll()
        {
            using var conn =
                new SqliteConnection(_connectionString);

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

                t.Type =
                    reader.GetString(4);

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

        //==============================
        // Active only
        //==============================
        public List<Thought> GetActiveThoughts()
        {
            using var con =
                new SqliteConnection(_connectionString);

            con.Open();

            var cmd = con.CreateCommand();

            cmd.CommandText =
            """
        SELECT id,parent_id,title,
        type,priority,created_at

        FROM thoughts

        WHERE deleted=0

        ORDER BY id;
        """;

            var list = new List<Thought>();

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new Thought
                {
                    Id = r.GetInt64(0),

                    ParentId =
                    r.IsDBNull(1) ?
                    null :
                    r.GetInt64(1),

                    Title = r.GetString(2),

                    Type = r.IsDBNull(3) ?
                    "" :
                    r.GetString(3),

                    Priority =
                    r.IsDBNull(4) ?
                    null :
                    r.GetInt32(4),

                    CreatedAt =
                    DateTime.Parse(
                        r.GetString(5))
                });
            }

            return list;
        }

        //==============================
        // 子存在チェック
        //==============================
        private bool HasChildren(
            SqliteConnection con,
            long id)
        {
            var cmd = con.CreateCommand();

            cmd.CommandText =
            """
        SELECT COUNT(*)
        FROM thoughts
        WHERE parent_id=$id
        AND deleted=0;
        """;

            cmd.Parameters.AddWithValue("$id", id);

            var c = (long)cmd.ExecuteScalar()!;

            return c > 0;
        }

        //==============================
        // 作成日取得
        //==============================
        private DateTime GetCreated(
            SqliteConnection con,
            long id)
        {
            var cmd = con.CreateCommand();

            cmd.CommandText =
            """
        SELECT created_at
        FROM thoughts
        WHERE id=$id;
        """;

            cmd.Parameters.AddWithValue("$id", id);

            var s = (string)cmd.ExecuteScalar()!;

            return DateTime.Parse(s);
        }

        //==============================
        // 削除（神仕様）
        //==============================
        public void DeleteThought(long id)
        {
            using var con =
                new SqliteConnection(_connectionString);

            con.Open();

            bool child =
                HasChildren(con, id);

            var created =
                GetCreated(con, id);

            bool today =
                created.Date ==
                DateTime.Today;

            var cmd = con.CreateCommand();

            // 完全削除
            if (!child && today)
            {
                cmd.CommandText =
                """
            DELETE FROM thoughts
            WHERE id=$id;
            """;
            }
            else
            {
                // 履歴
                cmd.CommandText =
                """
            UPDATE thoughts
            SET deleted=1
            WHERE id=$id;
            """;
            }

            cmd.Parameters.AddWithValue("$id", id);

            cmd.ExecuteNonQuery();
        }

        //==============================
        // ゴミ箱
        //==============================
        public List<Thought> GetTrash()
        {
            using var con =
                new SqliteConnection(_connectionString);

            con.Open();

            var cmd = con.CreateCommand();

            cmd.CommandText =
            """
        SELECT id,title,created_at
        FROM thoughts
        WHERE deleted=1;
        """;

            var list = new List<Thought>();

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(
                new Thought
                {
                    Id = r.GetInt64(0),
                    Title = r.GetString(1),
                    CreatedAt =
                    DateTime.Parse(
                        r.GetString(2))
                });
            }

            return list;
        }

        //==============================
        // 復元
        //==============================
        public void Restore(long id)
        {
            using var con =
                new SqliteConnection(_connectionString);

            con.Open();

            var cmd = con.CreateCommand();

            cmd.CommandText =
            """
        UPDATE thoughts
        SET deleted=0
        WHERE id=$id;
        """;

            cmd.Parameters.AddWithValue("$id", id);

            cmd.ExecuteNonQuery();
        }

        //==============================
        // タグ一覧
        //==============================
        public List<(string, string)> GetTagDescriptions()
        {
            using var con =
                new SqliteConnection(_connectionString);

            con.Open();

            var cmd = con.CreateCommand();

            cmd.CommandText =
            """
        SELECT name,description
        FROM tags;
        """;

            var list =
                new List<(string, string)>();

            using var r =
                cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(
                (r.GetString(0),
                 r.GetString(1)));
            }

            return list;
        }

    }
}