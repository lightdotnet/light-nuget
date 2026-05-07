using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace UnitTests.ExtensionsTests
{
    [DisplayName("Test DisplayName")]
    [System.ComponentModel.Description("Test Description")]
    [Display(Name = "Test Name in Display", Description = "Test Description in Display")]
    public class TestObject
    {
        [DisplayName("Id DisplayName")]
        [System.ComponentModel.Description("Id Description")]
        [Display(Name = "Id Name in Display", Description = "Id Description in Display")]
        public int Id { get; set; }

        public string? Value { get; set; }

        [System.ComponentModel.Description("Color Description")]
        public const string Color = "RGB";
    }
}
