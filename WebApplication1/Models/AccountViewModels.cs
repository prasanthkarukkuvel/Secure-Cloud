using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SecureCloud.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }

    public class SendCodeViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
    }

    public class VerifyCodeViewModel
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }
        public string ReturnUrl { get; set; }

        [Display(Name = "Remember this browser?")]
        public bool RememberBrowser { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ForgotViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class FileStore
    {
        [Key]
        public string ID { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public double Size { get; set; }
        [Required]
        public string Type { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }


        public string SizeText
        {
            get
            {
                var units = new[] { "B", "KB", "MB", "GB", "TB" };
                var index = 0;
                double size = Size;
                while (size > 1024)
                {
                    size /= 1024;
                    index++;
                }
                return string.Format("{0:2} {1}", size, units[index]);
            }
        }
    }


    public class TokenStore
    {
        [Key]
        public string ID { get; set; }
        [Required]
        public string Token { get; set; }
        [Required]
        public string UId { get; set; }
    }


    public class UserKeyPairStore
    {
        [Key]
        public string ID { get; set; }
        [Required]
        public string PublicKey { get; set; }
        [Required]
        public string PrivateKey { get; set; }
    }

    public class UserFileStore
    {
        [Key]
        public string ID { get; set; }
        [Required]
        public ApplicationUser User { get; set; }
        [Required]
        public FileStore FileStore { get; set; }
        [Required]
        public string Key { get; set; }
    }

    public class ShareFile
    {
        [Key]
        public string ID { get; set; }
        [Required]
        public ApplicationUser User { get; set; }
        [Required]
        public FileStore FileStore { get; set; }
        [Required]
        public string Key { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }
    }

    public class SharedUserStoreViewModel
    {
        public UserFileStore UserFileStore { get; set; }
        public ShareFile ShareFile { get; set; }
    }

    public class ShareUserFileViewModel
    {
        [Required]
        public string ID { get; set; }

        [Required, DataType(DataType.EmailAddress), Display(Name = "Email Address"), RegularExpression("^([a-zA-Z0-9_.+-]{2,})+\\@(([a-zA-Z0-9-])+\\.)+([a-zA-Z0-9.]{2,})+$", ErrorMessage = "Please enter a valid email address."), MinLength(5, ErrorMessage = "Please enter a valid email address."), MaxLength(100)]
        public string Email { get; set; }

        public UserFileStore UserFileStore { get; set; }
    }
}
