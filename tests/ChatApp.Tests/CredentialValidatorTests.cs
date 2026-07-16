using ChatApp.Web.Validation;
using Xunit;

namespace ChatApp.Tests;

public class CredentialValidatorTests
{
    [Theory]
    [InlineData("jane.doe")]
    [InlineData("mike_b")]
    [InlineData("a12")]
    [InlineData("stock-bot99")]
    public void IsValidUsername_AcceptsWellFormedNames(string username)
    {
        Assert.True(CredentialValidator.IsValidUsername(username));
    }

    [Theory]
    [InlineData("ab")]                 // too short
    [InlineData("1jane")]              // must start with a letter
    [InlineData("jane doe")]           // no spaces
    [InlineData("jane@doe")]           // no @
    [InlineData("this-username-is-way-too-long-for-the-rules")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValidUsername_RejectsMalformedNames(string? username)
    {
        Assert.False(CredentialValidator.IsValidUsername(username));
    }

    [Theory]
    [InlineData("password1")]
    [InlineData("Sup3rSecret")]
    public void IsValidPassword_AcceptsWellFormedPasswords(string password)
    {
        Assert.True(CredentialValidator.IsValidPassword(password));
    }

    [Theory]
    [InlineData("short1")]             // too short
    [InlineData("alllettersnodigits")] // no digit
    [InlineData("12345678")]           // no letter
    [InlineData("")]
    [InlineData(null)]
    public void IsValidPassword_RejectsMalformedPasswords(string? password)
    {
        Assert.False(CredentialValidator.IsValidPassword(password));
    }
}
