using System;
using System.Linq;
using RapidImpex.Ampla;
using RapidImpex.Models;
using RapidImpexConsole;
using Shouldly;
using TechTalk.SpecFlow;

namespace RapidImpex.Testing.Features
{
    [Binding]
    public class CommandLineArgumentsSteps
    {
        private string[] _arguments;
        private RapidImpexConfiguration _configuration;

        [Given(@"that I have the following command line arguments:")]
        public void GivenThatIHaveTheFollowingCommandLineArguments(Table table)
        {
            _arguments = table.Rows.Select(x => x["Arguments"]).ToArray();
        }
        
        [When(@"I parse the arguments")]
        public void WhenIParseTheArguments()
        {
            new MyCommandLineParser().Parse(_arguments, out _configuration);
        }
        
        [Then(@"UseHttp is '(.*)'")]
        public void ThenUseHttpIs(bool value)
        {
            _configuration.UseBasicHttp.ShouldBe(value);
        }
        
        [Then(@"User is '(.*)'")]
        public void ThenUserIs(string user)
        {
            _configuration.Username.ShouldBe(user);
        }
        
        [Then(@"Password is '(.*)'")]
        public void ThenPasswordIs(string password)
        {   
            _configuration.Password.ShouldBe(password);
        }
        
        [Then(@"Simple Authentication is '(.*)'")]
        public void ThenSimpleAuthenticationIs(bool value)
        {
            _configuration.UseSimpleAuthentication.ShouldBe(value);
        }
        
        [Then(@"StartTime is '(.*)' in '(.*)'")]
        public void ThenStartTimeIsIn(DateTime time, DateTimeKind kind)
        {
            _configuration.StartTime.ShouldBe(time);
            _configuration.StartTime.Kind.ShouldBe(kind);
        }
        
        [Then(@"EndTime is '(.*)' in '(.*)'")]
       public void ThenEndTimeIsIn(DateTime time, DateTimeKind kind)
        {
            _configuration.EndTime.ShouldBe(time);
            _configuration.EndTime.Kind.ShouldBe(kind);
        }
        
        [Then(@"Modules is:")]
        public void ThenModulesIs(Table table)
        {
            var expectedModules = table.Rows.Select(x => x["Modules"]);

            _configuration.Modules.ShouldBe(expectedModules);
        }

        [Then(@"Module is '(.*)'")]
        public void ThenModuleIs(string module)
        {
            _configuration.Module.ShouldBe(module);
        }

        [Then(@"Path is '(.*)'")]
        public void ThenPathIs(string path)
        {
            _configuration.WorkingDirectory.ShouldBe(path);
        }

        [Then(@"File is '(.*)'")]
        public void ThenFileIs(string file)
        {
            _configuration.File.ShouldBe(file);
        }


        [Then(@"Import is '(.*)'")]
        public void ThenImportIs(bool value)
        {
            _configuration.IsImport.ShouldBe(value);
        }

        [Then(@"Location is '(.*)'")]
        public void ThenLocationIs(string location)
        {
            _configuration.Location.ShouldBe(location);
        }

    }
}
