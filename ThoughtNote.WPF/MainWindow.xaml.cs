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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                SaveCurrentNode();
                e.Handled = true;
            }
        }

        void SaveNode(Node node)
        {
            _repository.Update(node);
        }

        void SaveCurrentNode()
        {
            if (currentNode == null) return;

            currentNode.Content = BodyBox.Text;

            _repository.Update(currentNode);

            // ★これ追加
           //ReloadTree();
        }

        void ReloadTree()
        {
            _isReloading = true;
            nodes = _repository.GetTree();
            NodeTree.ItemsSource = nodes;
            _isReloading = false;
        }
    }
}