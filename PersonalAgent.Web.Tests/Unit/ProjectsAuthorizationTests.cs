
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAgent.Web.Controllers;

namespace PersonalAgent.Web.Tests.Unit;

public class ProjectsAuthorizationTests
{
    [Fact]
    public void ProjectsController_ShouldRequireAuthentication()
    {
        var attribute = typeof(ProjectsController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Edit")]
    [InlineData("Delete")]
    public void AdminActions_ShouldRequireAdminRole(string actionName)
    {
        var methods = typeof(ProjectsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == actionName ||
                        m.GetCustomAttribute<ActionNameAttribute>()?.Name == actionName)
            .Where(m => m.GetCustomAttribute<HttpGetAttribute>() != null ||
                        m.GetCustomAttribute<HttpPostAttribute>() != null)
            .ToList();

        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(attribute);
            Assert.Contains("Admin",
                attribute.Roles?.Split(',').Select(r => r.Trim())
                ?? Array.Empty<string>());
        }
    }

    [Theory]
    [InlineData("Index")]
    [InlineData("Details")]
    public void ReadActions_ShouldNotRequireAdminRole(string actionName)
    {
        var methods = typeof(ProjectsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == actionName)
            .ToList();

        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<AuthorizeAttribute>();

            Assert.True(
                attribute is null || string.IsNullOrWhiteSpace(attribute.Roles));
        }
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Edit")]
    [InlineData("DeleteConfirmed")]
    public void WriteActions_ShouldValidateAntiForgeryToken(string actionName)
    {
        var methods = typeof(ProjectsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == actionName)
            .Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null)
            .ToList();

        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            Assert.NotNull(
                method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        }
    }
}
