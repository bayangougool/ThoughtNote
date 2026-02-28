using ThoughtNote.Core;

Console.OutputEncoding =
System.Text.Encoding.UTF8;

var baseDir =
AppContext.BaseDirectory;

var service =
new ThoughtService(baseDir);

Console.WriteLine("=== 思考ノート ===");

while (true)
{
    Console.WriteLine();

    Console.WriteLine(
"""
1 追加
2 Tree表示
0 終了
"""
);

    var input =
        Console.ReadLine();

    if (input == "0")
        return;

    //----------------------------------

    if (input == "1")
    {
        Console.Write("タイトル:");

        var title =
            Console.ReadLine()
            ?? "";

        Console.Write(
        "親ID(空Enterでroot):");

        var parentText =
            Console.ReadLine();

        long? parentId =
            long.TryParse(
                parentText,
                out var pid)
            ? pid
            : null;

        //--------------------------------
        // ⭐ ROOTだけ聞く
        //--------------------------------

        bool isTask = false;

        int? priority = null;

        string tags = "";

        if (parentId == null)
        {
            Console.Write(
            "タスク？(y/n):");

            isTask =
            Console.ReadLine()
            ?.ToLower() == "y";

            if (isTask)
            {
                Console.Write(
                "優先度1〜5:");

                if (int.TryParse(
                    Console.ReadLine(),
                    out var p))
                {
                    priority = p;

                    if (p == 1)
                    {
                        Console.WriteLine(
    "なぜ最優先？説明してください");

                        var reason =
                            Console.ReadLine()
                            ?? "";

                        var id =
                        service.AddThought(
                            title,
                            null,
                            priority,
                            true,
                            "");

                        service.AddThought(
                            "理由:" + reason,
                            id,
                            null,
                            false,
                            "#改善");

                        Console.WriteLine(
    "追加しました");

                        continue;
                    }
                }
            }

            Console.Write(
            "タグ:");

            tags =
            Console.ReadLine()
            ?? "";
        }

        //--------------------------------
        // 子ノードは全部Idea扱い
        //--------------------------------

        service.AddThought(
            title,
            parentId,
            priority,
            isTask,
            tags);

        Console.WriteLine(
        "追加しました");
    }

    //----------------------------------

    if (input == "2")
    {
        ShowTree(service);
    }
}

///////////////////////////////////////////////////

static void ShowTree(
ThoughtService service)
{
    var list =
        service.GetAll();

    const long ROOT = 0;

    var map =
    new Dictionary<long,
    List<Thought>>();

    foreach (var t in list)
    {
        long key =
            t.ParentId ?? ROOT;

        if (!map.ContainsKey(key))
            map[key] =
            new List<Thought>();

        map[key].Add(t);
    }

    Console.WriteLine();

    PrintNode(ROOT, 0, map);

    MaybeShowComment(list);
}

///////////////////////////////////////////////////

static void PrintNode(
long parent,
int depth,
Dictionary<long,
List<Thought>> map)
{
    if (!map.ContainsKey(parent))
        return;

    foreach (var node
        in map[parent])
    {
        var indent =
            new string(' ',
            depth * 2);

        string head = "";

        // ⭐ROOTだけ表示
        if (node.ParentId == null)
        {
            var type =
            node.IsTask
            ? "[Task]"
            : "[Idea]";

            var pri =
            node.Priority.HasValue
            ? $"(優先度{node.Priority.Value})"
            : "";

            head =
            $"{type} {pri}";
        }

        Console.WriteLine(
$"{indent}[{node.Id}] {node.Title} {head}");

        PrintNode(
            node.Id,
            depth + 1,
            map);
    }
}

///////////////////////////////////////////////////

static void MaybeShowComment(
List<Thought> list)
{
    // ⭐3回に1回だけ出る
    var rand = Random.Shared.Next(3);

    if (rand != 0)
        return;

    ShowRandomComment(list);
}


static void ShowRandomComment(
List<Thought> list)
{
    var tags =
    new Dictionary<string,
    List<string>>();

    tags["#健康"] =
    new()
    {
"睡眠は足りていますか？",
"運動はできていますか？",
"食事バランスどうです？"
    };

    tags["#原因"] =
    new()
    {
"最初に止まったのはどこ？",
"情報不足では？"
    };

    tags["#改善"] =
    new()
    {
"1分だけやるなら？",
"準備だけでもOKでは？"
    };

    var rand =
        new Random();

    var pool =
        new List<string>();

    foreach (var t in list)
    {
        foreach (var tag
            in tags.Keys)
        {
            if (t.Tags.Contains(tag))
            {
                pool.AddRange(
                    tags[tag]);
            }
        }
    }

    if (pool.Count == 0)
        return;

    Console.WriteLine();
    Console.WriteLine(
"💬 " + pool[rand.Next(
pool.Count)]);
}