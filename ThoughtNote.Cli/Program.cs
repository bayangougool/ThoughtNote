using System;
using ThoughtNote.Core;

var service =
new ThoughtService("C:\\Users\\ivonu\\ThoughtNote\\thoughtnote.db");

Console.WriteLine("思考ノートCLI");

while (true)
{
    Console.WriteLine(@"

add 追加
tree ツリー
del 削除
tasks 未完了タスク
today 今日ログ
exit 終了

");

    Console.Write("> ");

    var cmd =
    Console.ReadLine()?.Trim();

    if (cmd == "exit")
        break;

    switch (cmd)
    {

        case "tree":

            service.PrintTree();

            break;

        case "tasks":

            service.ShowTasks();

            break;

        case "today":

            service.ShowToday();

            break;

        case "del":

            Console.Write("削除ID:");

            int id = int.Parse(
            Console.ReadLine() ?? "0");

            service.DeleteNode(id);

            Console.WriteLine("削除しました");

            break;

        case "add":

            Console.Write("タイトル:");

            var title =
            Console.ReadLine();

            Console.Write(
            "親ID(トピック=Enter):");

            var parent =
            Console.ReadLine();

            int? parentId = null;

            string category = "idea";

            if (string.IsNullOrWhiteSpace(parent))
            {
                Console.Write(
                "カテゴリ idea/task:");

                category =
                Console.ReadLine() ?? "idea";
            }
            else
            {
                parentId =
                int.Parse(parent);
            }

            Console.Write(
            "優先度1-5:");

            int prio =
            int.Parse(
            Console.ReadLine() ?? "1");

            service.AddNode(
            title ?? "無題",
            category,
            prio,
            parentId);

            Console.WriteLine(
            "追加しました");

            break;

        default:

            Console.WriteLine(
            "不明コマンド");

            break;

    }
}

Console.WriteLine("終了");