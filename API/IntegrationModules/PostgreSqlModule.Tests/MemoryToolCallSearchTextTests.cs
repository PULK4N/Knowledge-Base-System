using Xunit;

namespace PostgreSqlModule.Tests;

public sealed class MemoryToolCallSearchTextTests
{
    [Theory]
    [InlineData(
        """{"tool_input":{"command":"git status","timeout":5},"tool_response":{"stdout":"fatal","interrupted":false}}""",
        "git status\n5",
        "fatal\nfalse"
    )]
    [InlineData(
        """{"tool_input":{"file_path":"/src/api/Program.cs"}}""",
        "/src/api/Program.cs\n src api Program.cs",
        ""
    )]
    [InlineData(
        """{"tool_response":[{"type":"text","text":"done"}]}""",
        "",
        "text\ndone"
    )]
    [InlineData("{}", "", "")]
    public void From_keeps_values_of_input_and_output_separately(
        string payloadJson,
        string expectedInput,
        string expectedOutput
    )
    {
        var (input, output) = MemoryToolCallSearchText.From(payloadJson);

        Assert.Equal(expectedInput, input);
        Assert.Equal(expectedOutput, output);
    }

    [Fact]
    public void From_caps_the_text_below_the_tsvector_size_limit()
    {
        var stdout = new string('a', MemoryToolCallSearchText.MaximumLength + 10);
        var payloadJson =
            $$$"""{"tool_response":{"stdout":"{{{stdout}}}"}}""";

        var (_, output) = MemoryToolCallSearchText.From(payloadJson);

        Assert.Equal(MemoryToolCallSearchText.MaximumLength, output.Length);
    }
}
