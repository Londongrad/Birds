using System.Globalization;
using System.Windows.Controls;
using Birds.UI.Converters;
using FluentAssertions;

namespace Birds.Tests.UI.Converters;

public class AlternationIndexDisplayConverterTests
{
    [Fact]
    public void Convert_Should_Return_OneBased_Display_Index_From_ItemsControl()
    {
        var sut = new AlternationIndexDisplayConverter();
        var listBox = new ListBox
        {
            ItemsSource = new[] { "A", "B", "C", "D", "E" }
        };

        var result = sut.Convert(["E", listBox], typeof(string), string.Empty, CultureInfo.InvariantCulture);

        result.Should().Be("#5");
    }
}
