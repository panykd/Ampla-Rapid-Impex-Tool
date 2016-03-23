﻿Feature: CommandLineArguments

Scenario: Simple Authentication using Http for Downtime module
	Given that I have the following command line arguments:
		| Arguments           |
		| --useHttp           |
		| -user="ampla"       |
		| -password="password"   |
		| --simple            |
		| -start="2016-02-01" |
		| -end="2016-02-22"   |
		| --Downtime          |
	When I parse the arguments
	Then UseHttp is 'true'
	And User is 'ampla'
	And Password is 'password'
	And Simple Authentication is 'true'
	And StartTime is '2016-02-01 00:00:00' in 'Local'
	And EndTime is '2016-02-22 00:00:00' in 'Local'
	And Import is 'false'
	And Modules is:
	| Modules  |
	| Downtime |

Scenario: Simple Authentication using TCP for Knowledge and Production
	Given that I have the following command line arguments:
		| Arguments                     |
		| -start="2016-02-01"           |
		| -endUtc="2016-02-22 12:34:56" |
		| --Knowledge                   |
		| --Production                  |
	When I parse the arguments
	Then UseHttp is 'false'
	And Simple Authentication is 'false'
	And StartTime is '2016-02-01 00:00:00' in 'Local'
	And EndTime is '2016-02-22 12:34:56' in 'Utc'
	And Import is 'false'
	And Modules is:
	| Modules    |
	| Knowledge  |
	| Production |

Scenario: Import with Integrated Authentication using TCP for Quality
	Given that I have the following command line arguments:
		| Arguments             |
		| --import              |
		| -path="c:\temp\files" |
		| --Quality             |
	When I parse the arguments
	Then UseHttp is 'false'
	And Simple Authentication is 'false'
	And Path is 'c:\temp\files'
	And Import is 'true'
	And Modules is:
	| Modules |
	| Quality |

