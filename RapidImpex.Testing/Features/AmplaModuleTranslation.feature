Feature: AmplaModuleTranslation
	Ampla Modules should be able to be represented as strings, and
	be converted backward and forward without issue.

Scenario Outline: String to Module Translations
	Given I have the string '<String>'
	When I translate into an Ampla Module
	Then I should have the Downtime Module
Examples: 
| String   |
| Downtime |
| downtime |
| dOwNTime |
