using Fluffy_Relations;
using Xunit;

namespace RelationsTab.Tests;

public class OpinionThresholdTests
{
    const float Neg = -50f;
    const float Pos = 50f;

    [Theory]
    [InlineData(51f, 0f, true)]
    [InlineData(50f, 0f, false)]
    [InlineData(0f, -51f, true)]
    [InlineData(0f, -50f, false)]
    [InlineData(49f, -49f, false)]
    [InlineData(-80f, 80f, true)]
    public void A_pair_is_a_strong_line_only_beyond_the_default_thresholds(float a, float b, bool expected) {
        Assert.Equal(expected, OpinionLines.IsStrong(a, b, Neg, Pos));
    }

    [Fact]
    public void Either_direction_alone_is_enough() {
        Assert.True(OpinionLines.IsStrong(-60f, 10f, Neg, Pos));
        Assert.True(OpinionLines.IsStrong(10f, -60f, Neg, Pos));
        Assert.True(OpinionLines.IsStrong(60f, 10f, Neg, Pos));
        Assert.True(OpinionLines.IsStrong(10f, 60f, Neg, Pos));
    }

    [Fact]
    public void Raising_the_dislike_threshold_brings_in_a_milder_dislike() {
        Assert.False(OpinionLines.IsStrong(-30f, 0f, Neg, Pos));
        Assert.True(OpinionLines.IsStrong(-30f, 0f, -20f, Pos));
    }

    [Fact]
    public void Lowering_the_like_threshold_brings_in_a_milder_like() {
        Assert.False(OpinionLines.IsStrong(30f, 0f, Neg, Pos));
        Assert.True(OpinionLines.IsStrong(0f, 30f, Neg, 20f));
    }

    [Fact]
    public void The_thresholds_are_read_per_call_not_remembered() {
        Assert.True(OpinionLines.IsStrong(-60f, 0f, Neg, Pos));
        Assert.False(OpinionLines.IsStrong(-60f, 0f, -90f, Pos));
    }

    [Theory]
    [InlineData(5.1f, 0f, true)]
    [InlineData(5f, 0f, false)]
    [InlineData(0f, -5.1f, true)]
    [InlineData(0f, -5f, false)]
    public void A_selected_colonist_also_shows_mild_opinions_beyond_5(float a, float b, bool expected) {
        Assert.Equal(expected, OpinionLines.IsShown(a, b, true, Neg, Pos));
    }

    [Fact]
    public void An_unselected_pair_with_mild_opinions_shows_no_line() {
        Assert.False(OpinionLines.IsShown(20f, -20f, false, Neg, Pos));
        Assert.True(OpinionLines.IsShown(20f, -20f, true, Neg, Pos));
    }
}
