using System;
using ThoughtNote.Core;

class Program
{
    static void Main()
    {
        var service = new ThoughtService("thought.db");
        Console.WriteLine("=== 思考ノート CLI ===");

        while (true)
        {
            Console.WriteLine("\nコマンド: add / tree / exit");
            Console.Write("> ");
            var cmd = Console.ReadLine()?.Trim();

            if (cmd == "exit") break;

            if (cmd == "add")
            {
                Console.Write("タイトル: ");
                var title = Console.ReadLine();

                Console.Write("カテゴリ (idea/task): ");
                var category = Console.ReadLine();

                service.AddNode(title ?? "無題", category ?? "idea");
                Console.WriteLine("ノードを追加しました。");
            }
            else if (cmd == "tree")
            {
                Console.WriteLine("\nツリー表示:");
                service.PrintTree();
            }
            else
            {
                Console.WriteLine("不明なコマンドです。");
            }
        }

        Console.WriteLine("終了します。");
    }
}