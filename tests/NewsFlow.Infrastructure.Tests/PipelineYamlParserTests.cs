using NewsFlow.Infrastructure.Yaml.Parsing;
using NewsFlow.Infrastructure.Yaml.Validation;

namespace NewsFlow.Infrastructure.Tests;

public class PipelineYamlParserTests
{
    private const string ValidPipelineYaml = @"
pipeline:
  code: test_pipeline
  name: Test Pipeline
  entity: Document
  version: 1
  statuses:
    - code: Draft
      name: Draft
      type: initial
      color: '#2196F3'
    - code: InReview
      name: In Review
      type: intermediate
      is_locked: true
    - code: Approved
      name: Approved
      type: terminal
  transitions:
    - from: Draft
      to: InReview
      action: take
      label: Take for review
      required_role: Reviewer
      lock_to_user: true
    - from: InReview
      to: Approved
      action: approve
      label: Approve
      required_role: Reviewer
      only_assigned_user: true
      unlock: true
";

    [Fact]
    public void ParsePipeline_ValidYaml_ReturnsPipeline()
    {
        var pipeline = YamlParser.ParsePipeline(ValidPipelineYaml);

        Assert.Equal("test_pipeline", pipeline.Code);
        Assert.Equal("Test Pipeline", pipeline.Name);
        Assert.Equal("Document", pipeline.Entity);
        Assert.Equal(1, pipeline.Version);
    }

    [Fact]
    public void ParsePipeline_HasCorrectStatuses()
    {
        var pipeline = YamlParser.ParsePipeline(ValidPipelineYaml);

        Assert.Equal(3, pipeline.Statuses.Count);
        Assert.Equal("Draft", pipeline.Statuses[0].Code);
        Assert.Equal("initial", pipeline.Statuses[0].Type);
        Assert.True(pipeline.Statuses[1].IsLocked);
    }

    [Fact]
    public void ParsePipeline_HasCorrectTransitions()
    {
        var pipeline = YamlParser.ParsePipeline(ValidPipelineYaml);

        Assert.Equal(2, pipeline.Transitions.Count);
        Assert.Equal("Draft", pipeline.Transitions[0].GetFromStatuses()[0]);
        Assert.Equal("InReview", pipeline.Transitions[0].To);
        Assert.Equal("Reviewer", pipeline.Transitions[0].RequiredRole);
        Assert.True(pipeline.Transitions[0].LockToUser);
    }

    [Fact]
    public void ValidatePipeline_ValidPipeline_IsValid()
    {
        var pipeline = YamlParser.ParsePipeline(ValidPipelineYaml);
        var result = PipelineValidator.Validate(pipeline);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidatePipeline_NoStatuses_HasError()
    {
        var pipeline = YamlParser.ParsePipeline(@"
pipeline:
  code: empty
  entity: Document
  statuses: []
");
        var result = PipelineValidator.Validate(pipeline);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("at least one status"));
    }

    [Fact]
    public void ValidatePipeline_NoInitialStatus_HasError()
    {
        var pipeline = YamlParser.ParsePipeline(@"
pipeline:
  code: no_initial
  entity: Document
  statuses:
    - code: Done
      name: Done
      type: terminal
");
        var result = PipelineValidator.Validate(pipeline);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("initial status"));
    }

    [Fact]
    public void ValidatePipeline_TransitionToUnknownStatus_HasError()
    {
        var pipeline = YamlParser.ParsePipeline(@"
pipeline:
  code: bad_transition
  entity: Document
  statuses:
    - code: Draft
      name: Draft
      type: initial
  transitions:
    - from: Draft
      to: NonExistent
      action: go
      label: Go
");
        var result = PipelineValidator.Validate(pipeline);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("NonExistent"));
    }

    [Fact]
    public void ParseWorkspace_ValidYaml_ReturnsWorkspace()
    {
        var yaml = @"
workspace:
  code: translator
  name: Translator Workspace
  required_role: Translator
  input_queue:
    entity: Material
    filter:
      status: New
  actions:
    take:
      label: Take
      log_message: Taken
    complete:
      label: Complete
      requires_fields:
        - translated_text
";
        var ws = YamlParser.ParseWorkspace(yaml);

        Assert.Equal("translator", ws.Code);
        Assert.Equal("Translator", ws.RequiredRole);
        Assert.Equal("Material", ws.InputQueue.Entity);
        Assert.Equal(2, ws.Actions.Count);
        Assert.Contains("take", ws.Actions.Keys);
        Assert.Single(ws.Actions["complete"].RequiresFields!);
    }

    [Fact]
    public void ValidateWorkspace_ValidWorkspace_IsValid()
    {
        var yaml = @"
workspace:
  code: test
  name: Test
  required_role: Admin
  input_queue:
    entity: Document
  actions:
    take:
      label: Take
";
        var ws = YamlParser.ParseWorkspace(yaml);
        var result = PipelineValidator.ValidateWorkspace(ws);

        Assert.True(result.IsValid);
    }
}
