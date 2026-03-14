using System.Windows;
using ThoughtNote.Core;

namespace ThoughtNote.WPF
{
    public partial class MainWindow : Window
    {
        List<Node> nodes = new();

        public MainWindow()
        {
            InitializeComponent();

            LoadTestData();

            NodeTree.ItemsSource = nodes;
        }

        void LoadTestData()
        {
            var root = new Node
            {
                Title = "🐑 荒ぶる羊の群れ"
            };

            var python = new Node
            {
                Title = "Python"
            };

            python.Children.Add(new Node { Title = "コーパス作成" });
            python.Children.Add(new Node { Title = "モデル学習" });

            root.Children.Add(python);

            root.Children.Add(new Node
            {
                Title = "健康"
            });

            nodes.Add(root);
        }

        private void NodeTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (NodeTree.SelectedItem is Node node)
            {
                TitleBox.Text = node.Title;
                BodyBox.Text = node.Body;
            }
        }
    }
}
