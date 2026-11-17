using Fluffy_Relations.ForceDirectedGraph;
using Xunit;

namespace RelationsTab.Tests;

public class LayoutMathTests
{
    const float Slot = 70f;

    [Fact]
    public void ClampCenter_keeps_a_value_that_already_fits() {
        Assert.Equal(200f, LayoutMath.ClampCenter(200f, 500f, Slot));
    }

    [Theory]
    [InlineData(-1000f, 35f)]
    [InlineData(0f, 35f)]
    [InlineData(34f, 35f)]
    [InlineData(466f, 465f)]
    [InlineData(500f, 465f)]
    [InlineData(float.MaxValue, 465f)]
    [InlineData(float.MinValue, 35f)]
    public void ClampCenter_keeps_the_whole_slot_inside_the_area(float value, float expected) {
        Assert.Equal(expected, LayoutMath.ClampCenter(value, 500f, Slot));
    }

    [Fact]
    public void ClampCenter_of_an_area_exactly_one_slot_wide_is_its_middle() {
        Assert.Equal(35f, LayoutMath.ClampCenter(900f, 70f, Slot));
        Assert.Equal(35f, LayoutMath.ClampCenter(-900f, 70f, Slot));
    }

    [Fact]
    public void ClampCenter_throws_when_the_area_is_smaller_than_a_slot() {
        Assert.Throws<ArgumentOutOfRangeException>(() => LayoutMath.ClampCenter(10f, 69f, Slot));
    }

    [Fact]
    public void ClampCenter_throws_on_not_a_number() {
        Assert.Throws<ArgumentOutOfRangeException>(() => LayoutMath.ClampCenter(float.NaN, 500f, Slot));
    }

    [Fact]
    public void CapStep_leaves_a_short_step_alone() {
        Assert.Equal((3f, 4f), LayoutMath.CapStep(3f, 4f, 10f));
    }

    [Fact]
    public void CapStep_shortens_a_long_step_to_the_cap_and_keeps_its_direction() {
        var (x, y) = LayoutMath.CapStep(300f, 400f, 10f);
        Assert.Equal(6f, x, 3);
        Assert.Equal(8f, y, 3);
    }

    [Fact]
    public void CapStep_handles_a_step_that_points_the_other_way() {
        var (x, y) = LayoutMath.CapStep(-3000f, 0f, 35f);
        Assert.Equal(-35f, x, 3);
        Assert.Equal(0f, y, 3);
    }

    [Fact]
    public void CapStep_with_a_zero_cap_stops_the_node() {
        Assert.Equal((0f, 0f), LayoutMath.CapStep(5f, 5f, 0f));
    }

    [Fact]
    public void CapStep_throws_on_a_negative_cap() {
        Assert.Throws<ArgumentOutOfRangeException>(() => LayoutMath.CapStep(1f, 1f, -1f));
    }

    [Theory]
    [InlineData(float.NaN, 1f)]
    [InlineData(1f, float.PositiveInfinity)]
    public void CapStep_throws_on_a_step_that_is_not_finite(float x, float y) {
        Assert.Throws<ArgumentOutOfRangeException>(() => LayoutMath.CapStep(x, y, 10f));
    }

    [Fact]
    public void StepCap_is_half_a_slot_at_the_starting_temperature() {
        Assert.Equal(35f, LayoutMath.StepCap(0.05f, 0.05f, Slot), 3);
    }

    [Fact]
    public void StepCap_falls_in_step_with_the_temperature() {
        Assert.Equal(17.5f, LayoutMath.StepCap(0.025f, 0.05f, Slot), 3);
        Assert.Equal(3.5f, LayoutMath.StepCap(0.005f, 0.05f, Slot), 3);
    }

    [Fact]
    public void StepCap_is_zero_when_the_temperature_has_run_out() {
        Assert.Equal(0f, LayoutMath.StepCap(0f, 0.05f, Slot));
    }

    [Fact]
    public void StepCap_follows_a_player_set_maximum_temperature() {
        Assert.Equal(35f, LayoutMath.StepCap(0.5f, 0.5f, Slot), 3);
        Assert.Equal(17.5f, LayoutMath.StepCap(0.25f, 0.5f, Slot), 3);
    }

