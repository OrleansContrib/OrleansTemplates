using System.Collections.Generic;

namespace ETG.Orleans.Swmr
{
    public interface ITopology
    {
        IEnumerable<string> Nodes { get; }

        string GetNode(string key);

        void Add(string node);

        void Remove(string node);
    }
}