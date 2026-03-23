using System.ComponentModel;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ThoughtNote.Core
{


    public class Node : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string? ParentId { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _content = "";

        public string Content
        {
            get => _content;
            set
            {
                if (_content == value) return;

                _content = value;

                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(Title)); // ★超重要
            }
        }

        public int OrderIndex { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public List<Node> Children { get; set; } = new();

        public string Title
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Content))
                    return "";

                var firstLine = Content
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0]
                    .Trim();

                return firstLine.Length > 40
                    ? firstLine.Substring(0, 40) + "…"
                    : firstLine;
            }
        }
    }
}