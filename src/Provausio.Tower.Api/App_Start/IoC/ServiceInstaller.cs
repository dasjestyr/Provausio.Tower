using System;
using System.Configuration;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Provausio.Tower.Api.Data;
using Provausio.Tower.Core;

namespace Provausio.Tower.Api.App_Start.IoC
{
    public class ServiceInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component
                .For<IPubSubHub>()
                .ImplementedBy<Hub>()
                .DependsOn(Dependency.OnValue("hubLocation", GetHubUri())) // TODO: make configurable
                .LifestyleSingleton());

            container.Register(Component
                .For<ICryptoFunctions>()
                .ImplementedBy<CryptoFunctions>()
                .LifestyleSingleton());

            container.Register(Component
                .For<ISubscriptionStore>()
                .ImplementedBy<InMemorySubscriptionStore>()
                .LifestyleSingleton());
        }

        private Uri GetHubUri()
        {
            var hostname = ConfigurationManager.AppSettings.Get("PublicHostname");
            return new Uri(hostname);
        }
    }
}
