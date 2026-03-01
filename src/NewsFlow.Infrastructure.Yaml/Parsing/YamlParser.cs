using NewsFlow.Infrastructure.Yaml.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NewsFlow.Infrastructure.Yaml.Parsing;

public static class YamlParser
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public static PipelineDefinition ParsePipeline(string yaml)
    {
        var root = Deserializer.Deserialize<PipelineYamlRoot>(yaml);
        return root.Pipeline;
    }

    public static WorkspaceDefinition ParseWorkspace(string yaml)
    {
        var root = Deserializer.Deserialize<WorkspaceYamlRoot>(yaml);
        return root.Workspace;
    }

    public static PipelineDefinition ParsePipelineFromFile(string filePath)
    {
        var yaml = File.ReadAllText(filePath);
        return ParsePipeline(yaml);
    }

    public static WorkspaceDefinition ParseWorkspaceFromFile(string filePath)
    {
        var yaml = File.ReadAllText(filePath);
        return ParseWorkspace(yaml);
    }

    public static string SerializePipeline(PipelineDefinition pipeline)
    {
        var root = new PipelineYamlRoot { Pipeline = pipeline };
        return Serializer.Serialize(root);
    }

    public static string SerializeWorkspace(WorkspaceDefinition workspace)
    {
        var root = new WorkspaceYamlRoot { Workspace = workspace };
        return Serializer.Serialize(root);
    }
}
