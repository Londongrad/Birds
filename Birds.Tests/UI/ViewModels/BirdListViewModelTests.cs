using Birds.Application.DTOs;
using Birds.Domain.Enums;
using Birds.Domain.Extensions;
using Birds.Tests.UI.Services;
using Birds.UI.Enums;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.ViewModels;
using FluentAssertions;
using Moq;

namespace Birds.Tests.UI.ViewModels
{
    public class BirdListViewModelTests
    {
        [Fact]
        public void Filters_Should_Contain_Default_Options_And_All_Bird_Species()
        {
            var sut = CreateViewModel();

            sut.Filters.Should().HaveCount(4 + Enum.GetValues<BirdsName>().Length);
            sut.Filters.Should().ContainSingle(x => x.Filter == BirdFilter.All);
            sut.Filters.Should().ContainSingle(x => x.Filter == BirdFilter.Alive);
            sut.Filters.Should().ContainSingle(x => x.Filter == BirdFilter.Dead);
            sut.Filters.Should().ContainSingle(x => x.Filter == BirdFilter.DepartedButAlive);
            sut.Filters.Count(x => x.Filter == BirdFilter.BySpecies).Should().Be(Enum.GetValues<BirdsName>().Length);
        }

        [Fact]
        public void FilterBirds_Should_Filter_By_Selected_Species_Without_String_Switches()
        {
            var sparrow = CreateBird((BirdsName)1);
            var chickadee = CreateBird((BirdsName)6);
            var sut = CreateViewModel(sparrow, chickadee);

            sut.SelectedFilter = sut.Filters.Single(x => x.Filter == BirdFilter.BySpecies && x.Species == (BirdsName)6);

            sut.FilterBirds(sparrow).Should().BeFalse();
            sut.FilterBirds(chickadee).Should().BeTrue();
        }

        [Fact]
        public void FilterBirds_Should_Exclude_Invalid_Bird_Name_When_Filtering_By_Species()
        {
            var invalidBird = TestHelpers.Bird(name: "Unknown bird");
            var sut = CreateViewModel(invalidBird);

            sut.SelectedFilter = sut.Filters.Single(x => x.Filter == BirdFilter.BySpecies && x.Species == (BirdsName)1);

            sut.FilterBirds(invalidBird).Should().BeFalse();
        }

        [Fact]
        public void FilterBirds_Should_Combine_Search_And_Species_Filter()
        {
            var sparrow = CreateBird((BirdsName)1, desc: "forest visitor");
            var secondSparrow = CreateBird((BirdsName)1, desc: "city bird");
            var sut = CreateViewModel(sparrow, secondSparrow);
            sut.SelectedFilter = sut.Filters.Single(x => x.Filter == BirdFilter.BySpecies && x.Species == (BirdsName)1);
            sut.SearchText = "forest";

            sut.FilterBirds(sparrow).Should().BeTrue();
            sut.FilterBirds(secondSparrow).Should().BeFalse();
        }

        private static BirdListViewModel CreateViewModel(params BirdDTO[] birds)
        {
            var store = new BirdStore();
            store.CompleteLoading();

            foreach (var bird in birds)
                store.Birds.Add(bird);

            var manager = new Mock<IBirdManager>();
            manager.SetupGet(x => x.Store).Returns(store);
            var localization = new Mock<ILocalizationService>();

            return new BirdListViewModel(manager.Object, localization.Object);
        }

        private static BirdDTO CreateBird(BirdsName species, string? desc = null) =>
            TestHelpers.Bird(name: species.ToDisplayName(), desc: desc);
    }
}
