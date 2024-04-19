using System.Globalization;
using System.IO;
using ChatSharp.Tests.Data;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ChatSharp.Tests;

[TestClass]
public class ParsingTests
{
    private static T LoadYaml<T>(string path)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<T>(File.ReadAllText(path));
    }

    [TestMethod]
    public void Split()
    {
        foreach (var test in LoadYaml<SplitModel>("Data/msg-split.yaml").Tests)
        {
            var message = new IrcMessage(test.Input);
            var atoms = test.Atoms;

            Assert.AreEqual(atoms.Verb.ToUpper(CultureInfo.InvariantCulture), message.Command,
                $"command failed on: '{test.Input}'");
            Assert.AreEqual(atoms.Source, message.Source,
                $"source failed on: '{test.Input}' ");
            CollectionAssert.AreEqual(atoms.Tags, message.Tags,
                $"tags failed on: '{test.Input}' ");
            CollectionAssert.AreEqual(atoms.Params ?? [], message.Parameters,
                $"params failed on: '{test.Input}' ");
        }
    }

    [TestMethod]
    public void Join()
    {
        foreach (var test in LoadYaml<JoinModel>("Data/msg-join.yaml").Tests)
        {
            var atoms = test.Atoms;
            var line = new IrcMessage
            {
                Command = atoms.Verb,
                Parameters = atoms.Params,
                Source = atoms.Source,
                Tags = atoms.Tags
            }.Format();

            Assert.IsTrue(test.Matches.Contains(line), test.Description);
        }
    }
}