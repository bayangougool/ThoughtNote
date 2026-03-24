using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.IO;
using System.Windows;
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

        public MainWindow()
        {
            InitializeComponent();

            _repository = new NodeRepository(@"Data Source=C:\Users\ivonu\ThoughtNote\thoughtnote.db");

            nodes = _repository.GetTree();
            NodeTree.ItemsSource = nodes;
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

                //SaveCurrentNode(); ノード移動ではメモリ上に保存のみとする。
            }

            // ② 新しいノード
            if (NodeTree.SelectedItem is Node node)
            {
                currentNode = node;
                BodyBox.Text = node.Content;
            }
        }

        //Enter検知
        private void NodeTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CreateNewNode();
                e.Handled = true; // ★重要（変な挙動防止）
            }
        }

        //ノード生成（コア）
        void CreateNewNode()
        {
            if (NodeTree.SelectedItem is not Node selected)
                return;

            var newNode = new Node
            {
                Id = Guid.NewGuid().ToString(),
                ParentId = selected.ParentId, // ★同階層に作る
                Content = "",
                OrderIndex = selected.OrderIndex + 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _repository.Insert(newNode);

            // ★UIにも追加（ここ重要）
            var parent = FindParentNode(selected);

            if (parent == null)
            {
                nodes.Add(newNode); // ルート
            }
            else
            {
                parent.Children.Add(newNode);
            }
        }

        //親ノード取得
        Node? FindParentNode(Node child)
        {
            return FindParentRecursive(nodes, child);
        }

        Node? FindParentRecursive(List<Node> list, Node target)
        {
            foreach (var node in list)
            {
                if (node.Children.Contains(target))
                    return node;

                var result = FindParentRecursive(node.Children, target);
                if (result != null)
                    return result;
            }

            return null;
        }

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

            _repository.Update(node); // DB保存
        }
    }
}