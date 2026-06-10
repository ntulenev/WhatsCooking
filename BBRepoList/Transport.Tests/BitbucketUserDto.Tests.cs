using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Transport.Tests;

public sealed class BitbucketUserDtoTests
{
    [Fact(DisplayName = "Bitbucket user DTO maps JSON properties")]
    [Trait("Category", "Unit")]
    public void BitbucketUserDtoWhenDeserializedMapsProperties()
    {
        var result = JsonSerializer.Deserialize<BitbucketUserDto>(
            """{"uuid":"user-id","display_name":"User Name"}""");

        result.Should().Be(new BitbucketUserDto("user-id", "User Name"));
    }
}
