Feature: CommandLineArguments

Scenario: Simple Authentication using Http for Downtime module
	Given that I have the following command line arguments:
		| Arguments          |
		| --useHttp          |
		| -user=ampla        |
		| -password=password |
		| --simple           |
		| -start=2016-02-01  |
		| -end=2016-02-22    |
		| -module=Downtime         |
	When I parse the arguments
	Then UseHttp is 'true'
	And User is 'ampla'
	And Password is 'password'
	And Simple Authentication is 'true'
	And StartTime is '2016-02-01 00:00:00' in 'Local'
	And EndTime is '2016-02-22 00:00:00' in 'Local'
	And Import is 'false'
	And Module is 'Downtime'

Scenario: Simple Authentication using TCP for Knowledge and Production
	Given that I have the following command line arguments:
		| Arguments                     |
		| -start=2016-02-01           |
		| -endUtc=2016-02-22 12:34:56 |
		| -module=Production                 |
	When I parse the arguments
	Then UseHttp is 'false'
	And Simple Authentication is 'false'
	And StartTime is '2016-02-01 00:00:00' in 'Local'
	And EndTime is '2016-02-22 12:34:56' in 'Utc'
	And Import is 'false'
	And Module is 'Production'

Scenario: Import with Integrated Authentication using TCP for Quality
	Given that I have the following command line arguments:
		| Arguments           |
		| --import            |
		| -path=c:\temp\files |
		| -file=output.xlsx   |
		| -module=Knowledge   |
	When I parse the arguments
	Then UseHttp is 'false'
	And Simple Authentication is 'false'
	And Path is 'c:\temp\files'
	And File is 'output.xlsx'
	And Import is 'true'
	And Module is 'Knowledge'

