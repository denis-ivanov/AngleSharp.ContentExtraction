using AngleSharp.Html.Dom;
using NUnit.Framework;
using System.Threading.Tasks;

namespace AngleSharp.ContentExtraction.IntegrationTests;

[TestFixture]
public class ContentExtractorTests
{
    [Test]
    public async Task Extract_IntegrationTest()
    {
        // Arrange
        var config = Configuration.Default.WithDefaultLoader();
        const string address = "https://lenta.ru/articles/2020/05/13/coronausa/";
        var context = BrowsingContext.New(config);
        var document = (IHtmlDocument)await context.OpenAsync(address);
        var extractor = new ContentExtractor();

        // Act
        extractor.Extract(document);

        // Assert
        Assert.Pass();
    }
}
