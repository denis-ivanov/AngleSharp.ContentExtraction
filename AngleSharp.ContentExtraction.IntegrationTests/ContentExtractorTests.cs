using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using NUnit.Framework;

namespace AngleSharp.ContentExtraction.IntegrationTests
{
    [TestFixture]
    public class ContentExtractorTests
    {
        [Test]
        public async Task Extract_IntegrationTest()
        {
            // Arrange
            var config = Configuration.Default.WithDefaultLoader();
            var address = "https://lenta.ru/articles/2020/05/13/coronausa/";
            var context = BrowsingContext.New(config);
            var document = (IHtmlDocument)await context.OpenAsync(address);
            var extractor = new ContentExtractor();

            // Act
            extractor.Extract(document);

            // Assert
            Assert.Pass();
        }
    }
}
