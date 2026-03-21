using ThoughtNote.Core;
using ThoughtNote.Infrastructure;

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
3 削除
4 タグ説明
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
                            "Idea",
                            priority,
                            "");

                        service.AddThought(
                            "理由:" + reason,
                            id,
                            "Task",
                            priority,
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
            "Idea",
            priority,
            tags);

        Console.WriteLine(
        "追加しました");
    }

    //----------------------------------

    if (input == "2")
    {
        ShowTree(service);
    }
    else if (input == "3")
    {
        Console.Write("削除ID:");

        if (long.TryParse(
            Console.ReadLine(),
            out var id))
        {
            service.DeleteThought(id);
        }
    }
    else if (input == "4")
    {
        ShowTagHelp();
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
            node.Type == "Task"
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

static void ShowTagHelp()
{
    Console.WriteLine();

    Console.WriteLine("#健康 食事・運動・睡眠のコメント");

    Console.WriteLine("#習慣 怠惰な方へのコメント");

    Console.WriteLine("#お金 無駄遣い・節約・収入");

    Console.WriteLine("#恐怖 不安や未知が理由の時");

    Console.WriteLine("#逃避 自己防衛です。えへん。");

    Console.WriteLine("#失敗 凹んだ時にどうぞ");

    Console.WriteLine("#原因 対策が進まない時");

    Console.WriteLine("#改善 最優先。理由説明を要求します");

    Console.WriteLine();

    Console.WriteLine("優先度1 一週間以内");

    Console.WriteLine("優先度2 二週間以内");

    Console.WriteLine("優先度3 一か月以内");

    Console.WriteLine("優先度4 期限未定。でも考えましょう");

    Console.WriteLine("優先度5 なぜ選んだ？言語化しなさい！");
}