using Iciclecreek.AI.OpenAI.FormFill.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Iciclecreek.AI.Forms.Tests
{
    public class TestForm
    {
        [Required]
        [Display(Name = "Name", Prompt = "Whats your name?", Description = "The name of a person.")]
        public string Name { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [System.ComponentModel.Description("Your birthday")]
        [Range(typeof(DateOnly), minimum: "01-01-1900", maximum: "01-01-2100", ErrorMessage = "Birthday has to be between 1900 and 2022")]
        public DateOnly? Birthday { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Range(typeof(TimeOnly), minimum: "08:00", maximum: "20:00", ErrorMessage = "Arrival time must be between 8AM and 8PM")]
        public TimeOnly? ArrivalTime { get; set; }

        [Required]
        [DataType(DataType.Duration)]
        public TimeSpan Duration { get; set; }

        [Required]
        [Display(Name = "Percentage")]
        [Range(minimum: 0f, maximum: 100.0f, ErrorMessage = "Percentage must be between 0 and 100.")]
        public Double? Percent { get; set; }

        [Required]
        [Range(minimum: 0, maximum: 100, ErrorMessage = "Attendees must be between 0 and 100.")]
        public int? Attendees { get; set; }

        [Display(Name = "Cool")]
        public bool? Cool { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Favorite Pet is required")]
        public Pets? FavoritePet { get; set; }

        [Required(ErrorMessage = "Categories is required")]
        public List<string> Categories { get; private set; } = new List<string>();

        [ItemValidation(typeof(RangeAttribute), 0, 100)]
        public List<int> Numbers { get; private set; } = new List<int>();
    }
}