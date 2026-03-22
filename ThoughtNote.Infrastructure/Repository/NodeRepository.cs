using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;
using ThoughtNote.Core;
using System.Windows;
using System.Diagnostics;

namespace ThoughtNote.Infrastructure.Repository
{
    public class NodeRepository : INodeRepository
    {
        private readonly string _connectionString;

        public NodeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Node GetNode(string id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            """
             SELECT id,parent_id,title,body,order_index,type,status,created_at,updated_at
             FROM nodes
             WHERE id = $id
             """;

            cmd.Parameters.AddWithValue("$id", id);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read()) return null;

            return ReadNode(reader);
        }

        public List<Node> GetChildren(string parentId)
        {
            var list = new List<Node>();

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();

            cmd.CommandText =
            """
            SELECT *
            FROM nodes
            WHERE parent_id = $parentId
            AND status = 'active'
            ORDER BY order_index
            """;

            cmd.Parameters.AddWithValue("$parentId", parentId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(ReadNode(reader));
            }

            return list;
        }

        public List<Node> GetTree(string rootId)
        {
            var root = GetNode(rootId);

            LoadChildren(root);

            return new List<Node> { root };
        }

        private void LoadChildren(Node node)
        {
            var children = GetChildren(node.Id);

            node.Children = children;

            foreach (var child in children)
            {
                LoadChildren(child);
            }
        }

        public void Create(Node node)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();

            cmd.CommandText =
            """
            INSERT INTO nodes
            (id,parent_id,title,body,order_index,type,status,created_at,updated_at)
            VALUES
            ($id,$parent,$title,$body,$order,$type,$status,$created,$updated)
            """;

            cmd.Parameters.AddWithValue("$id", node.Id);
            cmd.Parameters.AddWithValue("$parent", node.ParentId);
            cmd.Parameters.AddWithValue("$title", node.Title);
               // cmd.Parameters.AddWithValue("$body", node.Body);
            cmd.Parameters.AddWithValue("$order", node.OrderIndex);
            cmd.Parameters.AddWithValue("$created", node.CreatedAt);
            cmd.Parameters.AddWithValue("$updated", node.UpdatedAt);

            cmd.ExecuteNonQuery();
        }

        public void Update(Node node)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();

            cmd.CommandText =
            """
            UPDATE nodes
            SET
                content = $content,
                order_index = $order,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = $id
            """;

            cmd.Parameters.AddWithValue("$content", node.Content);
            cmd.Parameters.AddWithValue("$order", node.OrderIndex);
            cmd.Parameters.AddWithValue("$id", node.Id);

            cmd.ExecuteNonQuery();
        }

        public void Delete(string id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();

            cmd.CommandText =
            """
            UPDATE nodes
            SET status = 'deleted'
            WHERE id = $id
            """;

            cmd.Parameters.AddWithValue("$id", id);

            cmd.ExecuteNonQuery();
        }

        public void Move(string nodeId, string newParentId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();

            cmd.CommandText =
            """
            UPDATE nodes
            SET parent_id = $parent
            WHERE id = $id
            """;

            cmd.Parameters.AddWithValue("$parent", newParentId);
            cmd.Parameters.AddWithValue("$id", nodeId);

            cmd.ExecuteNonQuery();
        }

        private Node ReadNode(SqliteDataReader reader)
        {
            return new Node
            {
                Id = reader.GetString(0),
                ParentId = reader.IsDBNull(1) ? null : reader.GetString(1),
                Content = reader.GetString(2),
                OrderIndex = reader.GetInt32(3),
                CreatedAt = reader.GetDateTime(4),
                UpdatedAt = reader.GetDateTime(5),
                DeletedAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
            };
        }

        public bool Exists(string id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();

            cmd.CommandText =
            """
            SELECT COUNT(1)
            FROM nodes
            WHERE id = $id
            """;

            cmd.Parameters.AddWithValue("$id", id);

            var count = (long)cmd.ExecuteScalar();

            return count > 0;
        }

        public List<Node> GetTree()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            """
            SELECT id, parent_id, content, order_index
            FROM nodes
            
            ORDER BY order_index
            """;

            using var reader = cmd.ExecuteReader();

            // ① 全ノードを一旦リストに
            var allNodes = new List<Node>();

            while (reader.Read())
            {
                var id = reader["id"]?.ToString();
                var content = reader["content"]?.ToString();
                Debug.WriteLine($"id={id}\ncontent={content}");
                
                var node = new Node
                {
                    Id = reader.GetString(0),
                    ParentId = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Content = reader.GetString(2),
                    OrderIndex = reader.GetInt32(3),
                    Children = new List<Node>()
                };

                allNodes.Add(node);
            }

            // ② id → Node の辞書
            var dict = allNodes.ToDictionary(n => n.Id);

            // ③ ルートノードリスト
            var roots = new List<Node>();

            // ④ 親子組み立て
            foreach (var node in allNodes)
            {
                if (node.ParentId == null)
                {
                    roots.Add(node);
                }
                else if (dict.TryGetValue(node.ParentId, out var parent))
                {
                    parent.Children.Add(node);
                }
            }

            return roots;
        }
    }
}