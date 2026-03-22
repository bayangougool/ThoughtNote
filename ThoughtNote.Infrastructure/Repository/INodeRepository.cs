using System;
using System.Collections.Generic;
using System.Text;
using ThoughtNote.Core;

namespace ThoughtNote.Infrastructure.Repository
{
    public interface INodeRepository
    {
        Node GetNode(string id);

        List<Node> GetChildren(string parentId);

        List<Node> GetTree(string rootId);

        void Create(Node node);

        void Update(Node node);

        void Delete(string id);

        void Move(string nodeId, string newParentId);

        bool Exists(string id);

        public List<Node> GetTree();
    }
}

