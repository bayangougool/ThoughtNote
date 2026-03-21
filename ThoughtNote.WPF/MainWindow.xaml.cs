using System.Collections.Generic;
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

            _repository = new NodeRepository("Data Source=thoughtnote.db");

            LoadTestData();

            NodeTree.ItemsSource = nodes;
        }

        void LoadTestData()
        {
            var root = new Node
            {
                Content = "🐑 荒ぶる羊の群れ"
            };

            var python = new Node
            {
                Content = "Python"
            };

            python.Children.Add(new Node { Content = "コーパス作成" });
            python.Children.Add(new Node { Content = "モデル学習" });

            root.Children.Add(python);

            root.Children.Add(new Node
            {
                Content = "健康"
            });

            nodes.Add(root);
        }

        private void NodeTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // ① 前のノード保存
            if (currentNode != null)
            {
                currentNode.Body = BodyBox.Text;

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