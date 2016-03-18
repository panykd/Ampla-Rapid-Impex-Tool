using System;
using RapidImpex.Ampla;
using RapidImpex.Ampla.AmplaData200806;
using Shouldly;
using TechTalk.SpecFlow;

namespace RapidImpex.Testing.Features
{
    [Binding]
    public class AmplaModuleTranslationSteps
    {
        private string _inputString ;

        private AmplaModules? _amplaModule;

        [Given(@"I have the string '(.*)'")]
        public void GivenIHaveTheString(string value)
        {
            _inputString = value;
        }
        

        [When(@"I translate into an Ampla Module")]
        public void WhenITranslateIntoAnAmplaModule()
        {
            _amplaModule = _inputString.AsAmplaModule();
        }
        
        [Then(@"I should have the Downtime Module")]
        public void ThenIShouldHaveTheDowntimeModule()
        {
            _amplaModule.ShouldBe(AmplaModules.Downtime);
        }
    }
}
