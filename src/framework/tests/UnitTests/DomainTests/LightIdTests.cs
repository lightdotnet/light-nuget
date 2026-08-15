namespace UnitTests.DomainTests
{
    public class LightIdTests
    {
        [Test]
        public void NewId_Generates_Unique_NonEmpty_Ids()
        {
            var id1 = LightId.NewId();
            var id2 = LightId.NewId();

            Assert.That((Guid)id1, Is.Not.EqualTo(Guid.Empty));
            Assert.That((Guid)id1, Is.Not.EqualTo((Guid)id2));
        }

        [Test]
        public void NewId_Generates_Version7_Guid()
        {
            string value = LightId.NewId();

            value[12].ShouldBe('7');
        }

        [Test]
        public void ToString_Matches_Underlying_Guid_ToString_N_Format()
        {
            var id = LightId.NewId();

            id.ToString().ShouldBe(id.Guid.ToString("N"));
        }

        [Test]
        public void ToString_Does_Not_Contain_Hyphens()
        {
            var id = LightId.NewId();

            Assert.That(id.ToString(), Does.Not.Contain("-"));
        }

        [Test]
        public void Implicit_Conversion_To_String_Matches_Guid_ToString_N_Format()
        {
            var id = LightId.NewId();

            string value = id;

            value.ShouldBe(id.Guid.ToString("N"));
        }

        [Test]
        public void Implicit_Conversion_To_Guid_Returns_Underlying_Value()
        {
            var id = LightId.NewId();

            Guid value = id;

            value.ShouldBe(id.Guid);
        }

        [Test]
        public void Constructor_Wraps_Given_Guid()
        {
            var guid = Guid.NewGuid();

            var id = new LightId(guid);

            id.Guid.ShouldBe(guid);
        }

        [Test]
        public void Empty_Wraps_Guid_Empty()
        {
            LightId.Empty.Guid.ShouldBe(Guid.Empty);
            LightId.Empty.ShouldBe(default);
        }

        [Test]
        public void Equals_And_HashCode_Are_Consistent_For_Same_Value()
        {
            var guid = Guid.NewGuid();
            var id1 = new LightId(guid);
            var id2 = new LightId(guid);

            id1.Equals(id2).ShouldBeTrue();
            (id1 == id2).ShouldBeTrue();
            (id1 != id2).ShouldBeFalse();
            id1.GetHashCode().ShouldBe(id2.GetHashCode());
        }

        [Test]
        public void Equals_Is_False_For_Different_Values()
        {
            var id1 = new LightId(Guid.NewGuid());
            var id2 = new LightId(Guid.NewGuid());

            id1.Equals(id2).ShouldBeFalse();
            (id1 == id2).ShouldBeFalse();
            (id1 != id2).ShouldBeTrue();
        }

        [Test]
        public void CompareTo_Delegates_To_Underlying_Guid_Comparison()
        {
            var lower = new LightId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            var higher = new LightId(Guid.Parse("00000000-0000-0000-0000-000000000002"));

            lower.CompareTo(higher).ShouldBe(lower.Guid.CompareTo(higher.Guid));
            (lower.CompareTo(higher) < 0).ShouldBeTrue();
            (higher.CompareTo(lower) > 0).ShouldBeTrue();
            lower.CompareTo(lower).ShouldBe(0);
        }

        [Test]
        public void Parse_Round_Trips_Own_ToString_Output()
        {
            var id = LightId.NewId();

            var parsed = LightId.Parse(id.ToString());

            parsed.ShouldBe(id);
        }

        [Test]
        public void Parse_Accepts_Hyphenated_Guid_Format()
        {
            var guid = Guid.NewGuid();

            var parsed = LightId.Parse(guid.ToString());

            parsed.Guid.ShouldBe(guid);
        }

        [Test]
        public void Parse_Throws_For_Invalid_Input()
        {
            Assert.Throws<FormatException>(() => LightId.Parse("not-a-guid"));
        }

        [Test]
        public void TryParse_Returns_True_And_Result_For_Valid_Input()
        {
            var id = LightId.NewId();

            var success = LightId.TryParse(id.ToString(), null, out var result);

            success.ShouldBeTrue();
            result.ShouldBe(id);
        }

        [Test]
        public void TryParse_Returns_False_And_Default_For_Invalid_Input()
        {
            var success = LightId.TryParse("not-a-guid", null, out var result);

            success.ShouldBeFalse();
            result.ShouldBe(default);
        }
    }
}
