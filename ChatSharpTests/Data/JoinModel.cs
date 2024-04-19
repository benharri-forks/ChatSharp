using YamlDotNet.Serialization;
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ChatSharp.Tests.Data;

public class JoinModel
{
    public List<Test> Tests { get; set; }

    public class Test
    {
        [YamlMember(Alias = "desc")]
        public string Description { get; set; }

        public Atoms Atoms { get; set; }

        public List<string> Matches { get; set; }
    }

    public class Atoms
    {
        public Dictionary<string, string> Tags { get; set; }
        public string Source { get; set; }
        public string Verb { get; set; }
        public List<string> Params { get; set; }
    }
}