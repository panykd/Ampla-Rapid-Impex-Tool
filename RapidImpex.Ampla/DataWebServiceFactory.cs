using System;
using Autofac.Features.Indexed;
using RapidImpex.Ampla.AmplaData200806;
using RapidImpex.Models;

namespace RapidImpex.Ampla
{
    public class DataWebServiceFactory
    {
        private readonly RapidImpexImportExportConfiguration _importExportConfiguration;
        private readonly IIndex<string, IDataWebService> _dataClientFactory;

        public DataWebServiceFactory(IIndex<string, IDataWebService> dataClientFactory, RapidImpexImportExportConfiguration importExportConfiguration)
        {
            _dataClientFactory = dataClientFactory;
            _importExportConfiguration = importExportConfiguration;
        }

        public IDataWebService GetClient()
        {
            return _importExportConfiguration.UseBasicHttp ? _dataClientFactory["BasicHttp"] : _dataClientFactory["Tcp"];
        }

        public Credentials GetCredentials()
        {
            if (_importExportConfiguration.UseSimpleAuthentication == false)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(_importExportConfiguration.Username) || string.IsNullOrWhiteSpace(_importExportConfiguration.Password))
            {
                throw new NotImplementedException();
            }

            return new Credentials() {Username = _importExportConfiguration.Username, Password = _importExportConfiguration.Password};
        }
    }
}