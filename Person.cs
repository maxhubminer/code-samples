using NarodnyUniversity.Helpers.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NarodnyUniversity.Models
{
    public class Person
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "The Phone number field is required.")]
        [Display(Name = "PhoneNumber", ResourceType = typeof(NarodnyUniversity.Resources.DisplayNames))]
        [MinLength(10)]
        [MaxLength(10)]
        [RegularExpression(@"^(9)[0-9]{9}$", ErrorMessage = "10 digits. For example, 9161234567.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "The Last name field is required.")]
        [Display(Name = "LastName", ResourceType = typeof(NarodnyUniversity.Resources.DisplayNames))]
        public string LastName { get; set; }

        [Required(ErrorMessage = "The First name field is required.")]
        [Display(Name = "FirstName", ResourceType = typeof(NarodnyUniversity.Resources.DisplayNames))]
        public string FirstName { get; set; }

        [Display(Name = "MiddleName", ResourceType = typeof(NarodnyUniversity.Resources.DisplayNames))]
        public string MiddleName { get; set; }

        [Display(Name = "isStudent", ResourceType = typeof(NarodnyUniversity.Resources.DisplayNames))]
        [RequiredIf("IsInstructor", true)]
        public bool IsStudent { get; set; }

        [Display(Name = "isInstructor", ResourceType = typeof(NarodnyUniversity.Resources.DisplayNames))]
        public bool IsInstructor { get; set; }

        [Display(Name = "isActive", ResourceType = typeof(NarodnyUniversity.Resources.DisplayNames))]
        public bool IsActive { get; set; } = true;

        // instructor-specific props:
        [Display(Name = "GroupsAsInstructor", ResourceType = typeof(NarodnyUniversity.Resources.DisplayNames))]
        public ICollection<GroupAssignmentAsInstructor> GroupsAsInstructor { get; set; }

        // student-specific props:
        [Display(Name = "GroupsAsStudent", ResourceType = typeof(NarodnyUniversity.Resources.DisplayNames))]
        public ICollection<GroupAssignmentAsStudent> GroupsAsStudent { get; set; }

        // message props+
        public ICollection<MessageRecepient> Messages { get; set; }
        public ICollection<MessageReader> ReadMessages { get; set; } // those who have read the message
        public string pushId; // id of the person's device in Firebase Cloud Messaging database
        // message props-

        // calculated props:
        [Display(Name = "Name", ResourceType = typeof(NarodnyUniversity.Resources.DisplayNames))]
        public string FullName
        {
            get
            {
                return FirstName + " " + LastName;
            }
        }

        [Display(Name = "Name", ResourceType = typeof(NarodnyUniversity.Resources.DisplayNames))]
        public string FullNameWithMiddleName
        {
            get
            {
                return FirstName + " " + MiddleName + " " + LastName;
            }
        }
    }
}
