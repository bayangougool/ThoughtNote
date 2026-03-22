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

        public MainWindow()
        {
            InitializeComponent();

            _repository = new NodeRepository(@"Data Source=C:\Users\ivonu\ThoughtNote\thoughtnote.db");

            var conn = new SqliteConnection(@"Data Source=C:\Users\ivonu\ThoughtNote\thoughtnote.db");
            conn.Open();

            System.Windows.MessageBox.Show(conn.DataSource);
            var file = conn.DataSource;
            var size = new FileInfo(file).Length;
            System.Windows.MessageBox.Show($"{file}\nサイズ: {size}");

            nodes = _repository.GetTree();
            MessageBox.Show(nodes.Count.ToString());
            NodeTree.ItemsSource = nodes;
        }

        private void NodeTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // ① 前のノード保存
            if (currentNode != null)
            {
                currentNode.Content = BodyBox.Text;

                SaveCurrentNode();
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
        }
    }
}