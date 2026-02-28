using System;
using ThoughtNote.Core;

var service = new ThoughtService();

Console.WriteLine("ThoughtNote CLI");

while (true)
{
    Console.Write("> ");

    var input = Console.ReadLine();

    if (input == "exit")
        break;

    if (input == "tree")
    {
        service.ShowTree();
        continue;
    }

    if (input == "topic")
    {
        Console.Write("タイトル:");
        var t = Console.ReadLine();

        Console.Write("idea or task:");
        var c = Console.ReadLine();

        Console.Write("priority:");
        int p =
        int.Parse(Console.ReadLine());

        service.CreateTopic(t, c, p);
    }

    if (input == "tag")
    {
        Console.Write("nodeId:");
        int id =
        int.Parse(Console.ReadLine());

        Console.Write("tag:");
        var tag =
        Console.ReadLine();

        service.AddTag(id, tag);
    }

    if (input == "comment")
    {
        Console.Write("tag:");
        var tag =
        Console.ReadLine();

        Console.Write("comment:");
        var com =
        Console.ReadLine();

        service.AddComment(tag, com);
    }
}