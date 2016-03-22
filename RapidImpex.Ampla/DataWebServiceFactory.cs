using System;
using Autofac.Features.Indexed;
using RapidImpex.Ampla.AmplaData200806;
using RapidImpex.Models;

namespace RapidImpex.Ampla
{
    public class DataWebServiceFactory
    {
        private readonly RapidImpexConfiguration _configuration;
        private readonly IIndex<string, IDataWebService> _dataClientFactory;

        public DataWebServiceFactory(IIndex<string, IDataWebService> dataClientFactory, RapidImpexConfiguration configuration)
        {
            _dataClientFactory = dataClientFactory;
            _configuration = configuration;
        }

        public IDataWebService GetClient()
        {
            return _configuration.UseBasicHttp ? _dataClientFactory["BasicHttp"] : _dataClientFactory["Tcp"];
        }

        public Credentials GetCredentials()
        {
            if (_configuration.UseSimpleAuthentication == false)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(_configuration.Username) || string.IsNullOrWhiteSpace(_configuration.Password))
            {
                throw new NotImplementedException();
            }

            return new Credentials() {Username = _configuration.Username, Password = _configuration.Password};
        }
    }
}