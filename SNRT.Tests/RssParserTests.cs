using FluentAssertions;
using CodeHollow.FeedReader;

namespace SNRT.Tests;

public class RssParserTests
{
	[Fact]
	public async Task Parse_Valid_Feed_Should_Return_Items()
	{
		var feed = await FeedReader.ReadAsync("https://www.hespress.com/feed");
		feed.Should().NotBeNull();
		feed.Items.Should().NotBeNull();
		feed.Items.Count.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task Parse_Invalid_Feed_Should_Throw()
	{
		var act = async () => await FeedReader.ReadAsync("https://invalid-host-for-tests.local/feed");
		await act.Should().ThrowAsync<Exception>();
	}
} 