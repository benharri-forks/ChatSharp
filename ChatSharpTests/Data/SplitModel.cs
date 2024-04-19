// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ChatSharp.Tests.Data;

public class SplitModel
{
    public List<Test> Tests { get; set; }

    public class Test
    {
        public string Input { get; set; }
        public JoinModel.Atoms Atoms { get; set; }
    }
}