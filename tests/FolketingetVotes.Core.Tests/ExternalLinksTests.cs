using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Tests;

public class ExternalLinksTests
{
    [Fact]
    public void Bill_links_point_at_folketingstidende()
    {
        Assert.Equal(
            "https://www.folketingstidende.dk/samling/20231/lovforslag/L41/index.htm",
            ExternalLinks.FolketingstidendeCaseUrl("20231", CaseType.Bill, "L 41"));
        Assert.Equal(
            "https://www.folketingstidende.dk/ripdf/samling/20231/lovforslag/l41/20231_l41_som_fremsat.pdf",
            ExternalLinks.BillAsIntroducedPdfUrl("20231", CaseType.Bill, "L 41"));
    }

    [Fact]
    public void Resolutions_use_their_own_segment()
    {
        Assert.Equal(
            "https://www.folketingstidende.dk/samling/20241/beslutningsforslag/B12/index.htm",
            ExternalLinks.FolketingstidendeCaseUrl("20241", CaseType.Resolution, "B 12"));
    }

    [Theory]
    [InlineData(CaseType.Interpellation, "F 3")]
    [InlineData(CaseType.Bill, null)]
    [InlineData(CaseType.Bill, "  ")]
    public void Other_case_types_or_missing_numbers_have_no_link(CaseType type, string? number)
    {
        Assert.Null(ExternalLinks.FolketingstidendeCaseUrl("20231", type, number));
        Assert.Null(ExternalLinks.BillAsIntroducedPdfUrl("20231", type, number));
    }

    [Fact]
    public void PagedResult_computes_pages()
    {
        var result = new PagedResult<int>([1, 2, 3], Page: 2, PageSize: 3, TotalCount: 7);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPrevious);
        Assert.True(result.HasNext);
    }
}
