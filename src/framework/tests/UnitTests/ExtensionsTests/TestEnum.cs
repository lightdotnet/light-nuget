using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace UnitTests.ExtensionsTests
{
    public enum TestEnum
    {
        [Description("Description 1")]
        [Display(Name = "Name of Display 1", Description = "Description of Display 1")]
        Value1 = 1,

        [Description("Description 2")]
        Value2 = 2,

        [Description("Description 3")]
        Value3 = 3,
    }
}