    [Fact]
    public void StepCap_throws_when_the_maximum_temperature_is_not_positive() {
        Assert.Throws<ArgumentOutOfRangeException>(() => LayoutMath.StepCap(0f, 0f, Slot));
    }

    [Fact]
    public void StepCap_throws_on_a_temperature_above_the_maximum() {
        Assert.Throws<ArgumentOutOfRangeException>(() => LayoutMath.StepCap(0.06f, 0.05f, Slot));
    }

    [Fact]
    public void An_exploding_velocity_moves_a_node_at_most_the_cap_and_ends_inside_the_area() {
        var cap = LayoutMath.StepCap(0.05f, 0.05f, Slot);
        var (vx, vy) = LayoutMath.CapStep(5e7f, -5e7f, cap);
        var x = LayoutMath.ClampCenter(250f + vx, 500f, Slot);
        var y = LayoutMath.ClampCenter(250f + vy, 500f, Slot);
        Assert.InRange(MathF.Sqrt(((x - 250f) * (x - 250f)) + ((y - 250f) * (y - 250f))), 0f, cap + 0.001f);
        Assert.InRange(x, 35f, 465f);
        Assert.InRange(y, 35f, 465f);
    }

    [Fact]
    public void CoincidentDirection_is_a_unit_vector_that_points_opposite_ways_for_the_two_nodes() {
        var (ax, ay) = LayoutMath.CoincidentDirection(2, 5);
        var (bx, by) = LayoutMath.CoincidentDirection(5, 2);
        Assert.Equal((1f, 0f), (ax, ay));
        Assert.Equal((-1f, 0f), (bx, by));
    }

    [Fact]
    public void CoincidentDirection_throws_for_a_node_with_itself() {
        Assert.Throws<ArgumentException>(() => LayoutMath.CoincidentDirection(3, 3));
    }

    [Theory]
    [InlineData(1000f, 1000f, 33, 70f)]
    [InlineData(1000f, 1000f, 6, 166.667f)]
    [InlineData(1000f, 1000f, 3, 333.333f)]
    [InlineData(1000f, 1000f, 2, 350f)]
    [InlineData(1000f, 1000f, 1000, 70f)]
    [InlineData(500f, 800f, 4, 158.114f)]
    public void IdealDistance_is_the_square_root_of_the_area_over_the_count_within_one_to_five_slots(float width, float height, int count, float expected) {
        Assert.Equal(expected, LayoutMath.IdealDistance(width, height, count, Slot), 0.01f);
    }

    [Fact]
    public void IdealDistance_throws_without_nodes() {
        Assert.Throws<ArgumentOutOfRangeException>(() => LayoutMath.IdealDistance(1000f, 1000f, 0, Slot));
    }

    [Theory]
    [InlineData(1, 1f)]
    [InlineData(6, 1f)]
    [InlineData(14, 1f)]
    [InlineData(28, 2f)]
    [InlineData(33, 2.357143f)]
    [InlineData(70, 5f)]
    public void Density_is_one_up_to_the_reference_count_and_grows_with_the_count_beyond_it(int count, float expected) {
        Assert.Equal(expected, LayoutMath.Density(count), 0.0001f);
    }

    [Fact]
    public void Density_throws_without_nodes() {
        Assert.Throws<ArgumentOutOfRangeException>(() => LayoutMath.Density(0));
    }

    [Theory]
    [InlineData(1f, 1f, 1f)]
    [InlineData(2f, 8f, 0.25f)]
    [InlineData(4f, 64f, 0.0625f)]
    public void Scales_are_one_at_the_reference_density_and_repulsion_outgrows_attraction_above_it(float density, float repulsion, float attraction) {
        Assert.Equal(repulsion, LayoutMath.RepulsionScale(density), 0.001f);
        Assert.Equal(attraction, LayoutMath.AttractionScale(density), 0.0001f);
    }

    [Theory]
    [InlineData(0.99f)]
    [InlineData(float.NaN)]
    public void Scales_throw_below_the_reference_density(float density) {
        Assert.Throws<ArgumentOutOfRangeException>(() => LayoutMath.RepulsionScale(density));
        Assert.Throws<ArgumentOutOfRangeException>(() => LayoutMath.AttractionScale(density));
    }
}
