using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public class ChnagePasswordModel
    {
        public string? UserName { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
    public class ForgotPasswordModel
    {
        public string Username { get; set; }
    }
    public class PasswordResetModel
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public class UserViewModel
    {
        public DateTimeOffset? LastLoginDate { get; set; }
        public string? Id { get; set; }
        public string PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string UserType { get; set; }
        public DateTimeOffset CreatedOn { get; set; } 
        public string? CreatedBy { get; set; }
        public bool IsActive { get; set; }
        public string? Name { get; set; }
        //public string Location { get; set; }
        //public string State { get; set; }
        //public string Lga { get; set; }
        //public string? Gender { get; set; }
        //public string? DateOfBirth { get; set; }
        public string BusinessCode { get; set; }
    }
    public class ImageComparisonResponse
    {
        public bool IsSimilarFace { get; set; }
        public double SimilarityPercentage { get; set; }
        public string ImageKey { get; set; }
    }

    public class ImageUploadResponse
    {
        public string Message { get; set; }
        public string ImageKey { get; set; }
    }
    public class ImageVerificationArgs
    {
        public string FarmerUserId { get; set; }
        public string BusinessCode { get; set; }
    }
    public class ImageModel
    {
        public IFormFile File { get; set; }
        public string FarmerUserId { get; set; }
    }
    public class UserModel
    {
      //  public IFormFile? FarmerCapturedImage { get; set; }
        public string? Id { get; set; }
        public string? Salt { get; set; }
        public string PhoneNumber { get; set; }
        public string? Pin { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
        //public UserType UserType { get; set; }
        public string UserType { get; set; }
        public string? OTP { get; set; }
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string? CreatedBy { get; set; }
        public DateTimeOffset? DateModified { get; set; } = DateTimeOffset.Now;
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset? LastLoginDate { get; set; } = DateTimeOffset.Now;
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }

        public string? Bvn { get; set; }
        public string? NIN { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? BusinessCode { get; set; }
    }
    public enum UserType
    {
        [Description("User")]
        User,
        [Description("Business")]
        Business,
        [Description("Farmer")]
        Farmer,
        [Description("Cooperative")]
        Cooperative,
        [Description("Agent")]
        Agent,
        [Description("Suppliers")]
        Suppliers,
        [Description("Processors")]
        Processors,
        [Description("Agrovesto")]
        Agrovesto,
        [Description("Buyer")]
        Buyer,
        [Description("Trader")]
        Trader
    }
    public class LoginModel
    {
        public string? Username { get; set; }
        public string? password { get; set; }
        public string? phonenumber { get; set; }
        public string? pin { get; set; }
        public LoginType LoginMethod { get; set; }
    }


    public class TokenModel
    {
        public string Token { get; set; }
        public TokenType TokenType { get; set; }
        public DateTimeOffset ExpiresOn { get; set; }
        public string Id { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public double ExpiresIn {  get; set; }
        public List<string> Roles { get; set; }
    }

    public enum TokenType
    {
        AccessToken,
        IdToken,
        RefreshToken
            
    }
    public enum LoginType
    {
        Password,
        Pin
    }
}
