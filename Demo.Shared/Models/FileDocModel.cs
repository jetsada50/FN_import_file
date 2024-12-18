
using System.ComponentModel.DataAnnotations;

namespace Demo.Shared.Models
{
    public class FileDocModel
    {
        [Required(ErrorMessage = "Prefix is required.")]
        //[RegularExpression(@"^(นาย|นาง|นางสาว|น\.ส\.|ดร\.|ศ\.|ผศ\.|รศ\.|พระ|พระครู|พระมหา|พล\.อ\.|พล\.ต\.|ร\.ต\.|ส\.ต\.|จ\.ส\.ต\.)$",
        //    ErrorMessage = "Invalid Prefix for Thailand.")]
        public string Prefix { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        //[RegularExpression(@"^[a-zA-Zก-๙]{2,50}$",
        //    ErrorMessage = "First name must be 2-50 characters and contain only Thai or English letters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        //[RegularExpression(@"^[ก-๙]+(?:[-][ก-๙]+)*$",
        //    ErrorMessage = "Last name must contain only Thai characters and may include a hyphen (-).")]
        //[StringLength(50, MinimumLength = 2,
        //    ErrorMessage = "Last name must be between 2 and 50 characters.")]
        public string Surname { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        //[RegularExpression(@"^[ก-๙\s]{2,50}$",
        //   ErrorMessage = "Department name must be 2-50 characters long and contain only Thai letters.")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Affiliation is required.")]
        //[RegularExpression(@"^[ก-๙a-zA-Z0-9\s\-\/\.]{1,100}$",
        //    ErrorMessage = "Affiliation must only contain Thai/English letters, numbers, spaces, and valid symbols (- / .) with a maximum of 100 characters.")]
        public string Affiliation { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        //[RegularExpression(@"^(?:\d{3}|\d{4}|0[689]\d{8}|0[689]\d{1}-\d{4}-\d{4}|(?:\+66|0066)?[689]\d{8}|\+?\d{9,15})$",
        //    ErrorMessage = "Invalid phone number format.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        //[EnumDataType(typeof(StatusEnum), ErrorMessage = "Status must be 'Active', 'Blacklist', or 'Pending'.")]
        public string Status { get; set; }
    }
}
