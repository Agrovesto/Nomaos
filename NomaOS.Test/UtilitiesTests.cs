using System.ComponentModel.DataAnnotations;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Utilities;
using FluentAssertions;

namespace NomaOS.Test;

public class UtilitiesTests
{
    [Theory]
    [InlineData("success", "SUCCESS")]
    [InlineData("SUCCESSFUL", "SUCCESS")]
    [InlineData("completed", "SUCCESS")]
    [InlineData("failed", "FAILED")]
    [InlineData("error", "FAILED")]
    [InlineData("pending", "PENDING")]
    [InlineData("unexpected", "FAILED")]
    public void MapStatus_MapsKnownAndUnknownProviderStatuses(string input, string expected)
    {
        ProviderMapStatus.MapStatus(input).Should().Be(expected);
    }

    [Fact]
    public void GetLast_ReturnsWholeValueWhenRequestedLengthIsLonger()
    {
        "1234".GetLast(8).Should().Be("1234");
    }

    [Fact]
    public void GetLast_ReturnsRequestedSuffix()
    {
        "123456".GetLast(3).Should().Be("456");
    }

    [Fact]
    public void ComputeSha256Hash_ReturnsKnownLowerCaseHash()
    {
        HashingHelper.ComputeSha256Hash("hello")
            .Should().Be("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
    }

    [Fact]
    public void HashSecret_IsDeterministicForSameSecretAndSalt()
    {
        var salt = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        var hash = "1234".HashSecret(salt);

        hash.Should().Be("b0ipvBogYNntrazm50l1y8ve4diPzlsskYDvvTPpziI=");
        "1234".EncryptTransactionPin(salt).Should().Be(hash);
    }

    [Fact]
    public void GetSalt_ReturnsNonEmptyRandomSalt()
    {
        EncryptionUtil.GetSalt().Should().HaveCount(256).And.NotBeEquivalentTo(new byte[256]);
    }

    [Theory]
    [InlineData(0, "Zero Naira Only")]
    [InlineData(250000, "Two Hundred and Fifty Thousand Naira Only")]
    [InlineData(12.50, "Twelve Naira, Fifty Kobo Only")]
    public void NumberToWords_ConvertsNairaAndKobo(decimal amount, string expected)
    {
        NumberToWords.Convert(amount).Should().Be(expected);
    }

    [Fact]
    public void NumberToWords_RejectsNegativeAmounts()
    {
        var action = () => NumberToWords.Convert(-0.01m);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GeometryHelpers_CalculateExpectedDistanceAndAreas()
    {
        GeometryHelper.HaversineDistanceMetres(0, 0, 0, 1).Should().BeApproximately(111_195, 100);
        GeometryHelper.CalculatePolygonAreaDegrees([(0, 0), (0, 1), (1, 1), (1, 0)]).Should().Be(1);
        GeometryHelper.CalculatePolygonAreaSqMeters([(0, 0), (0, 1), (1, 1), (1, 0)]).Should().BeGreaterThan(12_000_000_000);
        GeometryHelper.DegreesToRadians(180).Should().BeApproximately(Math.PI, 0.000001);
    }

    [Fact]
    public void IsOverlapping_UsesBoundingBoxesAndHandlesEmptyCoordinates()
    {
        var existing = new List<FarmCoordinate>
        {
            new() { Latitude = 0, Longitude = 0 }, new() { Latitude = 2, Longitude = 2 }
        };
        var overlapping = new List<FarmCoordinate>
        {
            new() { Latitude = 1, Longitude = 1 }, new() { Latitude = 3, Longitude = 3 }
        };
        var separate = new List<FarmCoordinate>
        {
            new() { Latitude = 3, Longitude = 3 }, new() { Latitude = 4, Longitude = 4 }
        };

        GeometryHelper.IsOverlapping(existing, overlapping).Should().BeTrue();
        GeometryHelper.IsOverlapping(existing, separate).Should().BeFalse();
        GeometryHelper.IsOverlapping(existing, []).Should().BeFalse();
    }

    [Fact]
    public void EnumExtensions_UsesDisplayAttributeWhenPresent()
    {
        EnumExtensions.GetDisplayName(DisplayEnum.Custom).Should().Be("Custom label");
        EnumExtensions.GetDisplayName(DisplayEnum.Default).Should().Be(nameof(DisplayEnum.Default));
    }

    private enum DisplayEnum
    {
        [Display(Name = "Custom label")]
        Custom,
        Default
    }
}
