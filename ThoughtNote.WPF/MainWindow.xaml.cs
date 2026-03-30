using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ThoughtNote.Core;
using ThoughtNote.Infrastructure.Repository;

namespace ThoughtNote.WPF
{
    public partial class MainWindow : Window
    {
        private List<Node> nodes = new();

        private Node? currentNode;
        private INodeRepository _repository;
        bool _isReloading = false; //無限ループ対策

        public Node? SelectedNode { get; set; } //選択用プロパティ。新規ノード作成後に利用

        //仮説チップ（UI上の定型文挿入用）
        Dictionary<string, string> chips = new()
{
    { "仮説", "仮説：\n・事実\n・仮説\n・反証\n" }
};

        public MainWindow()
        {
            InitializeComponent();

            _repository = new NodeRepository(@"Data Source=C:\Users\ivonu\ThoughtNote\thoughtnote.db");

            nodes = _repository.GetTree();
            NodeTree.ItemsSource = nodes;
        }

        //Ctrl+Tショートカットでチップス展開
        private void BodyBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.T)
            {
                ExpandChipAtCursor();
                e.Handled = true;
            }
        }

        //カーソル位置の#キーワードをtipsから展開
        void ExpandChipAtCursor()
        {
            var text = BodyBox.Text;

            var pattern = @"#(.*?)#";
            var match = Regex.Match(text, pattern);

            if (match.Success)
            {
                var key = match.Groups[1].Value;

                if (chips.ContainsKey(key))
                {
                    var expanded = chips[key];

                    BodyBox.Text = text.Replace(match.Value, expanded);
                    BodyBox.CaretIndex = BodyBox.Text.Length;
                }
            }
        }

        private void NodeTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_isReloading)
                return;

            if (e.NewValue is not Node nodetype)
                return;

            // ① 前のノード保存
            if (currentNode != null)
            {
                currentNode.Content = BodyBox.Text;
            }

            // ② 新しいノード
            if (NodeTree.SelectedItem is Node node)
            {
                SelectedNode = node;
                currentNode = node;
                BodyBox.Text = node.Content;
            }
        }

        private void NodeTree_KeyDown(object sender, KeyEventArgs e)
        {
            //Enter検知
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                CreateChildNode();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            { //Ctrl+Enterで同階層にノード作成
                CreateSiblingNode();
                e.Handled = true;
            }
        }

        // 子ノード生成
        void CreateChildNode()
        {
            AddNode(currentNode);
        }

        // 同階層にノード作成
        void CreateSiblingNode()
        {
            var parent = FindParent(nodes, currentNode);

            AddNode(parent);
        }

        // AddNodeは共通化。親ノードを受け取って子ノードを作成する。
        void AddNode(Node parent)
        {
            var newNode = new Node
            {
                Id = Guid.NewGuid().ToString(),
                ParentId = parent.Id,
                Content = "",
                OrderIndex = parent.OrderIndex + 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsPersisted = false
                //IsPersistedはfalseのまま（DBに保存されていない状態）
            };

            parent.Children.Add(newNode);

            NodeTree.UpdateLayout();

            var item = GetTreeViewItem(NodeTree, newNode);
            if (item != null)
            {
                item.IsSelected = true;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                BodyBox.Focus();
            }));
        }

        // 親ノードを再帰的に探す
        Node? FindParent(IEnumerable<Node> nodes, Node target)
        {
            foreach (var node in nodes)
            {
                if (node.Children.Contains(target))
                    return node;

                var parent = FindParent(node.Children, target);
                if (parent != null)
                    return parent;
            }

            return null;
        }

        //ノード生成（コア）
        //void CreateNewNode()
        //{
        //    if (NodeTree.SelectedItem is not Node selected)
        //        return;

        //    var newNode = new Node
        //    {
        //        Id = Guid.NewGuid().ToString(),
        //        ParentId = selected.ParentId, // ★同階層に作る
        //        Content = "",
        //        OrderIndex = selected.OrderIndex + 1,
        //        CreatedAt = DateTime.Now,
        //        UpdatedAt = DateTime.Now,
        //        IsPersisted = false
        //        //IsPersistedはfalseのまま（DBに保存されていない状態）
        //    };

        //    // ★UIにも追加（ここ重要）
        //    var parent = FindParentNode(selected);

        //    if (parent == null)
        //    {
        //        nodes.Add(newNode); // ルート
        //    }
        //    else
        //    {
        //        parent.Children.Add(newNode);
        //        // ノードを新規ノードに移動
        //        SelectedNode = newNode;

        //        NodeTree.UpdateLayout();

        //        var item = GetTreeViewItem(NodeTree, newNode);
        //        if (item != null)
        //        {
        //            item.IsSelected = true;
        //        }

        //        Dispatcher.BeginInvoke(new Action(() =>
        //        {
        //            BodyBox.Focus();
        //            BodyBox.CaretIndex = BodyBox.Text.Length;
        //        }));
        //        //NodeTree.UpdateLayout();//新規作成したノードを選択状態にするためのレイアウト更新
        //    }
        //}

        //選択ノード取得
        private TreeViewItem? GetTreeViewItem(ItemsControl parent, object item)
        {
            if (parent == null) return null;

            var container = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            if (container != null)
                return container;

            foreach (var child in parent.Items)
            {
                var parentContainer = parent.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;
                if (parentContainer != null)
                {
                    var result = GetTreeViewItem(parentContainer, item);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        //親ノード取得
        //Node? FindParentNode(Node child)
        //{
        //    return FindParentRecursive(nodes, child);
        //}

        //Node? FindParentRecursive(List<Node> list, Node target)
        //{
        //    foreach (var node in list)
        //    {
        //        if (node.Children.Contains(target))
        //            return node;

        //        var result = FindParentRecursive(node.Children.ToList(), target);
        //        if (result != null)
        //            return result;
        //    }

        //    return null;
        //}

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                SaveCurrentNode();
                e.Handled = true;
            }
        }


        void SaveCurrentNode()
        {
            if (NodeTree.SelectedItem is not Node node)
                return;

            node.Content = BodyBox.Text; // ★これだけでUI更新される

            if (currentNode != null && !currentNode.IsPersisted)
            {
                _repository.Insert(currentNode); // DBインサート
                node.IsPersisted = true; //インサート後は既存ノードに仲間入り
            }
            else
            {
                _repository.Update(node); // DB更新
            }
        }
    }
}